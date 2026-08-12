import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'

export const listingService = {
  getAll: (params) => api.get(API_ENDPOINTS.LISTINGS.BASE, { params }),
  getById: (id) => api.get(API_ENDPOINTS.LISTINGS.BY_ID(id)),
  getMyListings: (params) => api.get(API_ENDPOINTS.LISTINGS.MY, { params }),
  getRecentlyViewed: (params) => api.get(API_ENDPOINTS.LISTINGS.RECENTLY_VIEWED, { params }),
  recordView: (id) => api.post(API_ENDPOINTS.LISTINGS.VIEW(id)),
  create: (data) => api.post(API_ENDPOINTS.LISTINGS.BASE, data),
  update: (id, data) => api.put(API_ENDPOINTS.LISTINGS.BY_ID(id), data),
  delete: (id) => api.delete(API_ENDPOINTS.LISTINGS.BY_ID(id)),
  restore: (id) => api.patch(API_ENDPOINTS.LISTINGS.RESTORE(id)),
  uploadImage: (file) => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post(API_ENDPOINTS.LISTINGS.IMAGES, formData)
  },
  autocomplete: (q) => api.get(API_ENDPOINTS.LISTINGS.AUTOCOMPLETE, { params: { q } }),
  getFacets: (params) => api.get(API_ENDPOINTS.LISTINGS.FACETS, { params }),
}
