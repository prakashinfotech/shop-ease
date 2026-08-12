import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import Input from '@/components/common/Input'
import Button from '@/components/common/Button'
import { useResetPasswordMutation } from '../hooks/useAuthMutations'
import { ROUTES } from '@/constants/routes'

const schema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'Must contain an uppercase letter')
      .regex(/[0-9]/, 'Must contain a number'),
    confirmPassword: z.string(),
  })
  .refine((d) => d.newPassword === d.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword'],
  })

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')

  useEffect(() => {
    if (!token) navigate(ROUTES.FORGOT_PASSWORD, { replace: true })
  }, [token, navigate])

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({ resolver: zodResolver(schema) })

  const { mutate: resetPassword, isPending } = useResetPasswordMutation()

  const onSubmit = ({ newPassword, confirmPassword }) => {
    resetPassword({ token, newPassword, confirmPassword })
  }

  if (!token) return null

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div className="card w-full max-w-sm p-8">
        <div className="text-center mb-8">
          <Link to={ROUTES.HOME} className="flex justify-center items-center gap-0.5 mb-4 hover:opacity-80 transition-opacity">
            <span className="text-shopease-blue font-bold text-4xl">Shop</span>
            <span className="text-shopease-green font-bold text-4xl">Ease</span>
          </Link>
          <h1 className="text-xl font-semibold text-gray-900">Set a new password</h1>
          <p className="mt-1 text-sm text-gray-500">
            Choose a strong password for your account
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="New Password"
            type="password"
            placeholder="Min 8 chars, 1 uppercase, 1 number"
            required
            showPasswordToggle
            error={errors.newPassword?.message}
            {...register('newPassword')}
          />
          <Input
            label="Confirm New Password"
            type="password"
            placeholder="Re-enter your new password"
            required
            showPasswordToggle
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />
          <Button type="submit" loading={isPending} className="w-full" size="lg">
            Reset Password
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-gray-600">
          <Link to={ROUTES.LOGIN} className="font-medium text-primary hover:underline">
            Back to login
          </Link>
        </p>
      </div>
    </div>
  )
}
