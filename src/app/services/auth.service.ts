import { apiClient, tokenManager } from './api.client';
import { API_CONFIG } from '../config/api.config';
import type {
  ApiResponse,
  LoginDto,
  AuthResponseDto,
  UserDto,
} from '../types/api.types';

export class AuthService {
  /**
   * Login user
   * Maps to: POST /api/auth/login
   */
  async login(credentials: LoginDto): Promise<ApiResponse<AuthResponseDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      // Mock authentication
      const mockAuthResponse: AuthResponseDto = {
        token: 'mock-jwt-token-' + Date.now(),
        refreshToken: 'mock-refresh-token-' + Date.now(),
        expiresIn: 3600,
        user: {
          id: 'user-001',
          username: credentials.username,
          email: 'admin@govservices.gov',
          fullName: 'Admin User',
          role: 'Administrator',
          permissions: ['cases:read', 'cases:write', 'rules:manage', 'users:manage'],
        },
      };

      tokenManager.setToken(mockAuthResponse.token);
      tokenManager.setRefreshToken(mockAuthResponse.refreshToken);

      return {
        data: mockAuthResponse,
        success: true,
        message: 'Login successful (mock)',
      };
    }

    const response = await apiClient.post<ApiResponse<AuthResponseDto>>(
      API_CONFIG.ENDPOINTS.AUTH_LOGIN,
      credentials
    );

    if (response.success && response.data) {
      tokenManager.setToken(response.data.token);
      tokenManager.setRefreshToken(response.data.refreshToken);
    }

    return response;
  }

  /**
   * Logout user
   * Maps to: POST /api/auth/logout
   */
  async logout(): Promise<void> {
    if (!API_CONFIG.USE_MOCK_DATA) {
      try {
        await apiClient.post(API_CONFIG.ENDPOINTS.AUTH_LOGOUT);
      } catch (error) {
        console.error('Logout error:', error);
      }
    }

    tokenManager.clearTokens();
  }

  /**
   * Get current user
   * Maps to: GET /api/auth/user
   */
  async getCurrentUser(): Promise<ApiResponse<UserDto>> {
    if (API_CONFIG.USE_MOCK_DATA) {
      return {
        data: {
          id: 'user-001',
          username: 'admin',
          email: 'admin@govservices.gov',
          fullName: 'Admin User',
          role: 'Administrator',
          permissions: ['cases:read', 'cases:write', 'rules:manage', 'users:manage'],
        },
        success: true,
      };
    }

    return apiClient.get<ApiResponse<UserDto>>(
      API_CONFIG.ENDPOINTS.AUTH_USER
    );
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return tokenManager.getToken() !== null;
  }

  /**
   * Get current token
   */
  getToken(): string | null {
    return tokenManager.getToken();
  }
}

export const authService = new AuthService();
