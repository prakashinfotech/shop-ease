import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import ListingForm from '../components/ListingForm'
import { useListing, LISTING_KEYS } from '../hooks/useListings'
import { listingService } from '../services/listingService'
import { ROUTES } from '@/constants/routes'
import { ListingStatus } from '@/constants/enums'

export default function EditListingPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { data, isLoading } = useListing(id)

  const { mutate: update, isPending } = useMutation({
    mutationFn: (values) => listingService.update(id, values),
    onSuccess: (response) => {
      if (response?.data) {
        qc.setQueryData(LISTING_KEYS.detail(id), response)
      }
      qc.invalidateQueries({ queryKey: LISTING_KEYS.all })
      toast.success(
        data?.data?.status === ListingStatus.ACTIVE
          ? 'Changes submitted for approval'
          : 'Listing submitted for approval'
      )
      navigate(ROUTES.MY_LISTINGS)
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Update failed'),
  })

  if (isLoading) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-6 flex items-start justify-between gap-4">
        <h1 className="text-2xl font-bold text-gray-900">Edit Listing</h1>
        <Button variant="ghost" onClick={() => navigate(ROUTES.MY_LISTINGS)}>Cancel</Button>
      </div>

      <ListingForm
        initialListing={data?.data}
        onSubmit={update}
        isPending={isPending}
        submitLabel="Save Changes"
      />
    </div>
  )
}
