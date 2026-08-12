import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import Input from '@/components/common/Input'
import Button from '@/components/common/Button'
import { useRegisterMutation } from '../hooks/useAuthMutations'
import { ROUTES } from '@/constants/routes'
import { AccountType } from '@/constants/enums'
import ShopEaseLogo from '@/components/common/ShopEaseLogo'

const schema = z
  .object({
    firstName: z.string().min(2, 'First name must be at least 2 characters'),
    lastName: z.string().min(2, 'Last name must be at least 2 characters'),
    email: z.string().email('Enter a valid email'),
    accountType: z.coerce.number(),
    country: z.string().optional(),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'Must contain uppercase')
      .regex(/[0-9]/, 'Must contain a number'),
    confirmPassword: z.string(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword'],
  })

export default function RegisterPage() {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      accountType: AccountType.PERSONAL,
      country: 'United States',
    },
  })

  const accountType = Number(watch('accountType'))
  const isBusiness = accountType === AccountType.BUSINESS
  const { mutate: registerUser, isPending } = useRegisterMutation()

  useEffect(() => {
    if (isBusiness) {
      setValue('lastName', 'Business', { shouldValidate: true })
    }
  }, [isBusiness, setValue])

  const onSubmit = ({ confirmPassword, country, ...values }) => {
    registerUser(values)
  }

  return (
    <div className="min-h-screen bg-white text-gray-950">
      <header className="flex items-start justify-between px-4 sm:px-8 pt-5">
        <ShopEaseLogo className="text-[2.8rem]" />
        <p className="text-sm text-gray-600">
          Already have an account?{' '}
          <Link to={ROUTES.LOGIN} className="underline text-gray-950 hover:text-gray-950">
            Sign in
          </Link>
        </p>
      </header>

      <main className="mx-auto grid max-w-7xl gap-10 px-4 py-8 lg:grid-cols-[minmax(0,1fr)_410px] lg:items-start lg:px-10">
        <div className="hidden overflow-hidden rounded-2xl lg:block">
          <img
            src="https://images.unsplash.com/photo-1556745757-8d76bdb6984b?auto=format&fit=crop&w=1100&q=80"
            alt="Small business owners packing orders"
            className="h-[825px] w-full object-cover"
          />
        </div>

        <section className="mx-auto w-full max-w-[410px] pt-8 lg:pt-16">
          <h1 className="mb-5 text-3xl font-bold text-gray-950">Create an account</h1>

          <div className="mb-5 grid grid-cols-2 rounded-full border border-gray-400 p-1 text-sm">
            <button
              type="button"
              onClick={() => setValue('accountType', AccountType.PERSONAL, { shouldDirty: true })}
              className={`rounded-full px-5 py-2 transition-colors ${
                !isBusiness ? 'bg-gray-950 text-white' : 'text-gray-950 hover:bg-gray-50'
              }`}
            >
              Personal
            </button>
            <button
              type="button"
              onClick={() => setValue('accountType', AccountType.BUSINESS, { shouldDirty: true })}
              className={`rounded-full px-5 py-2 transition-colors ${
                isBusiness ? 'bg-gray-950 text-white' : 'text-gray-950 hover:bg-gray-50'
              }`}
            >
              Business
            </button>
          </div>

          {isBusiness && (
            <p className="mb-4 text-sm leading-5">
              Continue to register as a <span className="font-semibold">business or nonprofit</span>, or if you plan to sell a large number of goods.
            </p>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <input type="hidden" {...register('accountType')} />

            {isBusiness ? (
              <>
                <Input
                  placeholder="Business name"
                  error={errors.firstName?.message}
                  {...register('firstName')}
                />
                <input type="hidden" {...register('lastName')} />
                <Input
                  type="email"
                  placeholder="Business email"
                  error={errors.email?.message}
                  {...register('email')}
                />
              </>
            ) : (
              <div className="grid grid-cols-2 gap-3">
                <Input
                  placeholder="First name"
                  error={errors.firstName?.message}
                  {...register('firstName')}
                />
                <Input
                  placeholder="Last name"
                  error={errors.lastName?.message}
                  {...register('lastName')}
                />
                <div className="col-span-2">
                  <Input
                    type="email"
                    placeholder="Email"
                    error={errors.email?.message}
                    {...register('email')}
                  />
                </div>
              </div>
            )}

            <Input
              type="password"
              placeholder="Password"
              showPasswordToggle
              error={errors.password?.message}
              {...register('password')}
            />

            <Input
              type="password"
              placeholder="Confirm password"
              showPasswordToggle
              error={errors.confirmPassword?.message}
              {...register('confirmPassword')}
            />

            {isBusiness && (
              <>
                <select className="form-input bg-white py-3" {...register('country')}>
                  <option>United States</option>
                  <option>India</option>
                  <option>United Kingdom</option>
                  <option>Canada</option>
                  <option>Australia</option>
                </select>
                <p className="text-xs text-gray-600">
                  If your business isn't registered, select your country of residence.
                </p>
                <label className="flex items-start gap-3 text-sm">
                  <input type="checkbox" className="mt-1 h-4 w-4 rounded border-gray-400" />
                  <span>I'm only interested in buying on ShopEase for now</span>
                </label>
              </>
            )}

            <p className="text-xs leading-5 text-gray-600">
              By selecting {isBusiness ? 'Create business account' : 'Create account'}, you agree to our{' '}
              <a href="#" className="underline text-[#3665f3]">User Agreement</a>
              {' '}and acknowledge reading our{' '}
              <a href="#" className="underline text-[#3665f3]">User Privacy Notice</a>.
            </p>

            <Button
              type="submit"
              loading={isPending}
              size="lg"
              className="w-full rounded-full bg-[#3665f3] py-3 disabled:bg-gray-300"
            >
              {isBusiness ? 'Create business account' : 'Create account'}
            </Button>
          </form>
        </section>
      </main>

      <button
        type="button"
        className="fixed bottom-6 right-6 hidden h-11 w-11 rounded-full bg-white text-2xl shadow-lg ring-1 ring-gray-200 lg:block"
        aria-label="Help"
      >
        ?
      </button>
    </div>
  )
}


