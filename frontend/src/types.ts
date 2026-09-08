export type RiskState = 'RED' | 'YELLOW' | 'GREEN' | 'UNKNOWN';

export interface EvidenceStep {
  source: string;
  target: string;
  relationship: string;
  rule_id: string;
  confidence: number;
  source_file: string | null;
  line: number | null;
  dynamic: boolean;
}

export interface SecurityFinding {
  severity: string;
  type: string;
  file: string;
  line: number | null;
  description: string;
  source: string;
  node_id: string | null;
}

export interface NodeResult {
  node_id: string;
  node_type: string;
  name: string;
  state: RiskState;
  rule_applied: string;
  impact_type: string;
  distance: number | null;
  min_edge_confidence: number | null;
  parse_status: string;
  critical: boolean;
  path: EvidenceStep[];
  security_findings: SecurityFinding[];
  source_files: string[];
  explanation: string | null;
  explanation_source: string | null;
  flags: string[];
}

export interface RiskFactor {
  factor: string;
  points: number;
  detail: string | null;
}

export interface AnalysisResult {
  analysis_id: string;
  project_id: string;
  overall_state: RiskState;
  risk: { score: number; level: RiskState; factors: RiskFactor[] };
  confidence: number;
  changed_node_ids: string[];
  unmapped_files: string[];
  nodes: NodeResult[];
  security: {
    status: string;
    findings: SecurityFinding[];
    scanners_run: string[];
    scanners_unavailable: string[];
  };
  explanation: {
    summary: string;
    recommendations: string[];
    missing_information: string[];
    backend: string;
  };
  statuses: Record<string, string>;
  notes: string[];
  change: { title?: string; source?: string; files?: { path: string }[] };
}

export interface GraphEdge {
  source: string;
  target: string;
  relationship: string;
  rule_id: string;
  confidence: number;
  source_file: string | null;
  line: number | null;
  dynamic: boolean;
}

export interface GraphPayload {
  nodes: { id: string; type: string; name: string }[];
  edges: GraphEdge[];
}
