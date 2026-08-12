import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'

export const auctionService = {
  getActive: (page = 1, pageSize = 12) =>
    api.get(API_ENDPOINTS.AUCTIONS.ACTIVE, { params: { page, pageSize } }),

  getStatus: (listingId) =>
    api.get(API_ENDPOINTS.AUCTIONS.STATUS(listingId)),

  getBids: (listingId, page = 1, pageSize = 20) =>
    api.get(API_ENDPOINTS.AUCTIONS.BIDS(listingId), { params: { page, pageSize } }),

  placeBid: (listingId, amount) =>
    api.post(API_ENDPOINTS.AUCTIONS.PLACE_BID(listingId), { amount }),

  getMyBids: (page = 1, pageSize = 20) =>
    api.get(API_ENDPOINTS.AUCTIONS.MY_BIDS, { params: { page, pageSize } }),

  // Admin
  adminGetAuctions: (page = 1, pageSize = 15, status) =>
    api.get(API_ENDPOINTS.AUCTIONS.ADMIN_LIST, { params: { page, pageSize, status } }),

  adminGetBids: (listingId, page = 1, pageSize = 50) =>
    api.get(API_ENDPOINTS.AUCTIONS.ADMIN_BIDS(listingId), { params: { page, pageSize } }),

  adminCancel: (listingId) =>
    api.post(API_ENDPOINTS.AUCTIONS.ADMIN_CANCEL(listingId)),

  adminExtend: (listingId, minutes) =>
    api.post(API_ENDPOINTS.AUCTIONS.ADMIN_EXTEND(listingId), { minutes }),
}
