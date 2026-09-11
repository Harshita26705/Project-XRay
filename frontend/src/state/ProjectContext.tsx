import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { api } from '../api/endpoints';
import type { ProjectResponse } from '../api/types';

interface ProjectContextValue {
  projects: ProjectResponse[];
  currentProject: ProjectResponse | null;
  setCurrentProjectId: (id: string) => void;
  loading: boolean;
  refresh: () => Promise<void>;
}

const ProjectContext = createContext<ProjectContextValue | null>(null);

export function ProjectProvider({ children }: { children: React.ReactNode }) {
  const [projects, setProjects] = useState<ProjectResponse[]>([]);
  const [currentProjectId, setCurrentProjectId] = useState<string | null>(() => localStorage.getItem('xray_current_project'));
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      const list = await api.listProjects();
      setProjects(list);
      if (list.length > 0 && !list.some((p) => p.projectId === currentProjectId)) {
        setCurrentProjectId(list[0].projectId);
        localStorage.setItem('xray_current_project', list[0].projectId);
      }
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const handleSetCurrent = (id: string) => {
    setCurrentProjectId(id);
    localStorage.setItem('xray_current_project', id);
  };

  const currentProject = projects.find((p) => p.projectId === currentProjectId) ?? null;

  return (
    <ProjectContext.Provider value={{ projects, currentProject, setCurrentProjectId: handleSetCurrent, loading, refresh }}>
      {children}
    </ProjectContext.Provider>
  );
}

export function useProjects() {
  const ctx = useContext(ProjectContext);
  if (!ctx) throw new Error('useProjects must be used within ProjectProvider');
  return ctx;
}
