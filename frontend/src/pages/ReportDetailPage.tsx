import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { ReportResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';

export default function ReportDetailPage() {
  const { reportId } = useParams();
  const [report, setReport] = useState<ReportResponse | null>(null);

  useEffect(() => {
    if (!reportId) return;
    void api.getReport(reportId).then(setReport);
  }, [reportId]);

  if (!report) return <div className="text-sm text-text-muted">Loading...</div>;

  const section = (type: string) => report.sections.find((s) => s.sectionType === type);

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold">{report.title}</h1>
          <p className="mt-1 text-xs text-text-muted">Generated {new Date(report.createdAtUtc).toLocaleString()}</p>
        </div>
        <div className="flex gap-2">
          <Button>Export PDF</Button>
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
