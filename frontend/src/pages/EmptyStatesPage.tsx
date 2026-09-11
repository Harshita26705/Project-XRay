import { EmptyState } from '../components/States';
import { Button } from '../components/Button';

export default function EmptyStatesPage() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Platform Empty States</h1>
        <p className="mt-1 text-sm text-text-secondary">Review mockups of the platform's default states when integrations or data results are absent.</p>
      </div>
      <div className="grid grid-cols-2 gap-5">
        <EmptyState title="No projects connected" message="Connect your Azure DevOps project workspace to start tracing codebase structures." action={<Button variant="primary">Connect Project</Button>} />
        <EmptyState title="No analyses yet" message="Run your first X-Ray dependency sweep to determine high-risk areas of a change." action={<Button variant="primary">Analyze a Change</Button>} />
        <EmptyState title="No security findings" message="Compliance details and secrets disclosures will populate here after running a scan." action={<Button variant="primary">Run Security Scan</Button>} />
        <EmptyState title="No reports generated" message="Reports summarize structural trends and are created automatically on analysis cycles." action={<Button variant="primary">View Analyses</Button>} />
      </div>
    </div>
  );
}
