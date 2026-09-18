// Typed contracts mirroring backend/XRay.Api/Contracts/*.cs

export interface ProjectResponse {
  projectId: string;
  name: string;
  description: string | null;
  isActive: boolean;
  repositoryReference: string | null;
  connectionStatus: 'CONNECTED' | 'NOT_CONNECTED';
  nodeCount: number;
  relationshipCount: number;
  securityFindingCount: number;
  lastIndexedAtUtc: string | null;
  technologyTags: string[];
}

export interface BranchResponse {
  name: string;
  headCommitSha: string | null;
  isDefault: boolean;
  isIndexed: boolean;
}

export interface GraphNodeResponse {
  nodeId: string;
  externalKey: string;
  componentType: string;
  displayName: string;
  filePath: string | null;
  isParsed: boolean;
}

export interface GraphEdgeResponse {
  edgeId: string;
  sourceNodeId: string;
  targetNodeId: string;
  edgeType: string;
  confidence: number;
  isRuntimeResolved: boolean;
}

export interface GraphResponse {
  snapshotId: string | null;
  nodes: GraphNodeResponse[];
  edges: GraphEdgeResponse[];
}

export interface NodeConnectionDetail {
  neighborNodeId: string;
  neighborName: string;
  edgeType: string;
  confidence: number;
  direction: 'incoming' | 'outgoing';
}

export interface ProjectNodeDetailResponse {
  nodeId: string;
  externalKey: string;
  componentType: string;
  displayName: string;
  filePath: string | null;
  isParsed: boolean;
  contains: string[];
  incoming: NodeConnectionDetail[];
  outgoing: NodeConnectionDetail[];
  whyThisMatters: string | null;
}

export interface ProjectSecurityScanResponse {
  securityScanId: string;
  projectId: string;
  findingCount: number;
  status: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
}

export interface IngestResponse {
  ingestionRunId: string;
  filesDiscovered: number;
  filesParsed: number;
  filesFailed: number;
  nodesCreated: number;
  edgesCreated: number;
  errors: string[];
}

export interface AzureDevOpsRepositoryResponse {
  id: string;
  name: string;
  webUrl: string | null;
  defaultBranch: string | null;
}

export interface AzureDevOpsBranchResponse {
  name: string;
  objectId: string | null;
  isDefault: boolean;
}

export interface RecentAnalysisResponse {
  analysisId: string;
  changeTitle: string;
  changeType: string;
  riskState: string | null;
  affectedComponents: number;
  createdAtUtc: string;
  status: string;
}

export interface OverviewResponse {
  totalAnalyses: number;
  criticalChanges: number;
  componentsAnalyzed: number;
  securityFindings: number;
  recentAnalyses: RecentAnalysisResponse[];
}

export type ChangeType = 'PULL_REQUEST' | 'WORK_ITEM' | 'MANUAL';

export interface ChangeResponse {
  changeId: string;
  changeType: ChangeType;
  title: string;
  description: string | null;
  author: string | null;
  createdAtUtc: string;
  riskState: string | null;
  affectedComponents: number;
  status: string;
  changedFilePaths: string[];
}

export interface CreateChangeRequest {
  changeType: ChangeType;
  title: string;
  description?: string;
  externalChangeId?: string;
  changedFilePaths: string[];
}

export interface AnalysisScopeRequest {
  scanDirectDependencies: boolean;
  traceTransitiveDependencies: boolean;
  includeExternalBindings: boolean;
  runStaticSecurityAnalysis: boolean;
}

export interface AnalysisImpactEdgeResponse {
  sourceNodeId: string;
  targetNodeId: string;
  edgeType: string;
  confidence: number;
}

export interface AnalysisNodeResultResponse {
  graphNodeId: string;
  displayName: string;
  componentType: string;
  riskState: string;
  distance: number | null;
  minPathConfidence: number | null;
  ruleCode: string | null;
  isDirectlyChanged: boolean;
  recommendation: string | null;
}

export interface AnalysisResponse {
  analysisId: string;
  changeId: string;
  changeTitle: string;
  status: string;
  overallRiskState: string | null;
  branchName: string | null;
  criticalCount: number;
  riskyCount: number;
  safeCount: number;
  unknownCount: number;
  createdAtUtc: string;
  completedAtUtc: string | null;
  edges: AnalysisImpactEdgeResponse[];
  nodes: AnalysisNodeResultResponse[];
}

export interface AnalysisProgressResponse {
  statusCode: string;
}

export interface EvidenceResponse {
  evidenceId: string;
  evidenceType: string;
  title: string;
  description: string | null;
  filePath: string | null;
  lineStart: number | null;
  lineEnd: number | null;
  confidence: number | null;
  sourceSnippet: string | null;
}

export interface SecurityFindingResponse {
  securityFindingId: string;
  title: string;
  severity: string;
  component: string | null;
  filePath: string | null;
  lineStart: number | null;
  status: string;
  description: string | null;
  remediation: string | null;
}

export interface ExplainResponse {
  summary: string;
  keyPoints: string[];
  degraded: boolean;
}

export interface NodeExplainResponse {
  summary: string;
  degraded: boolean;
}

export interface ReportSectionResponse {
  sectionType: string;
  title: string;
  content: string | null;
  sortOrder: number;
}

export interface ReportResponse {
  reportId: string;
  analysisId: string;
  title: string;
  format: string;
  createdAtUtc: string;
  overallRiskState: string | null;
  sections: ReportSectionResponse[];
}

export interface IntegrationConnectionResponse {
  integrationConnectionId: string;
  provider: string;
  displayName: string;
  status: string;
  lastTestedAtUtc: string | null;
  isEnabled: boolean;
  externalBaseUrl: string | null;
  hasPersonalAccessToken: boolean;
}

export interface AzureDevOpsWorkItemResponse {
  id: number;
  title: string;
  workItemType: string | null;
  state: string | null;
  assignedTo: string | null;
  description: string | null;
}

export interface AzureDevOpsPullRequestResponse {
  id: number;
  title: string;
  description: string | null;
  status: string | null;
  createdBy: string | null;
  sourceBranch: string | null;
  targetBranch: string | null;
  changedFilePaths: string[];
}

export interface AiConfigurationResponse {
  provider: string;
  deploymentStatus: string;
  activeModel: string | null;
  knowledgeLayer: string;
}

export interface UpdateAiConfigurationRequest {
  provider: string;
  activeModel: string | null;
  temperature: number | null;
  maxTokens: number | null;
  systemPromptOverride: string | null;
}

export interface NotificationChannelResponse {
  notificationChannelId: string;
  channelType: string;
  displayName: string;
  isEnabled: boolean;
}

export interface NotificationRuleResponse {
  notificationRuleId: string;
  eventType: string;
  channelId: string;
  channelType: string;
  isEnabled: boolean;
}

export interface OrganizationSettingsResponse {
  organizationName: string;
  defaultTargetProjectId: string | null;
  includeSecurityScan: boolean;
  includeAiExplanation: boolean;
  autoAnalyzePullRequests: boolean;
}

export interface MeResponse {
  userId: string;
  displayName: string;
  email: string;
  organizationId: string;
  organizationName: string;
}
