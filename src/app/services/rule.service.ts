import { apiClient } from './api.client';
import { API_CONFIG } from '../config/api.config';
import type {
  ApiResponse,
  RuleDto,
  RuleTestResultDto,
} from '../types/api.types';
import { mockRules } from '../data/mockData';

export class RuleService {
  /**
   * Get all rules
   * Maps to: GET /api/rules
   */
  async getRules(): Promise<ApiResponse<RuleDto[]>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: mockRules as unknown as RuleDto[],
        success: true,
      };
    }

    return apiClient.get<ApiResponse<RuleDto[]>>(API_CONFIG.ENDPOINTS.RULES);
  }

  /**
   * Get rule by ID
   * Maps to: GET /api/rules/{id}
   */
  async getRuleById(id: string): Promise<ApiResponse<RuleDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      const rule = mockRules.find(r => r.id === id);
      if (!rule) throw new Error('Rule not found');
      return {
        data: rule as unknown as RuleDto,
        success: true,
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.RULE_BY_ID(id);
    return apiClient.get<ApiResponse<RuleDto>>(endpoint);
  }

  /**
   * Create new rule
   * Maps to: POST /api/rules
   */
  async createRule(data: Partial<RuleDto>): Promise<ApiResponse<RuleDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: { id: 'rule-new-001', ...data } as RuleDto,
        success: true,
        message: 'Rule created successfully (mock)',
      };
    }

    return apiClient.post<ApiResponse<RuleDto>>(
      API_CONFIG.ENDPOINTS.RULES,
      data
    );
  }

  /**
   * Update rule
   * Maps to: PUT /api/rules/{id}
   */
  async updateRule(id: string, data: Partial<RuleDto>): Promise<ApiResponse<RuleDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: { id, ...data } as RuleDto,
        success: true,
        message: 'Rule updated successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.RULE_BY_ID(id);
    return apiClient.put<ApiResponse<RuleDto>>(endpoint, data);
  }

  /**
   * Toggle rule enabled/disabled
   * Maps to: POST /api/rules/{id}/toggle
   */
  async toggleRule(id: string): Promise<ApiResponse<RuleDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      const rule = mockRules.find(r => r.id === id);
      if (!rule) {
        throw new Error('Rule not found');
      }

      return {
        data: {
          ...rule,
          enabled: !rule.enabled,
        } as unknown as RuleDto,
        success: true,
        message: 'Rule toggled successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.RULE_TOGGLE(id);
    return apiClient.post<ApiResponse<RuleDto>>(endpoint);
  }

  /**
   * Test rule against sample data
   * Maps to: POST /api/rules/{id}/test
   */
  async testRule(id: string, testData: unknown): Promise<ApiResponse<RuleTestResultDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: {
          matched: true,
          executedActions: ['test-action'],
          summary: 'Rule test completed (mock)',
        },
        success: true,
        message: 'Rule test completed (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.RULE_TEST(id);
    return apiClient.post<ApiResponse<RuleTestResultDto>>(endpoint, testData);
  }

  /**
   * Delete rule
   * Maps to: DELETE /api/rules/{id}
   */
  async deleteRule(id: string): Promise<ApiResponse<void>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: undefined as unknown as void,
        success: true,
        message: 'Rule deleted successfully (mock)',
      };
    }

    const endpoint = API_CONFIG.ENDPOINTS.RULE_BY_ID(id);
    return apiClient.delete<ApiResponse<void>>(endpoint);
  }
}

export const ruleService = new RuleService();
