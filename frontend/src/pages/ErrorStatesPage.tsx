import { ErrorState } from '../components/States';
import { Button } from '../components/Button';

export default function ErrorStatesPage() {
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Platform Error States</h1>
        <p className="mt-1 text-sm text-text-secondary">Review mockups of system failure parameters, unavailable services, and unknown metadata boundaries.</p>
      </div>
      <div className="grid grid-cols-2 gap-5">
        <ErrorState tone="critical" title="Analysis incomplete" message="Some evidence could not be collected. The dependency graph may be incomplete." action={<Button variant="danger">Retry Analysis</Button>} />
        <ErrorState tone="risky" title="Azure DevOps unavailable" message="Cannot reach Azure DevOps repository. Check your enterprise connection permissions and credentials." action={<Button>Retry Connection</Button>} />
        <ErrorState tone="unknown" title="Unknown Impact" message="X-Ray could not determine the dependency relationship. Reasons: parser limitation, repository unavailable, dynamic runtime dependency, insufficient metadata." action={<Button>View Evidence</Button>} />
        <ErrorState tone="critical" title="AI analysis unavailable" message="Microsoft Foundry is not responding. Dependency analysis is still available, but AI explanations are temporarily unavailable." action={<Button>Check AI Status</Button>} />
      </div>
    </div>
  );
}
