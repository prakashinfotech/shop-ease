import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { Eye, EyeOff } from 'lucide-react'
import Button from '@/components/common/Button'
import { useLoginMutation } from '../hooks/useAuthMutations'
import { authService } from '../services/authService'
import { ROUTES } from '@/constants/routes'
import ShopEaseLogo from '@/components/common/ShopEaseLogo'

const emailSchema = z.object({
  email: z.string().email('Enter a valid email'),
})

const schema = emailSchema.extend({
  password: z.string().min(6, 'Password must be at least 6 characters'),
})

export default function LoginPage() {
  const [step, setStep] = useState('email')
  const [showPassword, setShowPassword] = useState(false)
  const [checkingEmail, setCheckingEmail] = useState(false)
  const {
    register,
    handleSubmit,
    getValues,
    setError,
    clearErrors,
    formState: { errors },
  } = useForm()

  const { mutate: login, isPending } = useLoginMutation()

  const handleContinue = async () => {
    const result = emailSchema.safeParse({ email: getValues('email') })
    if (!result.success) {
      setError('email', { type: 'manual', message: result.error.issues[0].message })
      return
    }

    setCheckingEmail(true)
    try {
      const response = await authService.checkEmail(result.data.email)
      if (!response?.data?.exists) {
        setError('email', { type: 'manual', message: 'No account found with this email' })
        return
      }

      clearErrors('email')
      setStep('password')
    } catch (error) {
      setError('email', {
        type: 'manual',
        message: error?.response?.data?.message || 'Could not verify this email. Please try again.',
      })
    } finally {
      setCheckingEmail(false)
    }
  }

  const onSubmit = (values) => {
    const result = schema.safeParse(values)
    if (!result.success) {
      const issue = result.error.issues[0]
      const field = issue.path[0]
      setError(field, { type: 'manual', message: issue.message })
      if (field === 'email') setStep('email')
      return
    }

    login(result.data)
  }

  return (
    <div className="min-h-screen bg-white text-gray-950 flex flex-col">
      <header className="flex items-start justify-between px-4 sm:px-8 pt-5">
        <ShopEaseLogo className="text-[2.8rem]" />
        <a href="mailto:feedback@example.com" className="text-xs sm:text-sm text-gray-900 underline">
          Tell us what you think
        </a>
      </header>

      <main className="flex-1 flex justify-center px-4 pt-10 sm:pt-12">
        <div className="w-full max-w-[355px]">
          <div className="mb-5 bg-[#3665f3] text-white px-4 py-4 text-sm leading-5">
            <div className="flex gap-3">
              <span className="mt-0.5 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-white text-[11px] font-bold text-[#3665f3]">
                i
              </span>
              <p>
                To buy and sell on this marketplace, existing users can sign in with their email and password, or new users can register an account.
              </p>
            </div>
          </div>

          <h1 className="text-center text-2xl font-bold text-gray-950 mb-3">Sign in to your account</h1>

          <div className="mb-6 flex items-center justify-between rounded-2xl bg-gray-100 p-2 text-sm">
            <span className="pl-2">New to ShopEase?</span>
            <Link
              to={ROUTES.REGISTER}
              className="rounded-full border border-gray-950 bg-white px-5 py-1.5 text-gray-950 hover:text-gray-950"
            >
              Create account
            </Link>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {step === 'email' ? (
              <>
                <div>
                  <input
                    type="email"
                    placeholder="Email or username"
                    className={`w-full rounded-lg border px-4 py-3 text-sm outline-none focus:border-gray-950 ${
                      errors.email ? 'border-red-500' : 'border-gray-400'
                    }`}
                    {...register('email')}
                  />
                  {errors.email && <p className="form-error">{errors.email.message}</p>}
                </div>
                <p className="text-sm leading-5">
                  Created your account with a mobile number?<br />
                  <Link to={ROUTES.LOGIN} className="underline text-gray-950 hover:text-gray-950">
                    Sign in with mobile
                  </Link>
                </p>
                <Button type="button" loading={checkingEmail} size="lg" className="w-full rounded-full bg-[#3665f3] py-3" onClick={handleContinue}>
                  Continue
                </Button>
              </>
            ) : (
              <>
                <div>
                  <div className="relative">
                    <input
                      type={showPassword ? 'text' : 'password'}
                      placeholder="Password"
                      className={`w-full rounded-lg border px-4 py-3 pr-11 text-sm outline-none focus:border-gray-950 ${
                        errors.password ? 'border-red-500' : 'border-gray-400'
                      }`}
                      {...register('password')}
                    />
                    <button
                      type="button"
                      aria-label={showPassword ? 'Hide password' : 'Show password'}
                      title={showPassword ? 'Hide password' : 'Show password'}
                      className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-gray-700 hover:text-gray-950"
                      onMouseDown={(event) => event.preventDefault()}
                      onClick={() => setShowPassword((current) => !current)}
                    >
                      {showPassword ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}
                    </button>
                  </div>
                  {errors.password && <p className="form-error">{errors.password.message}</p>}
                </div>
                <div className="flex justify-end">
                  <Link to={ROUTES.FORGOT_PASSWORD} className="text-sm underline text-gray-950 hover:text-gray-950">
                    Forgot password?
                  </Link>
                </div>
                <Button type="submit" loading={isPending} size="lg" className="w-full rounded-full bg-[#3665f3] py-3">
                  Sign in
                </Button>
              </>
            )}

            <div className="flex items-center gap-3 py-3 text-sm">
              <span className="h-px flex-1 bg-gray-300" />
              <span>or</span>
              <span className="h-px flex-1 bg-gray-300" />
            </div>

            <SocialButton provider="Google" mark="G" />
            <SocialButton provider="Apple" mark="●" />
            <SocialButton provider="Facebook" mark="f" />

            <label className="flex items-center justify-center gap-2 pt-1 text-sm">
              <input type="checkbox" defaultChecked className="rounded border-gray-400 text-gray-950" />
              Stay signed in
              <span className="inline-flex h-4 w-4 items-center justify-center rounded-full border border-gray-700 text-[10px]">i</span>
            </label>
          </form>
        </div>
      </main>

      <footer className="px-4 py-4 text-center text-[11px] text-gray-700">
        Copyright © 1995-2026 ShopEase Inc. All Rights Reserved.
        {' '}
        <a className="underline" href="#">User Agreement</a>
        {' '}
        <a className="underline" href="#">Privacy</a>
        {' '}
        <a className="underline" href="#">Cookies</a>
      </footer>
    </div>
  )
}



function SocialButton({ provider, mark }) {
  return (
    <button
      type="button"
      className="flex w-full items-center justify-center rounded-full border border-gray-900 px-4 py-3 text-sm font-semibold text-gray-800 hover:bg-gray-50"
    >
      <span className="mr-10 inline-flex w-5 justify-center text-xl font-bold">{mark}</span>
      Continue with {provider}
    </button>
  )
}
