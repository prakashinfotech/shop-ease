import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { listingService } from '../services/listingService'

export const LISTING_KEYS = {
  all: ['listings'],
  list: (params) => ['listings', 'list', params],
  detail: (id) => ['listings', 'detail', id],
  my: (params) => ['listings', 'my', params],
  recentlyViewed: (params) => ['listings', 'recently-viewed', params],
}

export const useListings = (params, options = {}) =>
  useQuery({
    queryKey: LISTING_KEYS.list(params),
    queryFn: () => listingService.getAll(params),
    enabled: params !== null,
    ...options,
  })

export const useListing = (id) =>
  useQuery({
    queryKey: LISTING_KEYS.detail(id),
    queryFn: () => listingService.getById(id),
    enabled: !!id,
  })

export const useMyListings = (params) =>
  useQuery({
    queryKey: LISTING_KEYS.my(params),
    queryFn: () => listingService.getMyListings(params),
  })

export const useRecentlyViewedListings = (params, options = {}) =>
  useQuery({
    queryKey: LISTING_KEYS.recentlyViewed(params),
    queryFn: () => listingService.getRecentlyViewed(params),
    ...options,
  })

export const useRecordListingView = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: listingService.recordView,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['listings', 'recently-viewed'] })
    },
  })
}

export const useCreateListing = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: listingService.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: LISTING_KEYS.all })
      toast.success('Listing submitted for approval')
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Failed to create listing'),
  })
}

export const useDeleteListing = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: listingService.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: LISTING_KEYS.all })
      toast.success('Listing deleted')
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Failed to delete listing'),
  })
}

export const useRestoreListing = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: listingService.restore,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: LISTING_KEYS.all })
      toast.success('Listing restored')
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Failed to restore listing'),
  })
}

export const useListingAutocomplete = (q) =>
  useQuery({
    queryKey: ['listings', 'autocomplete', q],
    queryFn: () => listingService.autocomplete(q),
    enabled: typeof q === 'string' && q.length >= 2,
    staleTime: 30_000,
    select: (res) => res?.data?.suggestions ?? [],
  })

export const useSearchFacets = (params) =>
  useQuery({
    queryKey: ['listings', 'facets', params],
    queryFn: () => listingService.getFacets(params),
    staleTime: 60_000,
    select: (res) => res?.data ?? null,
  })
