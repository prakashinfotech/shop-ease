import { useEffect } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { useVerifyEmailMutation } from '../hooks/useAuthMutations'
import Spinner from '@/components/common/Spinner'
import { ROUTES } from '@/constants/routes'

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const { mutate: verifyEmail, isPending, isSuccess, isError, error } = useVerifyEmailMutation()

  useEffect(() => {
    if (token) verifyEmail(token)
  }, [token]) // eslint-disable-line react-hooks/exhaustive-deps

  if (!token) {
    return (
      <PageShell>
        <StatusIcon type="error" />
        <h2 className="text-lg font-semibold text-gray-900 mt-4">Invalid verification link</h2>
        <p className="text-sm text-gray-500 mt-1">This link is missing the verification token.</p>
        <Link to={ROUTES.HOME} className="mt-6 block text-sm font-medium text-primary hover:underline">
          Go to homepage
        </Link>
      </PageShell>
    )
  }

  if (isPending) {
    return (
      <PageShell>
        <Spinner size="lg" />
        <p className="text-sm text-gray-600 mt-4">Verifying your email…</p>
      </PageShell>
    )
  }

  if (isSuccess) {
    return (
      <PageShell>
        <StatusIcon type="success" />
        <h2 className="text-lg font-semibold text-gray-900 mt-4">Email verified!</h2>
        <p className="text-sm text-gray-500 mt-1">Your email address has been successfully verified.</p>
        <Link to={ROUTES.HOME} className="mt-6 block text-sm font-medium text-primary hover:underline">
          Continue to marketplace
        </Link>
      </PageShell>
    )
  }

  if (isError) {
    return (
      <PageShell>
        <StatusIcon type="error" />
        <h2 className="text-lg font-semibold text-gray-900 mt-4">Verification failed</h2>
        <p className="text-sm text-gray-500 mt-1">
          {error?.response?.data?.message || 'The link may have expired or already been used.'}
        </p>
        <Link to={ROUTES.LOGIN} className="mt-6 block text-sm font-medium text-primary hover:underline">
          Back to login
        </Link>
      </PageShell>
    )
  }

  return null
}

function PageShell({ children }) {
  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div className="card w-full max-w-sm p-10 text-center">
        <div className="flex justify-center items-center gap-0.5 mb-6">
          <span className="text-shopease-blue font-bold text-3xl">Shop</span>
          <span className="text-shopease-green font-bold text-3xl">Ease</span>
        </div>
        {children}
      </div>
    </div>
  )
}

function StatusIcon({ type }) {
  const isSuccess = type === 'success'
  return (
    <div className={`w-14 h-14 rounded-full flex items-center justify-center mx-auto ${isSuccess ? 'bg-green-100' : 'bg-red-100'}`}>
      {isSuccess ? (
        <svg className="w-7 h-7 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
        </svg>
      ) : (
        <svg className="w-7 h-7 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
      )}
    </div>
  )
}
