// DTOs matching .NET Core API models
// These should mirror your C# DTO classes

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface CaseDto {
  id: string;
  applicantName: string;
  serviceType: string;
  status: string;
  priority: string;
  submittedDate: string;
  lastUpdated: string;
  assignedTo?: string;
  currentStage: string;
}

export interface CaseDetailDto extends CaseDto {
  documents: DocumentDto[];
  timeline: TimelineEventDto[];
  ruleEngineOutput: RuleEngineOutputDto[];
  metrics: CaseMetricsDto;
}

export interface CreateCaseDto {
  applicantName: string;
  serviceType: string;
  priority: string;
  documents?: File[];
}

export interface UpdateCaseDto {
  status?: string;
  priority?: string;
  assignedTo?: string;
  notes?: string;
}

export interface WorkflowDto {
  id: string;
  caseId: string;
  name: string;
  status: string;
  steps: WorkflowStepDto[];
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowStepDto {
  id: string;
  name: string;
  stage: string;
  status: string;
  assignedTo?: string;
  completedAt?: string;
  description?: string;
  order: number;
}

export interface TimelineEventDto {
  id: string;
  timestamp: string;
  action: string;
  user: string;
  details: string;
  metadata?: Record<string, unknown>;
}

export interface DocumentDto {
  id: string;
  name: string;
  type: string;
  uploadedAt: string;
  status: string;
  url?: string;
  size?: number;
}

export interface RuleDto {
  id: string;
  name: string;
  description: string;
  enabled: boolean;
  priority: number;
  conditions: RuleConditionDto[];
  actions: RuleActionDto[];
  createdAt: string;
  updatedAt: string;
}

export interface RuleConditionDto {
  id: string;
  field: string;
  operator: string;
  value: string;
}

export interface RuleActionDto {
  id: string;
  type: string;
  target: string;
  parameters?: Record<string, unknown>;
}

export interface RuleEngineOutputDto {
  ruleId: string;
  ruleName: string;
  matched: boolean;
  executedActions: string[];
  timestamp: string;
}

export interface CaseMetricsDto {
  timeElapsed: number;
  slaStatus: string;
  automationScore: number;
  daysRemaining?: number;
}

export interface DashboardMetricsDto {
  totalWorkflows: number;
  activeCases: number;
  pendingApprovals: number;
  completedProcesses: number;
  avgProcessingTime: number;
  successRate: number;
  processingData: ProcessingDataPoint[];
  statusDistribution: StatusDistribution[];
}

export interface ProcessingDataPoint {
  month: string;
  cases: number;
}

export interface StatusDistribution {
  name: string;
  value: number;
  color: string;
}

export interface ActivityDto {
  id: string;
  caseId: string;
  action: string;
  user: string;
  timestamp: string;
}

export interface LoginDto {
  username: string;
  password: string;
}

export interface AuthResponseDto {
  token: string;
  refreshToken: string;
  expiresIn: number;
  user: UserDto;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: string;
  permissions: string[];
}

export interface CaseFilterDto {
  status?: string;
  serviceType?: string;
  priority?: string;
  assignedTo?: string;
  dateFrom?: string;
  dateTo?: string;
  searchQuery?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface RuleTestResultDto {
  matched: boolean;
  executedActions: string[];
  summary: string;
}
