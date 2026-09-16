import React, { createContext, useContext, useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api/endpoints';
import type { BranchResponse, ProjectResponse } from '../api/types';

interface ProjectContextValue {
  projects: ProjectResponse[];
  currentProject: ProjectResponse | null;
  setCurrentProjectId: (id: string) => void;
  loading: boolean;
  error: string | null;
  branches: BranchResponse[];
  currentBranch: BranchResponse | null;
  setCurrentBranchName: (name: string) => void;
  refreshBranches: () => Promise<void>;
  branchError: string | null;
  refresh: () => Promise<void>;
}

const ProjectContext = createContext<ProjectContextValue | null>(null);

export function ProjectProvider({ children }: { children: React.ReactNode }) {
  const [currentProjectId, setCurrentProjectId] = useState<string | null>(() => localStorage.getItem('xray_current_project'));
  const [currentBranchName, setCurrentBranchName] = useState<string | null>(null);

  const projectsQuery = useQuery({ queryKey: ['projects'], queryFn: api.listProjects });
  const projects: ProjectResponse[] = projectsQuery.data ?? [];
  const branchesQuery = useQuery({
    queryKey: ['branches', currentProjectId],
    queryFn: () => api.listBranches(currentProjectId!),
    enabled: Boolean(currentProjectId)
  });
  const branches: BranchResponse[] = branchesQuery.data ?? [];

  useEffect(() => {
    if (projects.length > 0 && !projects.some((project) => project.projectId === currentProjectId)) {
      setCurrentProjectId(projects[0].projectId);
      localStorage.setItem('xray_current_project', projects[0].projectId);
    }
  }, [projects, currentProjectId]);

  useEffect(() => {
    if (!currentProjectId) {
      setCurrentBranchName(null);
      return;
    }
    const saved = localStorage.getItem(`xray_branch_${currentProjectId}`);
    const selected = branches.find((branch) => branch.name === saved)
      ?? branches.find((branch) => branch.isDefault)
      ?? branches[0];
    setCurrentBranchName(selected?.name ?? null);
    if (selected) localStorage.setItem(`xray_branch_${currentProjectId}`, selected.name);
  }, [branches, currentProjectId]);

  const handleSetCurrent = (id: string) => {
    setCurrentProjectId(id);
    localStorage.setItem('xray_current_project', id);
  };

  const handleSetCurrentBranch = (name: string) => {
    setCurrentBranchName(name);
    if (currentProjectId) localStorage.setItem(`xray_branch_${currentProjectId}`, name);
  };

  const refresh = async () => {
    await projectsQuery.refetch();
  };

  const refreshBranches = async () => {
    await branchesQuery.refetch();
  };

  const currentProject = projects.find((p) => p.projectId === currentProjectId) ?? null;
  const currentBranch = branches.find((branch) => branch.name === currentBranchName) ?? null;

  return (
    <ProjectContext.Provider value={{
      projects,
      currentProject,
      setCurrentProjectId: handleSetCurrent,
      loading: projectsQuery.isLoading,
      error: projectsQuery.error instanceof Error ? projectsQuery.error.message : null,
      branches,
      currentBranch,
      setCurrentBranchName: handleSetCurrentBranch,
      refreshBranches,
      branchError: branchesQuery.error instanceof Error ? branchesQuery.error.message : null,
      refresh
    }}>
      {children}
    </ProjectContext.Provider>
  );
}

export function useProjects() {
  const ctx = useContext(ProjectContext);
  if (!ctx) throw new Error('useProjects must be used within ProjectProvider');
  return ctx;
}
