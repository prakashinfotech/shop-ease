import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Upload, X } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import Button from '@/components/common/Button'
import Input from '@/components/common/Input'
import Select from '@/components/common/Select'
import Spinner from '@/components/common/Spinner'
import { AttributeDataType, ListingStatus, ListingType } from '@/constants/enums'
import { categoryService } from '@/features/categories/services/categoryService'
import { assetUrl } from '@/utils/assets'
import { formatCurrency } from '@/utils/formatters'
import { listingService } from '../services/listingService'
import DynamicAttributeFields from './DynamicAttributeFields'
import { isAttributeVisible } from '../utils/attributeVisibility'

const flattenLeafCategories = (items = [], parentLabel = '') =>
  items.flatMap((item) => {
    const label = parentLabel ? `${parentLabel} / ${item.name}` : item.name
    return item.children?.length
      ? flattenLeafCategories(item.children, label)
      : [{ value: item.id, label }]
  })

const findCategoryPath = (items = [], categoryId, path = []) => {
  for (const item of items) {
    const nextPath = [...path, item]
    if (item.id === categoryId) return nextPath
    if (item.children?.length) {
      const found = findCategoryPath(item.children, categoryId, nextPath)
      if (found) return found
    }
  }

  return null
}

const toDateInput = (value) => value ? String(value).slice(0, 10) : ''

const roundMoney = (value) => Math.round(value * 100) / 100

const getListingBasePrice = (listing) => {
  if (!listing) return 0
  return listing.listingType === ListingType.AUCTION
    ? Number(listing.buyItNowPrice || listing.startingBid || listing.price || 0)
    : Number(listing.price || 0)
}

const getDiscountPercentage = (listing) => {
  const basePrice = getListingBasePrice(listing)
  if (!basePrice || !listing?.discountAmount) return ''
  return roundMoney((Number(listing.discountAmount) / basePrice) * 100)
}

const buildAttributeDefaults = (listing) => {
  const defaults = {}
  listing?.attributeValues?.forEach((value) => {
    if (value.dataType === AttributeDataType.MULTI_SELECT) {
      try {
        defaults[value.categoryAttributeId] = JSON.parse(value.value)
      } catch {
        defaults[value.categoryAttributeId] = value.value?.split(',').map((item) => item.trim()).filter(Boolean) || []
      }
    } else if (value.dataType === AttributeDataType.BOOLEAN) {
      defaults[value.categoryAttributeId] = value.value === 'true'
    } else {
      defaults[value.categoryAttributeId] = value.value
    }
  })
  return defaults
}

export default function ListingForm({ initialListing, onSubmit, isPending, submitLabel }) {
  const [images, setImages] = useState([])
  const [uploading, setUploading] = useState(false)

  const { data: treeData, isLoading: loadingCategories } = useQuery({
    queryKey: ['categories', 'tree'],
    queryFn: categoryService.getTree,
  })

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm({
    defaultValues: {
      listingType: ListingType.FIXED_PRICE,
      status: ListingStatus.PENDING_APPROVAL,
      quantity: 1,
      freeShipping: false,
      parentCategoryId: '',
      categoryId: '',
      attributes: {},
    },
  })

  const parentCategoryId = watch('parentCategoryId')
  const categoryId = watch('categoryId')
  const listingType = Number(watch('listingType'))
  const priceValue = Number(watch('price') || 0)
  const startingBidValue = Number(watch('startingBid') || 0)
  const buyItNowValue = Number(watch('buyItNowPrice') || 0)
  const discountPercentage = Number(watch('discountPercentage') || 0)
  const basePrice = listingType === ListingType.AUCTION
    ? (buyItNowValue || startingBidValue)
    : priceValue
  const discountAmount = roundMoney(basePrice * (discountPercentage / 100))
  const finalPrice = Math.max(basePrice - discountAmount, 0)

  const parentCategoryOptions = useMemo(
    () => (treeData?.data || []).map((category) => ({ value: category.id, label: category.name })),
    [treeData],
  )

  const childCategoryOptions = useMemo(() => {
    const parent = (treeData?.data || []).find((category) => category.id === parentCategoryId)
    return flattenLeafCategories(parent?.children || [])
  }, [parentCategoryId, treeData])

  const { data: metadataData, isLoading: loadingMetadata } = useQuery({
    queryKey: ['categories', 'metadata', categoryId],
    queryFn: () => categoryService.getMetadata(categoryId),
    enabled: !!categoryId,
  })

  const attributes = metadataData?.data?.attributes || []

  useEffect(() => {
    if (!initialListing) return

    const categoryPath = findCategoryPath(treeData?.data || [], initialListing.categoryId)
    const parentCategoryId = categoryPath?.[0]?.id || ''

    reset({
      title: initialListing.title,
      description: initialListing.description,
      listingType: initialListing.listingType ?? ListingType.FIXED_PRICE,
      price: initialListing.price,
      discountPercentage: getDiscountPercentage(initialListing),
      startingBid: initialListing.startingBid || '',
      reservePrice: initialListing.reservePrice || '',
      buyItNowPrice: initialListing.buyItNowPrice || '',
      auctionEndAt: toDateInput(initialListing.auctionEndAt),
      quantity: initialListing.quantity,
      freeShipping: initialListing.freeShipping,
      parentCategoryId,
      categoryId: initialListing.categoryId || '',
      status: ListingStatus.PENDING_APPROVAL,
      attributes: buildAttributeDefaults(initialListing),
    })
    setImages(initialListing.images || [])
  }, [initialListing, reset, treeData])

  const handleParentCategoryChange = (event) => {
    setValue('parentCategoryId', event.target.value, { shouldDirty: true, shouldValidate: true })
    setValue('categoryId', '', { shouldDirty: true, shouldValidate: true })
    setValue('attributes', {})
  }

  const handleChildCategoryChange = (event) => {
    setValue('categoryId', event.target.value, { shouldDirty: true, shouldValidate: true })
    setValue('attributes', {})
  }

  const handleImages = async (event) => {
    const files = Array.from(event.target.files || [])
    if (!files.length) return

    setUploading(true)
    try {
      const uploaded = []
      for (const file of files) {
        const response = await listingService.uploadImage(file)
        uploaded.push({
          url: response.data.url,
          altText: file.name,
          sortOrder: images.length + uploaded.length,
        })
      }
      setImages((current) => [...current, ...uploaded])
      toast.success(uploaded.length > 1 ? 'Images uploaded' : 'Image uploaded')
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Image upload failed')
    } finally {
      setUploading(false)
      event.target.value = ''
    }
  }

  const submit = (values) => {
    const visibleValues = values.attributes || {}
    const attributeValues = attributes
      .filter((attribute) => isAttributeVisible(attribute, visibleValues))
      .map((attribute) => {
        const raw = visibleValues[attribute.id]
        const value = Array.isArray(raw) ? JSON.stringify(raw) : raw
        return { categoryAttributeId: attribute.id, value: value === undefined || value === null ? '' : String(value) }
      })

    onSubmit({
      title: values.title,
      description: values.description,
      listingType: Number(values.listingType),
      price: Number(values.price || values.startingBid || 0),
      discountAmount: roundMoney(basePrice * (Number(values.discountPercentage || 0) / 100)),
      startingBid: values.startingBid ? Number(values.startingBid) : null,
      reservePrice: values.reservePrice ? Number(values.reservePrice) : null,
      buyItNowPrice: values.buyItNowPrice ? Number(values.buyItNowPrice) : null,
      auctionEndAt: values.auctionEndAt || null,
      quantity: Number(values.quantity || 1),
      freeShipping: Boolean(values.freeShipping),
      categoryId: values.categoryId || null,
      status: Number(values.status),
      attributeValues,
      images: images.map((image, index) => ({
        url: image.url,
        altText: image.altText || values.title,
        sortOrder: index,
      })),
    })
  }

  if (loadingCategories) {
    return <div className="flex justify-center py-16"><Spinner size="lg" /></div>
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="card p-6 space-y-5">
      <div className="grid sm:grid-cols-2 gap-4">
        <Select
          label="Parent Category"
          placeholder="Choose a parent category"
          required
          options={parentCategoryOptions}
          error={errors.parentCategoryId?.message}
          {...register('parentCategoryId', { required: 'Parent category is required' })}
          onChange={handleParentCategoryChange}
        />
        <Select
          label="Child Category"
          placeholder={parentCategoryId ? 'Choose a child category' : 'Select parent first'}
          required
          options={childCategoryOptions}
          error={errors.categoryId?.message}
          disabled={!parentCategoryId}
          {...register('categoryId', { required: 'Category is required' })}
          onChange={handleChildCategoryChange}
        />
      </div>

      <div className="grid sm:grid-cols-2 gap-4">
        <Select
          label="Listing Type"
          options={[
            { value: ListingType.FIXED_PRICE, label: 'Fixed Price' },
            { value: ListingType.AUCTION, label: 'Auction' },
          ]}
          {...register('listingType')}
        />
      </div>

      <Input
        label="Title"
        placeholder="What are you selling?"
        required
        error={errors.title?.message}
        {...register('title', { required: 'Title is required', minLength: { value: 3, message: 'Minimum 3 characters' }, maxLength: { value: 80, message: 'Maximum 80 characters' } })}
      />

      <div>
        <label className="form-label">Description <span className="text-red-500">*</span></label>
        <textarea
          rows={5}
          className={`form-input resize-none ${errors.description ? 'border-red-400' : ''}`}
          placeholder="Describe condition, features, dimensions, and anything buyers should know"
          {...register('description', { required: 'Description is required', minLength: { value: 10, message: 'Minimum 10 characters' } })}
        />
        {errors.description && <p className="form-error">{errors.description.message}</p>}
      </div>

      <div className="grid sm:grid-cols-3 gap-4">
        {listingType === ListingType.FIXED_PRICE ? (
          <Input
            label="Price (INR)"
            type="number"
            step="0.01"
            required
            error={errors.price?.message}
            {...register('price', { required: 'Price is required', min: { value: 0.01, message: 'Price must be positive' } })}
          />
        ) : (
          <>
            <Input label="Starting Bid" type="number" step="0.01" required error={errors.startingBid?.message} {...register('startingBid', { required: 'Starting bid is required', min: { value: 0.01, message: 'Starting bid must be positive' } })} />
            <Input label="Reserve Price" type="number" step="0.01" {...register('reservePrice')} />
            <Input label="Buy It Now" type="number" step="0.01" {...register('buyItNowPrice')} />
            <Input label="Auction Ends" type="date" className="sm:col-span-1" {...register('auctionEndAt')} />
          </>
        )}
        <Input
          label="Discount (%)"
          type="number"
          step="0.01"
          min="0"
          max="99.99"
          error={errors.discountPercentage?.message}
          {...register('discountPercentage', {
            min: { value: 0, message: 'Discount cannot be negative' },
            max: { value: 99.99, message: 'Discount must be less than 100%' },
          })}
        />
        <div className="rounded-md border border-gray-200 bg-gray-50 px-3 py-2">
          <p className="text-xs font-medium text-gray-500">Final Amount</p>
          <p className="text-base font-bold text-gray-900">{formatCurrency(finalPrice)}</p>
        </div>
        <Input label="Quantity" type="number" required error={errors.quantity?.message} {...register('quantity', { required: 'Quantity is required', min: { value: 1, message: 'Quantity must be at least 1' } })} />
        <Select
          label="Status"
          options={[
            { value: ListingStatus.PENDING_APPROVAL, label: 'Submit for Approval' },
          ]}
          {...register('status')}
        />
      </div>

      <label className="flex items-center gap-3 cursor-pointer">
        <input type="checkbox" className="w-4 h-4 text-primary rounded border-gray-300" {...register('freeShipping')} />
        <span className="text-sm font-medium text-gray-700">Offer free shipping</span>
      </label>

      {loadingMetadata ? (
        <div className="py-6 flex justify-center"><Spinner /></div>
      ) : (
        <DynamicAttributeFields attributes={attributes} register={register} errors={errors} watch={watch} />
      )}

      <div className="space-y-3 border-t border-gray-100 pt-5">
        <label className="form-label">Images</label>
        <label className="inline-flex items-center gap-2 btn-secondary px-4 py-2 text-sm cursor-pointer">
          <Upload size={16} />
          {uploading ? 'Uploading...' : 'Upload Images'}
          <input type="file" accept="image/*" multiple className="hidden" onChange={handleImages} disabled={uploading} />
        </label>
        {!!images.length && (
          <div className="grid grid-cols-3 sm:grid-cols-5 gap-3">
            {images.map((image, index) => (
              <div key={`${image.url}-${index}`} className="relative aspect-square rounded-md border border-gray-200 bg-gray-50 overflow-hidden">
                <img src={assetUrl(image.url)} alt={image.altText || ''} className="w-full h-full object-cover" />
                <button
                  type="button"
                  onClick={() => setImages((current) => current.filter((_, i) => i !== index))}
                  className="absolute top-1 right-1 p-1 rounded-full bg-white text-gray-500 shadow hover:text-red-500"
                >
                  <X size={14} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="flex gap-3 pt-2">
        <Button type="submit" loading={isPending || uploading} className="flex-1">{submitLabel}</Button>
      </div>
    </form>
  )
}
