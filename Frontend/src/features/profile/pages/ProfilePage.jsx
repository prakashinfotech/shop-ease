import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import toast from 'react-hot-toast'
import {
  BadgeCheck,
  Building2,
  ChevronRight,
  KeyRound,
  Mail,
  Package,
  Shield,
  ShoppingBag,
  Store,
  User,
} from 'lucide-react'
import Badge from '@/components/common/Badge'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import Button from '@/components/common/Button'
import Input from '@/components/common/Input'
import { useAuth } from '@/hooks/useAuth'
import { useAuthStore } from '@/store/authStore'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import { AccountType, AccountTypeLabel, VerificationStatus, VerificationStatusLabel } from '@/constants/enums'
import { ROUTES } from '@/constants/routes'

const schema = z.object({
  firstName: z.string().min(2, 'At least 2 characters'),
  lastName: z.string().min(2, 'At least 2 characters'),
  email: z.string().email('Invalid email'),
})

const QUICK_LINKS = [
  {
    to: ROUTES.ORDERS,
    icon: ShoppingBag,
    label: 'My Orders',
    desc: 'Track purchases and order history',
    color: 'bg-blue-50 text-blue-700',
  },
  {
    to: ROUTES.MY_LISTINGS,
    icon: Package,
    label: 'My Listings',
    desc: 'Manage the items you are selling',
    color: 'bg-green-50 text-green-700',
  },
  {
    to: ROUTES.SELLER_ORDERS,
    icon: ShoppingBag,
    label: 'Seller Orders',
    desc: 'Ship and track sold items',
    color: 'bg-orange-50 text-orange-700',
  },
  {
    to: ROUTES.BUSINESS_PROFILE,
    icon: Store,
    label: 'Business Profile',
    desc: 'Verification and seller details',
    color: 'bg-purple-50 text-purple-700',
  },
]

const TABS = [
  { id: 'profile', label: 'Profile', icon: User },
  { id: 'security', label: 'Security', icon: Shield },
]

const getAccountTypeLabel = (accountType) => {
  if (accountType === undefined || accountType === null) return 'Personal'
  return AccountTypeLabel[accountType] ?? accountType
}

const isBusinessAccountType = (accountType) =>
  accountType === AccountType.BUSINESS ||
  String(accountType).toLowerCase() === AccountTypeLabel[AccountType.BUSINESS].toLowerCase()

const getVerificationLabel = (status) => {
  if (!status) return 'Not submitted'
  return VerificationStatusLabel[status] ?? status
}

const getVerificationVariant = (status) => {
  if (status === VerificationStatus.VERIFIED) return 'success'
  if (status === VerificationStatus.REJECTED) return 'danger'
  if (status === VerificationStatus.UNDER_REVIEW) return 'warning'
  return 'default'
}

export default function ProfilePage() {
  const { user } = useAuth()
  const setUser = useAuthStore((s) => s.setUser)
  const [activeTab, setActiveTab] = useState('profile')

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      firstName: user?.firstName ?? '',
      lastName: user?.lastName ?? '',
      email: user?.email ?? '',
    },
  })

  const { mutate: save, isPending } = useMutation({
    mutationFn: (data) => api.put(API_ENDPOINTS.USERS.PROFILE, data),
    onSuccess: (res) => {
      setUser(res.data)
      toast.success('Profile updated')
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Update failed'),
  })

  const initials = `${user?.firstName?.[0] ?? ''}${user?.lastName?.[0] ?? ''}`.toUpperCase() || 'U'
  const isBusinessAccount = isBusinessAccountType(user?.accountType)
  const accountType = getAccountTypeLabel(user?.accountType)
  const verificationLabel = getVerificationLabel(user?.businessVerificationStatus)
  const verificationVariant = getVerificationVariant(user?.businessVerificationStatus)
  const quickLinks = QUICK_LINKS.filter((link) => link.to !== ROUTES.BUSINESS_PROFILE || isBusinessAccount)

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <Breadcrumbs items={[{ label: 'Profile' }]} />

      <div className="grid lg:grid-cols-[minmax(0,1fr)_20rem] gap-5">
        <section className="card p-5 sm:p-6">
          <div className="flex flex-col sm:flex-row sm:items-center gap-5">
            <div className="w-20 h-20 rounded-2xl bg-primary text-white flex items-center justify-center text-2xl font-extrabold shadow-sm shrink-0">
              {initials}
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-primary">Account overview</p>
              <h1 className="text-2xl font-bold text-gray-900 truncate">
                {user?.firstName} {user?.lastName}
              </h1>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-gray-500">
                <span className="inline-flex items-center gap-1.5 min-w-0">
                  <Mail size={15} className="shrink-0" />
                  <span className="truncate">{user?.email}</span>
                </span>
              </div>
              <div className="mt-4 flex flex-wrap gap-2">
                <Badge variant="primary" className="gap-1.5">
                  <Building2 size={13} />
                  {accountType} account
                </Badge>
                <Badge variant={user?.isEmailVerified ? 'success' : 'warning'} className="gap-1.5">
                  <BadgeCheck size={13} />
                  {user?.isEmailVerified ? 'Email verified' : 'Email pending'}
                </Badge>
                {isBusinessAccount && (
                  <Badge variant={verificationVariant} className="gap-1.5">
                    <Store size={13} />
                    {verificationLabel}
                  </Badge>
                )}
              </div>
            </div>
          </div>
        </section>

        <aside className="card p-5">
          <p className="text-sm font-semibold text-gray-900">Selling tools</p>
          <p className="mt-1 text-sm text-gray-500">
            Keep your seller details current so buyers can trust your listings.
          </p>
          <Link to={ROUTES.CREATE_LISTING} className="mt-4 inline-flex w-full">
            <Button className="w-full">Create Listing</Button>
          </Link>
        </aside>
      </div>

      <div className="grid sm:grid-cols-3 gap-3">
        {quickLinks.map(({ to, icon: Icon, label, desc, color }) => (
          <Link
            key={to}
            to={to}
            className="card p-4 flex items-center gap-3 hover:shadow-md hover:border-primary/30 transition-all group"
          >
            <div className={`p-2.5 rounded-lg shrink-0 ${color}`}>
              <Icon size={18} />
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-sm font-semibold text-gray-900">{label}</p>
              <p className="text-xs text-gray-500 truncate">{desc}</p>
            </div>
            <ChevronRight size={15} className="text-gray-300 group-hover:text-primary transition-colors shrink-0" />
          </Link>
        ))}
      </div>

      <div className="card overflow-hidden">
        <div className="flex overflow-x-auto border-b border-gray-200">
          {TABS.map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              type="button"
              onClick={() => setActiveTab(id)}
              className={`flex items-center gap-2 px-5 py-3.5 text-sm font-medium border-b-2 whitespace-nowrap transition-colors ${
                activeTab === id
                  ? 'border-primary text-primary bg-primary/5'
                  : 'border-transparent text-gray-500 hover:text-gray-800 hover:border-gray-300'
              }`}
            >
              <Icon size={15} />
              {label}
            </button>
          ))}
        </div>

        <div className="p-5 sm:p-6">
          {activeTab === 'profile' && (
            <div className="grid lg:grid-cols-[minmax(0,1fr)_18rem] gap-6">
              <form onSubmit={handleSubmit(save)} className="space-y-4 max-w-2xl">
                <div className="grid sm:grid-cols-2 gap-3">
                  <Input
                    label="First Name"
                    required
                    error={errors.firstName?.message}
                    {...register('firstName')}
                  />
                  <Input
                    label="Last Name"
                    required
                    error={errors.lastName?.message}
                    {...register('lastName')}
                  />
                </div>
                <Input
                  label="Email Address"
                  type="email"
                  required
                  error={errors.email?.message}
                  {...register('email')}
                />
                <div className="pt-2 flex flex-col sm:flex-row sm:items-center gap-3">
                  <Button type="submit" loading={isPending} disabled={!isDirty}>
                    Save Changes
                  </Button>
                  <p className="text-xs text-gray-500">
                    Profile changes update your marketplace account immediately.
                  </p>
                </div>
              </form>

              <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                <p className="text-sm font-semibold text-gray-900">Account details</p>
                <dl className="mt-4 space-y-3 text-sm">
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-gray-400">Role</dt>
                    <dd className="font-medium text-gray-900">{user?.role ?? 'User'}</dd>
                  </div>
                  <div>
                    <dt className="text-xs uppercase tracking-wide text-gray-400">Email</dt>
                    <dd className="font-medium text-gray-900">
                      {user?.isEmailVerified ? 'Verified' : 'Pending verification'}
                    </dd>
                  </div>
                  {isBusinessAccount && (
                    <div>
                      <dt className="text-xs uppercase tracking-wide text-gray-400">Business</dt>
                      <dd className="font-medium text-gray-900">{verificationLabel}</dd>
                    </div>
                  )}
                </dl>
              </div>
            </div>
          )}

          {activeTab === 'security' && (
            <div className="grid md:grid-cols-2 gap-4">
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="flex items-start gap-3">
                  <div className="p-2 rounded-lg bg-blue-50 text-primary">
                    <KeyRound size={18} />
                  </div>
                  <div>
                    <p className="text-sm font-semibold text-gray-900">Password</p>
                    <p className="mt-1 text-sm text-gray-500">
                      Use the reset flow to securely change your password.
                    </p>
                    <Link
                      to={ROUTES.FORGOT_PASSWORD}
                      className="mt-3 inline-flex items-center gap-1 text-sm text-primary font-medium hover:text-primary-700"
                    >
                      Change Password <ChevronRight size={14} />
                    </Link>
                  </div>
                </div>
              </div>

              <div className="rounded-lg border border-gray-200 p-4">
                <div className="flex items-start gap-3">
                  <div className="p-2 rounded-lg bg-green-50 text-green-700">
                    <Shield size={18} />
                  </div>
                  <div>
                    <p className="text-sm font-semibold text-gray-900">Session protection</p>
                    <p className="mt-1 text-sm text-gray-500">
                      Your account uses short-lived access tokens and refresh sessions.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
