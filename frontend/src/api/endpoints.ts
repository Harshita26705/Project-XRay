import { get, post, put } from './client';
import type {
  ProjectResponse,
  GraphResponse,
  IngestResponse,
  OverviewResponse,
  ChangeResponse,
  CreateChangeRequest,
  AnalysisResponse,
  AnalysisScopeRequest,
  AnalysisProgressResponse,
  EvidenceResponse,
  SecurityFindingResponse,
  ExplainResponse,
  ReportResponse,
  IntegrationConnectionResponse,
  AiConfigurationResponse,
  NotificationChannelResponse,
  NotificationRuleResponse,
  OrganizationSettingsResponse,
  MeResponse
} from './types';

export const api = {
  me: () => get<MeResponse>('/me'),

  listProjects: () => get<ProjectResponse[]>('/projects'),
  getProject: (projectId: string) => get<ProjectResponse>(`/projects/${projectId}`),
  createProject: (payload: { name: string; description?: string; externalProjectUrl?: string; localRepositoryPath?: string }) =>
    post<ProjectResponse>('/projects', payload),
  ingest: (projectId: string, localRepositoryPath?: string) =>
    post<IngestResponse>(`/projects/${projectId}/ingest`, { localRepositoryPath }),
  getGraph: (projectId: string) => get<GraphResponse>(`/projects/${projectId}/graph`),
  getOverview: () => get<OverviewResponse>('/projects/overview'),

  listChanges: (projectId: string) => get<ChangeResponse[]>(`/projects/${projectId}/changes`),
  getChange: (changeId: string) => get<ChangeResponse>(`/changes/${changeId}`),
  createChange: (projectId: string, payload: CreateChangeRequest) => post<ChangeResponse>(`/projects/${projectId}/changes`, payload),

  createAnalysis: (changeId: string, scope?: AnalysisScopeRequest) => post<AnalysisResponse>('/analyses', { changeId, scope }),
  getAnalysis: (analysisId: string) => get<AnalysisResponse>(`/analyses/${analysisId}`),
  listAnalyses: (projectId: string) => get<AnalysisResponse[]>(`/projects/${projectId}/analyses`),
  getAnalysisProgress: (analysisId: string) => get<AnalysisProgressResponse>(`/analyses/${analysisId}/progress`),
  getEvidence: (analysisId: string) => get<EvidenceResponse[]>(`/analyses/${analysisId}/evidence`),
  getAnalysisSecurityFindings: (analysisId: string) => get<SecurityFindingResponse[]>(`/analyses/${analysisId}/security-findings`),
  explain: (analysisId: string, focus?: string) => post<ExplainResponse>(`/analyses/${analysisId}/explain`, { focus }),

  generateReport: (analysisId: string) => post<ReportResponse>(`/analyses/${analysisId}/reports`),
  getReport: (reportId: string) => get<ReportResponse>(`/reports/${reportId}`),
  listReports: (projectId: string) => get<ReportResponse[]>(`/projects/${projectId}/reports`),

  getSecurityFindings: (projectId: string) => get<SecurityFindingResponse[]>(`/projects/${projectId}/security-findings`),

  listIntegrations: () => get<IntegrationConnectionResponse[]>('/integrations'),
  upsertIntegration: (payload: { provider: string; displayName: string; externalBaseUrl?: string; externalTenantId?: string }) =>
    post<IntegrationConnectionResponse>('/integrations', payload),
  testIntegration: (connectionId: string) => post<IntegrationConnectionResponse>(`/integrations/${connectionId}/test`),

  getAiConfiguration: () => get<AiConfigurationResponse>('/ai/configuration'),

  listNotificationChannels: () => get<NotificationChannelResponse[]>('/notifications/channels'),
  listNotificationRules: () => get<NotificationRuleResponse[]>('/notifications/rules'),
  updateNotificationRule: (ruleId: string, isEnabled: boolean) =>
    put<NotificationRuleResponse>(`/notifications/rules/${ruleId}`, { isEnabled }),

  getSettings: () => get<OrganizationSettingsResponse>('/settings'),
  updateSettings: (payload: OrganizationSettingsResponse) => put<OrganizationSettingsResponse>('/settings', payload)
};
