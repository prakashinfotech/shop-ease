import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'

export const businessProfileService = {
  get: () => api.get(API_ENDPOINTS.BUSINESS_PROFILE.BASE),

  submit: (data) => api.post(API_ENDPOINTS.BUSINESS_PROFILE.BASE, data),

  update: (data) => api.put(API_ENDPOINTS.BUSINESS_PROFILE.BASE, data),

  uploadDocument: (file, documentType) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('documentType', documentType)
    return api.post(API_ENDPOINTS.BUSINESS_PROFILE.DOCUMENTS, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  submitForReview: () =>
    api.post(API_ENDPOINTS.BUSINESS_PROFILE.SUBMIT_FOR_REVIEW),

  deleteDocument: (documentId) =>
    api.delete(API_ENDPOINTS.BUSINESS_PROFILE.DOCUMENT_BY_ID(documentId)),

  // Admin
  adminGetAll: (params) =>
    api.get(API_ENDPOINTS.ADMIN.BUSINESS_PROFILES, { params }),

  adminReview: (id, data) =>
    api.put(API_ENDPOINTS.ADMIN.BUSINESS_PROFILE_REVIEW(id), data),
}
