import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'

export const orderService = {
  getAll: (params) => api.get(API_ENDPOINTS.ORDERS.BASE, { params }),
  getById: (id) => api.get(API_ENDPOINTS.ORDERS.BY_ID(id)),
  checkout: (data) => api.post(API_ENDPOINTS.ORDERS.CHECKOUT, data),
  cancel: (id) => api.post(API_ENDPOINTS.ORDERS.CANCEL(id)),
  getSellerOrders: (params) => api.get(API_ENDPOINTS.ORDERS.SELLER, { params }),
  updateSellerStatus: (id, data) => api.put(API_ENDPOINTS.ORDERS.SELLER_STATUS(id), data),
}
