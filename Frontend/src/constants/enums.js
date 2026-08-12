export const AccountType = {
  PERSONAL: 0,
  BUSINESS: 1,
}

export const AccountTypeLabel = {
  [AccountType.PERSONAL]: 'Personal',
  [AccountType.BUSINESS]: 'Business',
}

export const ListingStatus = {
  DRAFT: 0,
  ACTIVE: 1,
  SOLD: 2,
  ENDED: 3,
  REMOVED: 4,
  PENDING_APPROVAL: 5,
  REJECTED: 6,
}

export const ListingStatusLabel = {
  [ListingStatus.DRAFT]: 'Draft',
  [ListingStatus.ACTIVE]: 'Active',
  [ListingStatus.SOLD]: 'Sold',
  [ListingStatus.ENDED]: 'Ended',
  [ListingStatus.REMOVED]: 'Removed',
  [ListingStatus.PENDING_APPROVAL]: 'Pending Approval',
  [ListingStatus.REJECTED]: 'Rejected',
}

export const ListingVersionStatus = {
  PENDING_APPROVAL: 0,
  APPROVED: 1,
  REJECTED: 2,
}

export const ListingVersionStatusLabel = {
  [ListingVersionStatus.PENDING_APPROVAL]: 'Pending Approval',
  [ListingVersionStatus.APPROVED]: 'Approved',
  [ListingVersionStatus.REJECTED]: 'Rejected',
}

export const EmailTemplateType = {
  EMAIL_VERIFICATION: 0,
  FORGOT_PASSWORD: 1,
  PASSWORD_CHANGED: 2,
  LISTING_PENDING_APPROVAL: 3,
  LISTING_APPROVED: 4,
  LISTING_REJECTED: 5,
}

export const EmailTemplateTypeLabel = {
  [EmailTemplateType.EMAIL_VERIFICATION]: 'Email Verification',
  [EmailTemplateType.FORGOT_PASSWORD]: 'Forgot Password',
  [EmailTemplateType.PASSWORD_CHANGED]: 'Password Changed',
  [EmailTemplateType.LISTING_PENDING_APPROVAL]: 'Listing Pending Approval',
  [EmailTemplateType.LISTING_APPROVED]: 'Listing Approved',
  [EmailTemplateType.LISTING_REJECTED]: 'Listing Rejected',
}

export const ListingType = {
  FIXED_PRICE: 0,
  AUCTION: 1,
}

export const ListingTypeLabel = {
  [ListingType.FIXED_PRICE]: 'Fixed Price',
  [ListingType.AUCTION]: 'Auction',
}

export const AttributeDataType = {
  TEXT: 0,
  NUMBER: 1,
  DECIMAL: 2,
  BOOLEAN: 3,
  DATE: 4,
  DROPDOWN: 5,
  MULTI_SELECT: 6,
}

export const OrderStatus = {
  PENDING: 0,
  CONFIRMED: 1,
  SHIPPED: 2,
  DELIVERED: 3,
  CANCELLED: 4,
  REFUNDED: 5,
}

export const OrderStatusLabel = {
  [OrderStatus.PENDING]: 'Pending',
  [OrderStatus.CONFIRMED]: 'Confirmed',
  [OrderStatus.SHIPPED]: 'Shipped',
  [OrderStatus.DELIVERED]: 'Delivered',
  [OrderStatus.CANCELLED]: 'Cancelled',
  [OrderStatus.REFUNDED]: 'Refunded',
}

export const PaymentStatus = {
  PENDING: 0,
  PAID: 1,
  FAILED: 2,
  REFUNDED: 3,
}

export const PaymentStatusLabel = {
  [PaymentStatus.PENDING]: 'Payment Pending',
  [PaymentStatus.PAID]: 'Paid',
  [PaymentStatus.FAILED]: 'Failed',
  [PaymentStatus.REFUNDED]: 'Refunded',
}

export const UserRole = {
  USER: 'User',
  ADMIN: 'Admin',
}

export const VerificationStatus = {
  PENDING: 'Pending',
  UNDER_REVIEW: 'UnderReview',
  VERIFIED: 'Verified',
  REJECTED: 'Rejected',
  DRAFT: 'Draft',
}

export const VerificationStatusLabel = {
  [VerificationStatus.PENDING]: 'Pending Review',
  [VerificationStatus.UNDER_REVIEW]: 'Under Review',
  [VerificationStatus.VERIFIED]: 'Verified',
  [VerificationStatus.REJECTED]: 'Rejected',
  [VerificationStatus.DRAFT]: 'Draft',
}

export const DocumentType = {
  GST_CERTIFICATE: 0,
  PAN_CARD: 1,
  BUSINESS_REGISTRATION: 2,
  ADDRESS_PROOF: 3,
  OTHER: 4,
}

export const DocumentTypeLabel = {
  [DocumentType.GST_CERTIFICATE]: 'GST Certificate',
  [DocumentType.PAN_CARD]: 'PAN Card',
  [DocumentType.BUSINESS_REGISTRATION]: 'Business Registration',
  [DocumentType.ADDRESS_PROOF]: 'Address Proof',
  [DocumentType.OTHER]: 'Other Document',
}

export const AuctionEndReason = {
  TIME_EXPIRED: 0,
  ADMIN_CANCELLED: 1,
  SELLER_CANCELLED: 2,
  BUY_IT_NOW: 3,
}

export const AuctionEndReasonLabel = {
  [AuctionEndReason.TIME_EXPIRED]: 'Time Expired',
  [AuctionEndReason.ADMIN_CANCELLED]: 'Cancelled by Admin',
  [AuctionEndReason.SELLER_CANCELLED]: 'Cancelled by Seller',
  [AuctionEndReason.BUY_IT_NOW]: 'Buy It Now',
}

export const SortDirection = {
  ASC: 'asc',
  DESC: 'desc',
}
