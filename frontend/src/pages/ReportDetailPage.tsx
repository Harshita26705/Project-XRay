import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { ReportResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { Loader } from '../components/Loader';

export default function ReportDetailPage() {
  const { reportId } = useParams();
  const [report, setReport] = useState<ReportResponse | null>(null);
  const [format, setFormat] = useState<'pdf' | 'xlsx'>('pdf');
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    if (!reportId) return;
    void api.getReport(reportId).then(setReport);
  }, [reportId]);

  if (!report) return <Loader label="Loading..." fullHeight />;

  const exportReport = async () => {
    if (!reportId) return;
    setExporting(true);
    try {
      const blob = await api.downloadReport(reportId, format);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `${report.title}.${format === 'pdf' ? 'html' : 'csv'}`;
      anchor.click();
      URL.revokeObjectURL(url);
    } finally {
      setExporting(false);
    }
  };

  const section = (type: string) => report.sections.find((s) => s.sectionType === type);

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold">{report.title}</h1>
          <p className="mt-1 text-xs text-text-muted">Generated {new Date(report.createdAtUtc).toLocaleString()}</p>
        </div>
        <div className="flex gap-2">
          <select value={format} onChange={(event) => setFormat(event.target.value as 'pdf' | 'xlsx')} aria-label="Export format" className="rounded-md border border-border bg-cardMuted px-2 text-sm">
            <option value="pdf">PDF</option>
            <option value="xlsx">Excel</option>
          </select>
          <Button onClick={() => void exportReport()} disabled={exporting}>{exporting ? 'Exporting...' : 'Export'}</Button>
          <Button>Markdown</Button>
          <Button variant="primary">Share Report</Button>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-5">
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Executive Summary</h3>
          <p className="text-sm text-text-secondary">{section('EXECUTIVE_SUMMARY')?.content}</p>
        </Card>
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Blast Radius</h3>
          <p className="text-sm text-text-secondary">{section('BLAST_RADIUS')?.content}</p>
        </Card>
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Security &amp; Vulnerabilities</h3>
          <p className="text-sm text-text-secondary">{section('SECURITY')?.content}</p>
        </Card>
      </div>

      <div className="grid grid-cols-2 gap-5">
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Microsoft Foundry Analysis &amp; Explanation</h3>
          <p className="text-sm text-text-secondary">{section('EXECUTIVE_SUMMARY')?.content}</p>
        </Card>
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Recommendations</h3>
          <ul className="space-y-1 text-sm text-text-secondary">
            {(section('RECOMMENDATIONS')?.content ?? '').split('\n').filter(Boolean).map((line) => (
              <li key={line} className="flex gap-2">
                <span className="text-risk-safe">&#10003;</span>
                {line}
              </li>
            ))}
          </ul>
        </Card>
      </div>
    </div>
  );
}
