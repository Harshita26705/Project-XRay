import { del, download, get, post, put } from './client';
import type {
  ProjectResponse,
  BranchResponse,
  GraphResponse,
  IngestResponse,
  AzureDevOpsRepositoryResponse,
  AzureDevOpsBranchResponse,
  OverviewResponse,
  ChangeResponse,
  CreateChangeRequest,
  AnalysisResponse,
  AnalysisScopeRequest,
  AnalysisProgressResponse,
  EvidenceResponse,
  SecurityFindingResponse,
  ExplainResponse,
  ProjectNodeDetailResponse,
  ProjectSecurityScanResponse,
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
  updateProject: (projectId: string, payload: { name: string; description: string | null }) => put<ProjectResponse>(`/projects/${projectId}`, payload),
  deleteProject: (projectId: string) => del(`/projects/${projectId}`),
  createProject: (payload: { name: string; description?: string; externalProjectUrl?: string; localRepositoryPath?: string }) =>
    post<ProjectResponse>('/projects', payload),
  listBranches: (projectId: string) => get<BranchResponse[]>(`/projects/${projectId}/branches`),
  ingest: (projectId: string, localRepositoryPath?: string, branchName?: string) =>
    post<IngestResponse>(`/projects/${projectId}/ingest`, { localRepositoryPath, branchName }),
  getGraph: (projectId: string, branchName?: string) =>
    get<GraphResponse>(`/projects/${projectId}/graph${branchName ? `?branch=${encodeURIComponent(branchName)}` : ''}`),
  getGraphNodeDetail: (projectId: string, nodeId: string) => get<ProjectNodeDetailResponse>(`/projects/${projectId}/graph/nodes/${nodeId}`),
  getOverview: () => get<OverviewResponse>('/projects/overview'),
  listAzureDevOpsRepositories: () => get<AzureDevOpsRepositoryResponse[]>('/azure-devops/repositories'),
  listAzureDevOpsBranches: (repositoryId: string) => get<AzureDevOpsBranchResponse[]>(`/azure-devops/repositories/${encodeURIComponent(repositoryId)}/branches`),

  listChanges: (projectId: string) => get<ChangeResponse[]>(`/projects/${projectId}/changes`),
  getChange: (changeId: string) => get<ChangeResponse>(`/changes/${changeId}`),
  createChange: (projectId: string, payload: CreateChangeRequest) => post<ChangeResponse>(`/projects/${projectId}/changes`, payload),

  createAnalysis: (changeId: string, scope?: AnalysisScopeRequest, branchName?: string) => post<AnalysisResponse>('/analyses', { changeId, scope, branchName }),
  getAnalysis: (analysisId: string) => get<AnalysisResponse>(`/analyses/${analysisId}`),
  listAnalyses: (projectId: string) => get<AnalysisResponse[]>(`/projects/${projectId}/analyses`),
  getAnalysisProgress: (analysisId: string) => get<AnalysisProgressResponse>(`/analyses/${analysisId}/progress`),
  getEvidence: (analysisId: string) => get<EvidenceResponse[]>(`/analyses/${analysisId}/evidence`),
  getAnalysisSecurityFindings: (analysisId: string) => get<SecurityFindingResponse[]>(`/analyses/${analysisId}/security-findings`),
  explain: (analysisId: string, focus?: string) => post<ExplainResponse>(`/analyses/${analysisId}/explain`, { focus }),

  generateReport: (analysisId: string) => post<ReportResponse>(`/analyses/${analysisId}/reports`),
  getReport: (reportId: string) => get<ReportResponse>(`/reports/${reportId}`),
  downloadReport: (reportId: string, format: 'pdf' | 'xlsx') => download(`/reports/${reportId}/export?format=${format}`),
  listReports: (projectId: string) => get<ReportResponse[]>(`/projects/${projectId}/reports`),

  getSecurityFindings: (projectId: string) => get<SecurityFindingResponse[]>(`/projects/${projectId}/security-findings`),
  runProjectSecurityScan: (projectId: string) => post<ProjectSecurityScanResponse>(`/projects/${projectId}/security-scans`),

  listIntegrations: () => get<IntegrationConnectionResponse[]>('/integrations'),
  upsertIntegration: (payload: { provider: string; displayName: string; externalBaseUrl?: string; externalTenantId?: string }) =>
    post<IntegrationConnectionResponse>('/integrations', payload),
  testIntegration: (connectionId: string) => post<IntegrationConnectionResponse>(`/integrations/${connectionId}/test`),

  getAiConfiguration: () => get<AiConfigurationResponse>('/ai/configuration'),
  updateAiConfiguration: (payload: { provider: string; activeModel: string | null; temperature: number | null; maxTokens: number | null; systemPromptOverride: string | null }) =>
    put<AiConfigurationResponse>('/ai/configuration', payload),

  listNotificationChannels: () => get<NotificationChannelResponse[]>('/notifications/channels'),
  listNotificationRules: () => get<NotificationRuleResponse[]>('/notifications/rules'),
  updateNotificationRule: (ruleId: string, isEnabled: boolean) =>
    put<NotificationRuleResponse>(`/notifications/rules/${ruleId}`, { isEnabled }),

  getSettings: () => get<OrganizationSettingsResponse>('/settings'),
  updateSettings: (payload: OrganizationSettingsResponse) => put<OrganizationSettingsResponse>('/settings', payload)
};
