import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'

export const categoryService = {
  getAll: () => api.get(API_ENDPOINTS.CATEGORIES.BASE),
  getTree: () => api.get(API_ENDPOINTS.CATEGORIES.TREE),
  getById: (id) => api.get(API_ENDPOINTS.CATEGORIES.BY_ID(id)),
  getMetadata: (id) => api.get(API_ENDPOINTS.CATEGORIES.METADATA(id)),
  create: (data) => api.post(API_ENDPOINTS.CATEGORIES.BASE, data),
  update: (id, data) => api.put(API_ENDPOINTS.CATEGORIES.BY_ID(id), data),
  delete: (id) => api.delete(API_ENDPOINTS.CATEGORIES.BY_ID(id)),
  createAttribute: (categoryId, data) => api.post(API_ENDPOINTS.CATEGORIES.ATTRIBUTES(categoryId), data),
  updateAttribute: (categoryId, attributeId, data) =>
    api.put(API_ENDPOINTS.CATEGORIES.ATTRIBUTE_BY_ID(categoryId, attributeId), data),
  deleteAttribute: (categoryId, attributeId) =>
    api.delete(API_ENDPOINTS.CATEGORIES.ATTRIBUTE_BY_ID(categoryId, attributeId)),
}
