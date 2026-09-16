import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { StatusBadge } from '../components/StatusBadge';
import type { AzureDevOpsRepositoryResponse } from '../api/types';

export default function ProjectsPage() {
  const { projects, refresh, setCurrentProjectId, currentProject, currentBranch, error: projectError } = useProjects();
  const navigate = useNavigate();
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('CGOne');
  const [path, setPath] = useState('D:\\Hackathon\\Project-XRay\\demo-app');
  const [azureDevOpsUrl, setAzureDevOpsUrl] = useState('');
  const [source, setSource] = useState<'LOCAL' | 'AZURE_DEVOPS'>('LOCAL');
  const [azureRepositories, setAzureRepositories] = useState<AzureDevOpsRepositoryResponse[]>([]);
  const [repositoryError, setRepositoryError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [editingProjectId, setEditingProjectId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [deleteProjectId, setDeleteProjectId] = useState<string | null>(null);

  useEffect(() => {
    if (!showForm || source !== 'AZURE_DEVOPS') return;
    setRepositoryError(null);
    void api.listAzureDevOpsRepositories()
      .then(setAzureRepositories)
      .catch((reason: unknown) => setRepositoryError(reason instanceof Error ? reason.message : 'Unable to load Azure DevOps repositories.'));
  }, [showForm, source]);

  const handleCreate = async () => {
    setBusy(true);
    try {
      const created = await api.createProject({
        name,
        localRepositoryPath: source === 'LOCAL' ? path || undefined : undefined,
        externalProjectUrl: source === 'AZURE_DEVOPS' ? azureDevOpsUrl || undefined : undefined
      });
      if (source === 'LOCAL' && path) await api.ingest(created.projectId, path);
      await refresh();
      setCurrentProjectId(created.projectId);
      setShowForm(false);
    } finally {
      setBusy(false);
    }
  };

  const startEdit = (project: typeof projects[number]) => {
    setEditingProjectId(project.projectId);
    setEditName(project.name);
    setEditDescription(project.description ?? '');
  };

  const handleUpdate = async () => {
    if (!editingProjectId || !editName.trim()) return;
    setBusy(true);
    try {
      await api.updateProject(editingProjectId, { name: editName.trim(), description: editDescription.trim() || null });
      await refresh();
      setEditingProjectId(null);
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteProjectId) return;
    setBusy(true);
    try {
      await api.deleteProject(deleteProjectId);
      if (currentProject?.projectId === deleteProjectId) setCurrentProjectId('');
      await refresh();
      setDeleteProjectId(null);
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
            <div className="col-span-2 flex gap-2 border-b border-border pb-3">
              {(['LOCAL', 'AZURE_DEVOPS'] as const).map((option) => (
                <button key={option} type="button" onClick={() => setSource(option)} className={`rounded border px-3 py-1.5 text-xs ${source === option ? 'border-primary bg-primary/10 text-primary' : 'border-border text-text-secondary'}`}>
                  {option === 'LOCAL' ? 'Local repository' : 'Azure DevOps'}
                </button>
              ))}
            </div>
            {source === 'LOCAL' ? (
              <label className="text-xs text-text-secondary">
                Local repository path
                <input value={path} onChange={(e) => setPath(e.target.value)} placeholder="D:\\path\\to\\repository" className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
              </label>
            ) : (
              <div className="col-span-2 space-y-2">
                <label className="block text-xs text-text-secondary">
                  Azure DevOps repository
                  <select value={azureDevOpsUrl} onChange={(e) => setAzureDevOpsUrl(e.target.value)} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm">
                    <option value="">Enter a repository URL below</option>
                    {azureRepositories.map((repository) => <option key={repository.id} value={repository.webUrl ?? ''}>{repository.name}</option>)}
                  </select>
                </label>
                <input value={azureDevOpsUrl} onChange={(e) => setAzureDevOpsUrl(e.target.value)} placeholder="https://dev.azure.com/org/project/_git/repo" className="w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
                {repositoryError && <p className="text-xs text-risk-risky">{repositoryError}</p>}
              </div>
            )}
          </div>
          <div className="mt-3 flex gap-2">
            <Button variant="primary" onClick={handleCreate} disabled={busy}>
              {busy ? 'Connecting...' : 'Connect Project'}
            </Button>
            <Button onClick={() => setShowForm(false)}>Cancel</Button>
          </div>
        </Card>
      )}

      {projectError && (
        <Card className="flex items-center justify-between border-risk-critical/40 bg-risk-critical/10 p-4">
          <div>
            <p className="text-sm font-medium text-risk-critical">Projects unavailable</p>
            <p className="mt-1 text-xs text-text-secondary">{projectError}</p>
          </div>
          <Button onClick={() => void refresh()}>Retry</Button>
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

            <div className="mt-4 grid grid-cols-2 gap-2 border-t border-border pt-3">
              <Button
                variant="primary"
                className="col-span-2 justify-center"
                onClick={() => { setCurrentProjectId(project.projectId); navigate(`/projects/${project.projectId}/architecture`); }}
              >
                <span aria-hidden="true">&#8594;</span> Open Project
              </Button>
              <Button
                className="justify-center"
                onClick={() => void api.ingest(project.projectId, undefined, project.projectId === currentProject?.projectId ? currentBranch?.name : undefined).then(() => refresh())}
              >
                <span aria-hidden="true">&#8635;</span> Re-index
              </Button>
              <Button className="justify-center" onClick={() => startEdit(project)}>
                <span aria-hidden="true">&#9998;</span> Edit
              </Button>
              <Button variant="danger" className="col-span-2 justify-center" onClick={() => setDeleteProjectId(project.projectId)}>
                <span aria-hidden="true">&#128465;</span> Delete Project
              </Button>
            </div>

            {editingProjectId === project.projectId && (
              <div className="mt-3 space-y-2 border-t border-border pt-3">
                <input value={editName} onChange={(event) => setEditName(event.target.value)} aria-label="Project name" className="w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
                <textarea value={editDescription} onChange={(event) => setEditDescription(event.target.value)} aria-label="Project description" rows={2} className="w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
                <div className="flex gap-2">
                  <Button variant="primary" onClick={() => void handleUpdate()} disabled={busy}>{busy ? 'Saving...' : 'Save'}</Button>
                  <Button onClick={() => setEditingProjectId(null)} disabled={busy}>Cancel</Button>
                </div>
              </div>
            )}
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

      {deleteProjectId && (
        <div className="fixed inset-0 z-20 flex items-center justify-center bg-black/60 px-4">
          <div className="w-full max-w-md rounded-lg border border-border bg-card p-5 shadow-xl">
            <h2 className="text-lg font-semibold">Delete project?</h2>
            <p className="mt-2 text-sm text-text-secondary">This permanently removes the project graph, analyses, findings, and repository metadata.</p>
            <div className="mt-5 flex justify-end gap-2">
              <Button onClick={() => setDeleteProjectId(null)} disabled={busy}>Cancel</Button>
              <Button variant="danger" onClick={() => void handleDelete()} disabled={busy}>{busy ? 'Deleting...' : 'Delete project'}</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
