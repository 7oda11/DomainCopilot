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
  inputTokens?: number | null;
  outputTokens?: number | null;
  estimatedCostUsd?: number | null;
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

export interface WorkspaceCase {
  caseId: string;
  summary: string;
  createdAtUtc: string;
}

export interface WorkspaceDraft {
  caseId: string;
  runId: string;
  draftId?: string | null;
  caseSummary: string;
  question: string;
  draftContent?: string | null;
  status: string;
  createdAtUtc: string;
  decision?: string | null;
  decidedAtUtc?: string | null;
  reviewerId?: string | null;
  comment?: string | null;
  approvedContent?: string | null;
}

export interface WorkspaceDocument {
  documentId: string;
  fileName: string;
  format: string;
  version: string;
  status: string;
  uploadedAtUtc: string;
}

export interface WorkspaceDocumentContent {
  documentId: string;
  fileName: string;
  format: string;
  version: string;
  text: string;
}

export type AccountRole = 'Clinician' | 'Researcher' | string;

export interface JwtPayload {
  sub?: string;
  email?: string;
  role?: AccountRole | AccountRole[];
  exp?: number;
}
