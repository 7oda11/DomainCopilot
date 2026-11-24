export interface AuthResult {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
}

export interface RegisteredUser {
  id: string;
  email: string;
  role: string;
}

export interface Evidence {
  documentId: string;
  chunkId: string;
  sourceName: string;
  sourceVersion: string;
  section?: string | null;
  pageNumber?: number | null;
  quotedText: string;
  score: number;
}

export interface SearchResponse {
  refused: boolean;
  refusalReason?: string | null;
  evidence: Evidence[];
}

export interface UploadResponse {
  documentId: string;
  alreadyIndexed: boolean;
  chunkCount: number;
}

export interface CreatedCase {
  caseId: string;
  createdAtUtc: string;
}

export interface AgentTrace {
  agentName: string;
  outcome: string;
  providerId: string;
  isDegraded: boolean;
  elapsedMilliseconds: number;
}

export interface WorkflowProgress extends AgentTrace {}

export interface ApiProblem {
  status?: number;
  title?: string;
  detail?: string;
}

export type WorkflowStreamEvent =
  | { type: 'progress'; progress: WorkflowProgress }
  | { type: 'completed'; result: WorkflowResponse }
  | { type: 'error'; problem: ApiProblem };

export interface WorkflowResponse {
  runId: string;
  draftId?: string | null;
  status: string;
  draft?: string | null;
  refused: boolean;
  refusalReason?: string | null;
  requiresClinicianApproval: boolean;
  activeProvider: string;
  isDegraded: boolean;
  evidence: Evidence[];
  trace: AgentTrace[];
}

export interface ReviewResponse {
  runId: string;
  draftId: string;
  action: string;
  status: string;
  approvedContent?: string | null;
}

export type AccountRole = 'Clinician' | 'Researcher' | string;

export interface JwtPayload {
  sub?: string;
  email?: string;
  role?: AccountRole | AccountRole[];
  exp?: number;
}
