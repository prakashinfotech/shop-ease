import axios from 'axios'
import { API_BASE_URL, API_ENDPOINTS } from '@/constants/api'
import { useAuthStore } from '@/store/authStore'

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  paramsSerializer: {
    serialize: (params) => {
      const sp = new URLSearchParams()
      for (const [key, val] of Object.entries(params)) {
        if (val === undefined || val === null || val === '') continue
        if (Array.isArray(val)) {
          val.forEach((v) => sp.append(key, v))
        } else if (typeof val === 'object') {
          for (const [k, v] of Object.entries(val)) {
            if (v !== undefined && v !== null) sp.append(`${key}[${k}]`, v)
          }
        } else {
          sp.append(key, val)
        }
      }
      return sp.toString()
    },
  },
})

// Request interceptor — attach access token
api.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().accessToken
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    if (config.data instanceof FormData) {
      delete config.headers['Content-Type']
    }
    return config
  },
  (error) => Promise.reject(error)
)

let isRefreshing = false
let failedQueue = []

const processQueue = (error, token = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error)
    else resolve(token)
  })
  failedQueue = []
}

// Response interceptor — handle 401 with token refresh
api.interceptors.response.use(
  (response) => response.data,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            return api(originalRequest)
          })
          .catch((err) => Promise.reject(err))
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const { refreshToken, setTokens, clearAuth } = useAuthStore.getState()
        if (!refreshToken) {
          clearAuth()
          window.location.href = '/login'
          return Promise.reject(error)
        }

        const response = await axios.post(`${API_BASE_URL}${API_ENDPOINTS.AUTH.REFRESH}`, {
          refreshToken,
        })
        const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data.data
        setTokens(newAccessToken, newRefreshToken)
        processQueue(null, newAccessToken)
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`
        return api(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError, null)
        useAuthStore.getState().clearAuth()
        window.location.href = '/login'
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(error)
  }
)

export default api
