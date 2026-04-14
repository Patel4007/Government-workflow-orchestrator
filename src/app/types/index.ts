export type CaseStatus = 'pending' | 'in-progress' | 'approved' | 'rejected' | 'completed';
export type CasePriority = 'low' | 'medium' | 'high' | 'critical';
export type ServiceType = 'tax-filing' | 'benefit-approval' | 'document-verification' | 'license-renewal';
export type WorkflowStage = 'submission' | 'verification' | 'review' | 'approval' | 'completion';

export interface Case {
  id: string;
  applicantName: string;
  serviceType: ServiceType;
  status: CaseStatus;
  priority: CasePriority;
  submittedDate: string;
  lastUpdated: string;
  assignedTo?: string;
  currentStage: WorkflowStage;
}

export interface WorkflowStep {
  id: string;
  name: string;
  stage: WorkflowStage;
  status: CaseStatus;
  assignedTo?: string;
  completedAt?: string;
  description?: string;
}

export interface TimelineEvent {
  id: string;
  timestamp: string;
  action: string;
  user: string;
  details: string;
}

export interface Document {
  id: string;
  name: string;
  type: string;
  uploadedAt: string;
  status: 'verified' | 'pending' | 'rejected';
}

export interface Rule {
  id: string;
  name: string;
  description: string;
  enabled: boolean;
  conditions: RuleCondition[];
  actions: RuleAction[];
}

export interface RuleCondition {
  id: string;
  field: string;
  operator: string;
  value: string;
}

export interface RuleAction {
  id: string;
  type: string;
  target: string;
}
