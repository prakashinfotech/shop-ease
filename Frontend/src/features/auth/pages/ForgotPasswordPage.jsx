import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import Input from '@/components/common/Input'
import Button from '@/components/common/Button'
import { useForgotPasswordMutation } from '../hooks/useAuthMutations'
import { ROUTES } from '@/constants/routes'

const schema = z.object({
  email: z.string().email('Enter a valid email address'),
})

export default function ForgotPasswordPage() {
  const [submitted, setSubmitted] = useState(false)
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) })

  const { mutate: forgotPassword, isPending } = useForgotPasswordMutation()

  const onSubmit = ({ email }) => {
    forgotPassword(email, {
      onSuccess: () => setSubmitted(true),
    })
  }

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div className="card w-full max-w-sm p-8">
        <div className="text-center mb-8">
          <Link to={ROUTES.HOME} className="flex justify-center items-center gap-0.5 mb-4 hover:opacity-80 transition-opacity">
            <span className="text-shopease-blue font-bold text-4xl">Shop</span>
            <span className="text-shopease-green font-bold text-4xl">Ease</span>
          </Link>
          <h1 className="text-xl font-semibold text-gray-900">Reset your password</h1>
          <p className="mt-1 text-sm text-gray-500">
            Enter your email and we'll send you a reset link
          </p>
        </div>

        {submitted ? (
          <div className="text-center space-y-4">
            <div className="w-14 h-14 bg-green-100 rounded-full flex items-center justify-center mx-auto">
              <svg className="w-7 h-7 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <p className="text-sm text-gray-700">
              If an account with that email exists, we've sent a password reset link. Check your inbox
              (and spam folder).
            </p>
            <p className="text-xs text-gray-500">The link expires in 1 hour.</p>
            <Link to={ROUTES.LOGIN} className="block text-sm font-medium text-primary hover:underline">
              Back to login
            </Link>
          </div>
        ) : (
          <>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <Input
                label="Email address"
                type="email"
                placeholder="you@example.com"
                required
                error={errors.email?.message}
                {...register('email')}
              />
              <Button type="submit" loading={isPending} className="w-full" size="lg">
                Send Reset Link
              </Button>
            </form>

            <p className="mt-6 text-center text-sm text-gray-600">
              Remember your password?{' '}
              <Link to={ROUTES.LOGIN} className="font-medium text-primary hover:underline">
                Sign in
              </Link>
            </p>
          </>
        )}
      </div>
    </div>
  )
}
