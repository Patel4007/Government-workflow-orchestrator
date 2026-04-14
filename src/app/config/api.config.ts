// API Configuration
// Update these values to match your .NET Core API endpoints

export const API_CONFIG = {
  // Base URL for your .NET Core API
  BASE_URL: import.meta.env.VITE_API_BASE_URL || '/api',
  
  // API Endpoints matching .NET Core Controllers
  ENDPOINTS: {
    // CasesController endpoints
    CASES: '/cases',
    CASE_BY_ID: (id: string) => `/cases/${id}`,
    CASE_APPROVE: (id: string) => `/cases/${id}/approve`,
    CASE_REJECT: (id: string) => `/cases/${id}/reject`,
    CASE_REQUEST_INFO: (id: string) => `/cases/${id}/request-info`,
    CASE_ASSIGN: (id: string) => `/cases/${id}/assign`,
    
    // WorkflowsController endpoints
    WORKFLOWS: '/workflows',
    WORKFLOW_BY_ID: (id: string) => `/workflows/${id}`,
    WORKFLOW_STEPS: (id: string) => `/workflows/${id}/steps`,
    WORKFLOW_ADVANCE: (id: string) => `/workflows/${id}/advance`,
    
    // RulesController endpoints
    RULES: '/rules',
    RULE_BY_ID: (id: string) => `/rules/${id}`,
    RULE_TOGGLE: (id: string) => `/rules/${id}/toggle`,
    RULE_TEST: (id: string) => `/rules/${id}/test`,
    
    // DocumentsController endpoints
    DOCUMENTS: '/documents',
    DOCUMENT_UPLOAD: '/documents/upload',
    DOCUMENT_BY_ID: (id: string) => `/documents/${id}`,
    
    // TimelineController endpoints
    TIMELINE: (caseId: string) => `/cases/${caseId}/timeline`,
    
    // DashboardController endpoints
    DASHBOARD_METRICS: '/dashboard/metrics',
    DASHBOARD_ACTIVITY: '/dashboard/activity',
    
    // ReportsController endpoints
    REPORTS: '/reports',
    REPORT_EXPORT: (type: string) => `/reports/export/${type}`,
    
    // AuthController endpoints
    AUTH_LOGIN: '/auth/login',
    AUTH_LOGOUT: '/auth/logout',
    AUTH_REFRESH: '/auth/refresh',
    AUTH_USER: '/auth/user',
  },
  
  // Request timeout
  TIMEOUT: 30000,
  
  // Enable mock mode for development without backend
  USE_MOCK_DATA: import.meta.env.VITE_USE_MOCK_DATA === 'true',
};

// HTTP Status Codes
export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  INTERNAL_SERVER_ERROR: 500,
};
