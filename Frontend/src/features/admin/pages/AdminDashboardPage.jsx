import { useQuery } from '@tanstack/react-query'
import { Package, Users, ShoppingBag, DollarSign } from 'lucide-react'
import { Link } from 'react-router-dom'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import { ROUTES } from '@/constants/routes'
import Spinner from '@/components/common/Spinner'
import { formatCurrency } from '@/utils/formatters'

function StatCard({ icon: Icon, label, value, color, to }) {
  return (
    <Link to={to} className="card p-5 flex items-center gap-4 hover:shadow-md transition-shadow">
      <div className={`p-3 rounded-xl ${color}`}>
        <Icon size={22} className="text-white" />
      </div>
      <div>
        <p className="text-sm text-gray-500 font-medium">{label}</p>
        <p className="text-2xl font-bold text-gray-900">{value}</p>
      </div>
    </Link>
  )
}

export default function AdminDashboardPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'stats'],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.STATS),
  })

  if (isLoading) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

  const stats = data?.data || {}

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-sm text-gray-500">Platform overview</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard icon={DollarSign} label="Total Revenue" value={formatCurrency(stats.totalRevenue || 0)} color="bg-green-500" to={ROUTES.ADMIN_ORDERS} />
        <StatCard icon={ShoppingBag} label="Total Orders" value={(stats.totalOrders || 0).toLocaleString()} color="bg-primary" to={ROUTES.ADMIN_ORDERS} />
        <StatCard icon={Package} label="Active Listings" value={(stats.activeListings || 0).toLocaleString()} color="bg-yellow-500" to={ROUTES.ADMIN_LISTINGS} />
        <StatCard icon={Users} label="Total Users" value={(stats.totalUsers || 0).toLocaleString()} color="bg-purple-500" to={ROUTES.ADMIN_USERS} />
      </div>

      <div className="card p-6">
        <h2 className="font-semibold text-gray-900 mb-4">Recent Activity</h2>
        <p className="text-sm text-gray-500">Charts and activity feed will appear here.</p>
      </div>
    </div>
  )
}