import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { StatusBadge } from '../components/StatusBadge';

export default function ProjectsPage() {
  const { projects, refresh, setCurrentProjectId } = useProjects();
  const navigate = useNavigate();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('CGOne');
  const [path, setPath] = useState('D:\\Hackathon\\Project-XRay\\demo-app');
  const [busy, setBusy] = useState(false);

  const handleCreate = async () => {
    setBusy(true);
    try {
      const created = await api.createProject({ name, localRepositoryPath: path });
      await api.ingest(created.projectId, path);
      await refresh();
      setCurrentProjectId(created.projectId);
      setShowForm(false);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold">Projects</h1>
          <p className="mt-1 text-sm text-text-secondary">Connect and manage dependency tracking directories.</p>
        </div>
        <Button variant="primary" onClick={() => setShowForm((v) => !v)}>
          + Connect Project
        </Button>
      </div>

      {showForm && (
        <Card className="p-4">
          <div className="grid grid-cols-2 gap-4">
            <label className="text-xs text-text-secondary">
              Project name
              <input value={name} onChange={(e) => setName(e.target.value)} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
            </label>
            <label className="text-xs text-text-secondary">
              Local repository path
              <input value={path} onChange={(e) => setPath(e.target.value)} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
            </label>
          </div>
          <div className="mt-3 flex gap-2">
            <Button variant="primary" onClick={handleCreate} disabled={busy}>
              {busy ? 'Creating & Ingesting...' : 'Create & Ingest'}
            </Button>
            <Button onClick={() => setShowForm(false)}>Cancel</Button>
          </div>
        </Card>
      )}

      <div className="grid grid-cols-3 gap-5">
        {projects.map((project) => (
          <Card key={project.projectId} className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-primary">&#128193;</span>
                <span className="font-semibold">{project.name}</span>
              </div>
              <StatusBadge status={project.connectionStatus} />
            </div>
            <p className="mt-1 truncate text-xs text-text-muted">{project.repositoryReference ?? 'No repository configured'}</p>

            <div className="mt-3 flex gap-1.5">
              {project.technologyTags.map((tag) => (
                <span key={tag} className="rounded border border-border px-1.5 py-0.5 text-[10px] text-text-secondary">
                  {tag}
                </span>
              ))}
            </div>

            <div className="mt-4 grid grid-cols-3 gap-2 text-xs">
              <div>
                <p className="text-text-muted">NODES</p>
                <p className="font-semibold">{project.nodeCount}</p>
              </div>
              <div>
                <p className="text-text-muted">RELATIONSHIPS</p>
                <p className="font-semibold">{project.relationshipCount}</p>
              </div>
              <div>
                <p className="text-text-muted">SECURITY</p>
                <p className="font-semibold text-risk-critical">{project.securityFindingCount} flaws</p>
              </div>
            </div>

            {project.lastIndexedAtUtc && (
              <p className="mt-2 text-[10px] text-text-muted">Last indexed: {new Date(project.lastIndexedAtUtc).toLocaleString()}</p>
            )}

            <div className="mt-4 flex gap-2">
              <Button variant="primary" onClick={() => { setCurrentProjectId(project.projectId); navigate(`/projects/${project.projectId}/architecture`); }}>
                Open Project
              </Button>
              <Button onClick={() => api.ingest(project.projectId).then(() => refresh())}>Re-index</Button>
            </div>
          </Card>
        ))}

        <button
          onClick={() => setShowForm(true)}
          className="flex min-h-[220px] flex-col items-center justify-center rounded-lg border border-dashed border-border p-4 text-center hover:border-primary/50"
        >
          <span className="mb-2 flex h-8 w-8 items-center justify-center rounded-full bg-cardMuted text-lg">+</span>
          <p className="text-sm font-semibold">Connect New Repository</p>
          <p className="mt-1 max-w-[220px] text-xs text-text-muted">
            Add an Azure DevOps project path to automatically construct its dependency blast radius graph.
          </p>
        </button>
      </div>
    </div>
  );
}
