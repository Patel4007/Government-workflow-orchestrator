import { apiClient } from './api.client';
import { API_CONFIG } from '../config/api.config';
import type {
  ApiResponse,
  PaginatedResponse,
  CaseDto,
  CaseDetailDto,
  CreateCaseDto,
  UpdateCaseDto,
  CaseFilterDto,
} from '../types/api.types';

// Mock data fallback
import { mockCases, mockDocuments, mockTimelineEvents } from '../data/mockData';

export class CaseService {
  /**
   * Get all cases with optional filtering and pagination
   * Maps to: GET /api/cases
   */
  async getCases(filters?: CaseFilterDto): Promise<PaginatedResponse<CaseDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      // Mock data fallback
      return {
        items: mockCases as unknown as CaseDto[],
        totalCount: mockCases.length,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 1,
      };
    }

    const queryParams = new URLSearchParams();
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          queryParams.append(key, String(value));
        }
      });
    }

    const endpoint = `${API_CONFIG.ENDPOINTS.CASES}?${queryParams.toString()}`;
    return apiClient.get<PaginatedResponse<CaseDto>>(endpoint);
  }

  /**
   * Get case by ID
   * Maps to: GET /api/cases/{id}
   */
  async getCaseById(id: string): Promise<ApiResponse<CaseDetailDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      const caseData = mockCases.find(c => c.id === id);
      if (!caseData) {
        throw new Error('Case not found');
      }
      return {
        data: {
          ...caseData,
          documents: mockDocuments as unknown as CaseDetailDto['documents'],
          timeline: mockTimelineEvents as unknown as CaseDetailDto['timeline'],
          ruleEngineOutput: [],
          metrics: {
            timeElapsed: 7,
            slaStatus: 'Within SLA',
            automationScore: 76,
            daysRemaining: 23,
          },
        } as CaseDetailDto,
        success: true,
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_BY_ID(id);
    return apiClient.get<ApiResponse<CaseDetailDto>>(endpoint);
  }

  /**
   * Create new case
   * Maps to: POST /api/cases
   */
  async createCase(data: CreateCaseDto): Promise<ApiResponse<CaseDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: { id: 'CASE-NEW-001', ...data } as unknown as CaseDto,
        success: true,
        message: 'Case created successfully (mock)',
      };
    }

    return apiClient.post<ApiResponse<CaseDto>>(
      API_CONFIG.ENDPOINTS.CASES,
      data
    );
  }

  /**
   * Update case
   * Maps to: PUT /api/cases/{id}
   */
  async updateCase(id: string, data: UpdateCaseDto): Promise<ApiResponse<CaseDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: { id, ...data } as unknown as CaseDto,
        success: true,
        message: 'Case updated successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_BY_ID(id);
    return apiClient.put<ApiResponse<CaseDto>>(endpoint, data);
  }

  /**
   * Approve case
   * Maps to: POST /api/cases/{id}/approve
   */
  async approveCase(id: string, notes?: string): Promise<ApiResponse<void>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: undefined as unknown as void,
        success: true,
        message: 'Case approved successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_APPROVE(id);
    return apiClient.post<ApiResponse<void>>(endpoint, { notes });
  }

  /**
   * Reject case
   * Maps to: POST /api/cases/{id}/reject
   */
  async rejectCase(id: string, reason: string): Promise<ApiResponse<void>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: undefined as unknown as void,
        success: true,
        message: 'Case rejected successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_REJECT(id);
    return apiClient.post<ApiResponse<void>>(endpoint, { reason });
  }

  /**
   * Request additional information
   * Maps to: POST /api/cases/{id}/request-info
   */
  async requestInfo(id: string, message: string): Promise<ApiResponse<void>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: undefined as unknown as void,
        success: true,
        message: 'Information requested successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_REQUEST_INFO(id);
    return apiClient.post<ApiResponse<void>>(endpoint, { message });
  }

  /**
   * Assign case to user/team
   * Maps to: POST /api/cases/{id}/assign
   */
  async assignCase(id: string, assignTo: string): Promise<ApiResponse<void>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: undefined as unknown as void,
        success: true,
        message: 'Case assigned successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_ASSIGN(id);
    return apiClient.post<ApiResponse<void>>(endpoint, { assignTo });
  }

  /**
   * Delete case
   * Maps to: DELETE /api/cases/{id}
   */
  async deleteCase(id: string): Promise<ApiResponse<void>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: undefined as unknown as void,
        success: true,
        message: 'Case deleted successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.CASE_BY_ID(id);
    return apiClient.delete<ApiResponse<void>>(endpoint);
  }
}

export const caseService = new CaseService();
