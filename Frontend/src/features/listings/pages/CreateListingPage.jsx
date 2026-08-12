import { Link, useNavigate } from 'react-router-dom'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import { useAuth } from '@/hooks/useAuth'
import { useResendVerificationMutation } from '@/features/auth/hooks/useAuthMutations'
import { useBusinessProfile } from '@/features/profile/hooks/useBusinessProfile'
import { AccountType, AccountTypeLabel } from '@/constants/enums'
import ListingForm from '../components/ListingForm'
import { useCreateListing } from '../hooks/useListings'
import { ROUTES } from '@/constants/routes'

const isBusinessAccountType = (accountType) =>
  accountType === AccountType.BUSINESS ||
  String(accountType).toLowerCase() === AccountTypeLabel[AccountType.BUSINESS].toLowerCase()

export default function CreateListingPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const { mutate: createListing, isPending } = useCreateListing()
  const { mutate: resendVerification, isPending: isResendingVerification } = useResendVerificationMutation()
  const isBusinessAccount = isBusinessAccountType(user?.accountType)
  const canCheckBusinessProfile = Boolean(user?.isEmailVerified && isBusinessAccount)
  const { data: businessProfile, isLoading: isLoadingBusinessProfile } = useBusinessProfile({
    enabled: canCheckBusinessProfile,
  })

  const handleSubmit = (data) => {
    createListing(data, {
      onSuccess: () => navigate(ROUTES.MY_LISTINGS),
    })
  }

  if (!user?.isEmailVerified) {
    return (
      <SellRequirementCard
        title="Verify your email to start selling"
        description="Before you create a listing, confirm your email address from the verification email we sent you."
        primaryAction={
          <Button onClick={() => resendVerification()} loading={isResendingVerification}>
            Resend Verification Email
          </Button>
        }
      />
    )
  }

  if (isLoadingBusinessProfile) {
    return <div className="flex justify-center py-16"><Spinner size="lg" /></div>
  }

  if (isBusinessAccount && !businessProfile) {
    return (
      <SellRequirementCard
        title="Submit business details to start selling"
        description="Business accounts need a submitted business profile before creating listings."
        primaryAction={
          <Link to={ROUTES.BUSINESS_PROFILE}>
            <Button>Submit Business Details</Button>
          </Link>
        }
      />
    )
  }

  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-6 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Create a Listing</h1>
          <p className="text-sm text-gray-500 mt-1">Choose a parent category, then a child category to load the right listing form.</p>
        </div>
        <Button variant="ghost" onClick={() => navigate(ROUTES.MY_LISTINGS)}>Cancel</Button>
      </div>

      <ListingForm onSubmit={handleSubmit} isPending={isPending} submitLabel="Create Listing" />
    </div>
  )
}

function SellRequirementCard({ title, description, primaryAction }) {
  return (
    <div className="max-w-xl mx-auto">
      <div className="card p-6 text-center">
        <h1 className="text-xl font-bold text-gray-900">{title}</h1>
        <p className="mt-2 text-sm text-gray-500">{description}</p>
        <div className="mt-5 flex justify-center">{primaryAction}</div>
        <Link to={ROUTES.PROFILE} className="mt-4 inline-flex text-sm font-medium text-primary hover:underline">
          Back to profile
        </Link>
      </div>
    </div>
  )
}
