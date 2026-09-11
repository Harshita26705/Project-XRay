import { useEffect } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthProvider';
import { registerTokenGetter } from './api/client';
import { ProjectProvider } from './state/ProjectContext';
import { AppShell } from './components/AppShell';

import LoginPage from './pages/LoginPage';
import OverviewPage from './pages/OverviewPage';
import ProjectsPage from './pages/ProjectsPage';
import ArchitecturePage from './pages/ArchitecturePage';
import AnalyzeChangePage from './pages/AnalyzeChangePage';
import AnalysisProgressPage from './pages/AnalysisProgressPage';
import AnalysisResultsPage from './pages/AnalysisResultsPage';
import TraceEvidencePage from './pages/TraceEvidencePage';
import SecurityCenterPage from './pages/SecurityCenterPage';
import ChangesPage from './pages/ChangesPage';
import PullRequestDetailPage from './pages/PullRequestDetailPage';
import WorkItemDetailPage from './pages/WorkItemDetailPage';
import ReportsPage from './pages/ReportsPage';
import ReportDetailPage from './pages/ReportDetailPage';
import IntegrationsPage from './pages/IntegrationsPage';
import AiConfigurationPage from './pages/AiConfigurationPage';
import SettingsPage from './pages/SettingsPage';
import NotificationSettingsPage from './pages/NotificationSettingsPage';
import EmptyStatesPage from './pages/EmptyStatesPage';
import ErrorStatesPage from './pages/ErrorStatesPage';
import AnalysesListPage from './pages/AnalysesListPage';

function RequireAuth({ children }: { children: JSX.Element }) {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return children;
}

function TokenBridge() {
  const { getAccessToken } = useAuth();
  useEffect(() => {
    registerTokenGetter(getAccessToken);
  }, [getAccessToken]);
  return null;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <ProjectProvider>
              <AppShell />
            </ProjectProvider>
          </RequireAuth>
        }
      >
        <Route path="/overview" element={<OverviewPage />} />
        <Route path="/projects" element={<ProjectsPage />} />
        <Route path="/projects/:projectId/architecture" element={<ArchitecturePage />} />
        <Route path="/analyses" element={<AnalysesListPage />} />
        <Route path="/analyses/new" element={<AnalyzeChangePage />} />
        <Route path="/analyses/:analysisId/progress" element={<AnalysisProgressPage />} />
        <Route path="/analyses/:analysisId" element={<AnalysisResultsPage />} />
        <Route path="/analyses/:analysisId/evidence" element={<TraceEvidencePage />} />
        <Route path="/security" element={<SecurityCenterPage />} />
        <Route path="/changes" element={<ChangesPage />} />
        <Route path="/changes/pr/:changeId" element={<PullRequestDetailPage />} />
        <Route path="/changes/work-item/:changeId" element={<WorkItemDetailPage />} />
        <Route path="/reports" element={<ReportsPage />} />
        <Route path="/reports/:reportId" element={<ReportDetailPage />} />
        <Route path="/integrations" element={<IntegrationsPage />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/settings/ai" element={<AiConfigurationPage />} />
        <Route path="/settings/notifications" element={<NotificationSettingsPage />} />
        <Route path="/dev/empty-states" element={<EmptyStatesPage />} />
        <Route path="/dev/error-states" element={<ErrorStatesPage />} />
        <Route index element={<Navigate to="/overview" replace />} />
      </Route>
      <Route path="*" element={<Navigate to="/overview" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <TokenBridge />
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AuthProvider>
  );
}
