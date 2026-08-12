import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'

export const authService = {
  login: (data) => api.post(API_ENDPOINTS.AUTH.LOGIN, data),
  checkEmail: (email) => api.post(API_ENDPOINTS.AUTH.CHECK_EMAIL, { email }),
  register: (data) => api.post(API_ENDPOINTS.AUTH.REGISTER, data),
  logout: (data) => api.post(API_ENDPOINTS.AUTH.LOGOUT, data),
  refreshToken: (refreshToken) => api.post(API_ENDPOINTS.AUTH.REFRESH, { refreshToken }),
  getMe: () => api.get(API_ENDPOINTS.AUTH.ME),
  verifyEmail: (token) => api.post(API_ENDPOINTS.AUTH.VERIFY_EMAIL, { token }),
  resendVerification: () => api.post(API_ENDPOINTS.AUTH.RESEND_VERIFICATION),
  forgotPassword: (email) => api.post(API_ENDPOINTS.AUTH.FORGOT_PASSWORD, { email }),
  resetPassword: (data) => api.post(API_ENDPOINTS.AUTH.RESET_PASSWORD, data),
}
