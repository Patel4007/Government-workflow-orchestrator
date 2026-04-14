import { apiClient } from './api.client';
import { API_CONFIG } from '../config/api.config';
import type {
  ApiResponse,
  WorkflowDto,
} from '../types/api.types';
import { mockWorkflowSteps, mockCases } from '../data/mockData';

export class WorkflowService {
  async getWorkflows(): Promise<ApiResponse<WorkflowDto[]>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: [
          {
            id: 'workflow-001',
            caseId: mockCases[0]?.id ?? 'CASE-2024-001',
            name: `Tax Filing Workflow - ${mockCases[0]?.id ?? 'CASE-2024-001'}`,
            status: 'in-progress',
            steps: mockWorkflowSteps.map((step, index) => ({
              ...step,
              order: index + 1,
            })),
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          },
        ] as WorkflowDto[],
        success: true,
      };
    }

    return apiClient.get<ApiResponse<WorkflowDto[]>>(API_CONFIG.ENDPOINTS.WORKFLOWS);
  }

  async getWorkflowById(id: string): Promise<ApiResponse<WorkflowDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      const workflows = await this.getWorkflows();
      const workflow = workflows.data.find((item) => item.id === id);
      if (!workflow) {
        throw new Error('Workflow not found');
      }

      return {
        data: workflow,
        success: true,
      };
    }

    return apiClient.get<ApiResponse<WorkflowDto>>(API_CONFIG.ENDPOINTS.WORKFLOW_BY_ID(id));
  }

  async advanceWorkflow(id: string, notes?: string): Promise<ApiResponse<WorkflowDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      const workflow = await this.getWorkflowById(id);
      return {
        data: workflow.data,
        success: true,
        message: 'Workflow advanced successfully (mock)',
      };
    }

    return apiClient.post<ApiResponse<WorkflowDto>>(
      API_CONFIG.ENDPOINTS.WORKFLOW_ADVANCE(id),
      { notes }
    );
  }
}

export const workflowService = new WorkflowService();
