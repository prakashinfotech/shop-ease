# API Contract

Base URL: `/api/v1`  
All responses: `ApiResponse<T>` envelope → `{ success, data, message }`  
Paginated: `ApiResponse<PagedResult<T>>` → `{ items, totalCount, page, pageSize }`  
Auth: `Authorization: Bearer <accessToken>`  
Admin: requires `UserRole=Admin`

---

## Auth `/api/v1/auth`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| POST | `/register` | — | `RegisterRequest` | `ApiResponse<AuthResponse>` |
| POST | `/login` | — | `LoginRequest` | `ApiResponse<AuthResponse>` |
| POST | `/check-email` | — | `CheckEmailRequest` | `ApiResponse<CheckEmailResponse>` |
| POST | `/refresh` | — | `RefreshRequest` | `ApiResponse<AuthResponse>` |
| POST | `/logout` | ✓ | `RefreshRequest` | `ApiResponse` |
| GET | `/me` | ✓ | — | `ApiResponse<UserDto>` |
| POST | `/verify-email` | — | `VerifyEmailRequest` | `ApiResponse<UserDto>` |
| POST | `/resend-verification` | ✓ | — | `ApiResponse` |
| POST | `/forgot-password` | — | `ForgotPasswordRequest` | `ApiResponse` (always 200) |
| POST | `/reset-password` | — | `ResetPasswordRequest` | `ApiResponse` |

**DTOs:**
```
RegisterRequest:       FirstName, LastName, Email, Password, AccountType=Personal
LoginRequest:          Email, Password
AuthResponse:          User(UserDto), AccessToken, RefreshToken
UserDto:               Id, FirstName, LastName, Email, AccountType, Role,
                       IsEmailVerified, IsSuspended, BusinessVerificationStatus?
RefreshRequest:        RefreshToken
CheckEmailRequest:     Email
CheckEmailResponse:    Exists(bool)
ForgotPasswordRequest: Email
VerifyEmailRequest:    Token
ResetPasswordRequest:  Token, NewPassword, ConfirmPassword
```

---

## Listings `/api/v1/listings`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| GET | `/` | — | `ListingQuery` (query params) | `ApiResponse<PagedResult<ListingResponse>>` |
| GET | `/autocomplete` | — | `?q=` | `ApiResponse<AutocompleteResponse>` |
| GET | `/facets` | — | `?categoryId=&search=` | `ApiResponse<SearchFacetsResponse>` |
| GET | `/recently-viewed` | ✓ | `?days=3&take=12` | `ApiResponse<IReadOnlyList<ListingResponse>>` |
| GET | `/my` | ✓ | `ListingQuery` | `ApiResponse<PagedResult<ListingResponse>>` |
| GET | `/{id}` | — | — | `ApiResponse<ListingResponse>` |
| POST | `/` | ✓ | `CreateListingRequest` | `ApiResponse<ListingResponse>` |
| POST | `/images` | ✓ | `ImageUploadRequest` (multipart) | `ApiResponse<ListingImageUploadResponse>` |
| POST | `/{id}/views` | ✓ | — | `ApiResponse` |
| PUT | `/{id}` | ✓ | `UpdateListingRequest` | `ApiResponse<ListingResponse>` |
| DELETE | `/{id}` | ✓ | — | `ApiResponse` |
| PATCH | `/{id}/restore` | ✓ | — | `ApiResponse<ListingResponse>` |
| POST | `/{id}/submit` | ✓ | — | `ApiResponse<ListingResponse>` |
| POST | `/{id}/submit-update` | ✓ | `SubmitListingUpdateRequest` | `ApiResponse` |
| GET | `/{id}/versions` | ✓ | — | `ApiResponse<IReadOnlyList<ListingVersionResponse>>` |

**DTOs:**
```
ListingQuery:            Page=1, PageSize=24, Search?, Category?, CategoryId?,
                         MinPrice?, MaxPrice?, FreeShipping?, Status?, ListingType?,
                         SellerId?, ExcludeSellerId?, IncludeDeleted=false,
                         SortBy=updatedAt, SortDirection=desc, AttributeFilters?

CreateListingRequest:    Title, Description, ListingType, Price, Quantity,
                         DiscountAmount?, FreeShipping?, CategoryId?,
                         StartingBid?, ReservePrice?, BuyItNowPrice?,
                         AuctionStartAt?, AuctionEndAt?,
                         AttributeValues?([]ListingAttributeValueRequest),
                         Images?([]ListingImageRequest)

UpdateListingRequest:    Same as Create + Status?

ListingAttributeValueRequest: CategoryAttributeId, Value?
ListingImageRequest:     Url, AltText?, SortOrder=0

ListingResponse:         Id, Title, Description, ListingType, Price, DiscountAmount,
                         FinalPrice, StartingBid?, ReservePrice?, BuyItNowPrice?,
                         AuctionStartAt?, AuctionEndAt?, Quantity, FreeShipping,
                         Status, IsDeleted, PrimaryImageUrl?, SellerId, SellerName,
                         CategoryId?, CategoryName?, AttributeValues, Images,
                         CreatedAt, UpdatedAt, HasPendingVersion?

ListingVersionResponse:  Id, ListingId, VersionNumber, IsPendingUpdate, Status,
                         StatusName, RejectionReason?, ReviewedByAdminName?,
                         ReviewedAt?, SubmittedAt, Snapshot?

AutocompleteResponse:    Suggestions(IReadOnlyCollection<string>)

ImageUploadRequest:      File (IFormFile, multipart) — defined in API/Models/FileUploadModels.cs
ListingImageUploadResponse: Url
```

> `ImageUploadRequest` was previously defined inline in `ListingsController.cs`. Moved to `API/Models/FileUploadModels.cs`. Request shape and route unchanged.

---

## Admin `/api/v1/admin`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| GET | `/stats` | Admin | — | `ApiResponse<AdminStatsResponse>` |
| GET | `/users` | Admin | `?page&pageSize&search&accountType&role&status&sortBy&sortDirection` | `ApiResponse<PagedResult<AdminUserResponse>>` |
| PATCH | `/users/{id}/suspend` | Admin | — | `ApiResponse` |
| PATCH | `/users/{id}/activate` | Admin | — | `ApiResponse` |
| GET | `/listings` | Admin | `?page&pageSize&search&status&visibility&sortBy&sortDirection` | `ApiResponse<PagedResult<AdminListingResponse>>` |
| GET | `/listings/{id}` | Admin | — | `ApiResponse<AdminListingDetailResponse>` |
| GET | `/listings/{id}/versions` | Admin | — | `ApiResponse<IReadOnlyList<ListingVersionResponse>>` |
| POST | `/listings/{id}/approve` | Admin | `ApproveListingRequest` | `ApiResponse<ListingResponse>` |
| POST | `/listings/{id}/reject` | Admin | `RejectListingRequest` | `ApiResponse<ListingResponse>` |
| DELETE | `/listings/{id}` | Admin | — | `ApiResponse` |
| GET | `/orders` | Admin | `?page&pageSize&search&status&sortBy&sortDirection` | `ApiResponse<PagedResult<AdminOrderResponse>>` |
| GET | `/business-profiles` | Admin | `PagedQuery + ?status=` | `ApiResponse<PagedResult<AdminBusinessProfileResponse>>` |
| PUT | `/business-profiles/{id}/review` | Admin | `ReviewBusinessProfileRequest` | `ApiResponse<BusinessProfileResponse>` |
| GET | `/email-templates` | Admin | `?page=1&pageSize=20&type=` | `ApiResponse<PagedResult<EmailTemplateResponse>>` |
| GET | `/email-templates/{id}` | Admin | — | `ApiResponse<EmailTemplateResponse>` |
| POST | `/email-templates` | Admin | `CreateEmailTemplateRequest` | `ApiResponse<EmailTemplateResponse>` |
| PUT | `/email-templates/{id}` | Admin | `UpdateEmailTemplateRequest` | `ApiResponse<EmailTemplateResponse>` |
| PATCH | `/email-templates/{id}/activate` | Admin | — | `ApiResponse<EmailTemplateResponse>` |
| PATCH | `/email-templates/{id}/deactivate` | Admin | — | `ApiResponse<EmailTemplateResponse>` |
| DELETE | `/email-templates/{id}` | Admin | — | `ApiResponse` |

**DTOs** (all moved to `Application/DTOs/Admin/AdminDtos.cs` — API contract unchanged):
```
AdminStatsResponse:             TotalUsers, ActiveListings, TotalOrders, TotalRevenue
AdminUserResponse:              Id, FirstName, LastName, Email, AccountType, Role,
                                 IsEmailVerified, IsSuspended, IsDeleted, CreatedAt
AdminListingResponse:           Id, Title, SellerName, Price, DiscountAmount, FinalPrice,
                                 Status, HasPendingVersion, IsDeleted, CreatedAt
AdminListingDetailResponse:     Id, Title, Description, SellerName, SellerEmail, Price,
                                 DiscountAmount, FinalPrice, Status, HasPendingVersion,
                                 IsDeleted, CreatedAt, PendingVersion?
AdminOrderResponse:             Id, OrderNumber, BuyerName, ItemCount, TotalAmount,
                                 Status, CreatedAt
ApproveListingRequest:          Notes?
RejectListingRequest:           Reason, Notes?
CreateEmailTemplateRequest:     Name, Subject, HtmlBody, TemplateType
UpdateEmailTemplateRequest:     Name, Subject, HtmlBody
ReviewBusinessProfileRequest:   IsApproved, RejectionReason?

AdminBusinessProfileResponse:   Id, UserId, UserEmail, UserFullName, CompanyName,
                                 GstNumber, PanNumber, VerificationStatus,
                                 DocumentCount, CreatedAt, UpdatedAt
EmailTemplateResponse:          Id, Name, Subject, HtmlBody, TemplateType,
                                 TemplateTypeName, IsActive, Version, CreatedAt, UpdatedAt
```

> DTOs were previously defined inline in `AdminController.cs`. They are now in `Application/DTOs/Admin/AdminDtos.cs`. All routes, request shapes, response shapes, and status codes are unchanged.

---

## Users `/api/v1/users`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| GET | `/profile` | ✓ | — | `ApiResponse<UserResponse>` |
| PUT | `/profile` | ✓ | `UpdateProfileRequest` | `ApiResponse<UserResponse>` |
| GET | `/` | Admin | `PagedQuery` | `ApiResponse<PagedResult<UserResponse>>` |
| GET | `/{id}` | Admin | — | `ApiResponse<UserResponse>` |
| PATCH | `/{id}/suspend` | Admin | — | `ApiResponse` |
| PATCH | `/{id}/activate` | Admin | — | `ApiResponse` |

**DTOs:**
```
UpdateProfileRequest: FirstName, LastName, Email, PhoneNumber?
UserResponse:         Id, FirstName, LastName, Email, PhoneNumber?, AvatarUrl?,
                      AccountType, Role, IsEmailVerified, IsSuspended, IsDeleted, CreatedAt
```

---

## Categories `/api/v1/categories`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| GET | `/` | — | — | `ApiResponse<IEnumerable<CategoryResponse>>` |
| GET | `/tree` | — | — | `ApiResponse<IEnumerable<CategoryTreeResponse>>` |
| GET | `/{id}` | — | — | `ApiResponse<CategoryResponse>` |
| GET | `/{id}/metadata` | — | — | `ApiResponse<CategoryMetadataResponse>` |
| POST | `/` | Admin | `CategoryRequest` | `ApiResponse<CategoryResponse>` |
| PUT | `/{id}` | Admin | `CategoryRequest` | `ApiResponse<CategoryResponse>` |
| DELETE | `/{id}` | Admin | — | `ApiResponse` |
| POST | `/{id}/attributes` | Admin | `CategoryAttributeRequest` | `ApiResponse<CategoryAttributeResponse>` |
| PUT | `/{id}/attributes/{attrId}` | Admin | `CategoryAttributeRequest` | `ApiResponse<CategoryAttributeResponse>` |
| DELETE | `/{id}/attributes/{attrId}` | Admin | — | `ApiResponse` |

---

## Orders `/api/v1/orders`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| GET | `/` | ✓ | `PagedQuery` | `ApiResponse<PagedResult<OrderResponse>>` |
| GET | `/{id}` | ✓ | — | `ApiResponse<OrderResponse>` |
| POST | `/checkout` | ✓ | `CheckoutRequest` | `ApiResponse<OrderResponse>` |
| POST | `/{id}/cancel` | ✓ | — | `ApiResponse` |

**DTOs:**
```
CheckoutRequest:  Items([]CheckoutItem), ShippingAddress?, Notes?
CheckoutItem:     ListingId, Quantity
OrderResponse:    Id, OrderNumber, TotalAmount, Status, ShippingAddress?,
                  BuyerId, BuyerName, Items([]OrderItemResponse), ItemCount, CreatedAt
OrderItemResponse: Id, ListingId, ListingTitle, Quantity, UnitPrice
```

---

## Business Profile `/api/v1/business-profile`

| Method | Path | Auth | Request | Response |
|--------|------|------|---------|---------|
| GET | `/` | ✓ | — | `ApiResponse<BusinessProfileResponse>` |
| POST | `/` | ✓ | `BusinessProfileRequest` | `ApiResponse<BusinessProfileResponse>` |
| PUT | `/` | ✓ | `BusinessProfileRequest` | `ApiResponse<BusinessProfileResponse>` |
| POST | `/documents` | ✓ | `DocumentUploadRequest` (multipart, max 10MB) | `ApiResponse<DocumentResponse>` |
| DELETE | `/documents/{documentId}` | ✓ | — | `ApiResponse` |

**DTOs:**
```
BusinessProfileRequest:  CompanyName, GstNumber, PanNumber, BusinessAddress?,
                         BusinessPhone?, BusinessEmail?, BusinessWebsite?
BusinessProfileResponse: Id, UserId, CompanyName, GstNumber, PanNumber,
                         BusinessAddress?, BusinessPhone?, BusinessEmail?,
                         BusinessWebsite?, VerificationStatus, RejectionReason?,
                         VerifiedAt?, Documents([]DocumentResponse), CreatedAt, UpdatedAt
DocumentResponse:        Id, FileName, FileUrl, DocumentType, FileSizeBytes,
                         ContentType, CreatedAt

DocumentUploadRequest:   File (IFormFile, multipart), DocumentType — defined in API/Models/FileUploadModels.cs
```

> `DocumentUploadRequest` was previously defined inline in `BusinessProfileController.cs`. Moved to `API/Models/FileUploadModels.cs`. Request shape and route unchanged.

---

## Other Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/cart/checkout` | ✓ | Alias for orders checkout |
| GET | `/health` | — | Health check |
| GET | `/swagger` | — | Swagger UI |
