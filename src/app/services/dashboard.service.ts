import { apiClient } from './api.client';
import { API_CONFIG } from '../config/api.config';
import type {
  ApiResponse,
  DashboardMetricsDto,
  ActivityDto,
} from '../types/api.types';
import { dashboardMetrics, recentActivity } from '../data/mockData';

export class DashboardService {
  /**
   * Get dashboard metrics
   * Maps to: GET /api/dashboard/metrics
   */
  async getMetrics(): Promise<ApiResponse<DashboardMetricsDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: dashboardMetrics as DashboardMetricsDto,
        success: true,
      };
    }

    return apiClient.get<ApiResponse<DashboardMetricsDto>>(
      API_CONFIG.ENDPOINTS.DASHBOARD_METRICS
    );
  }

  /**
   * Get recent activity
   * Maps to: GET /api/dashboard/activity
   */
  async getRecentActivity(): Promise<ApiResponse<ActivityDto[]>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: recentActivity as ActivityDto[],
        success: true,
      };
    }

    return apiClient.get<ApiResponse<ActivityDto[]>>(
      API_CONFIG.ENDPOINTS.DASHBOARD_ACTIVITY
    );
  }
}

export const dashboardService = new DashboardService();
