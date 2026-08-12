import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { AlertCircle, ChevronRight, ClipboardList, PackageCheck, Receipt, Search } from 'lucide-react'
import { orderService } from '../services/orderService'
import Badge from '@/components/common/Badge'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { OrderStatus, OrderStatusLabel } from '@/constants/enums'
import { buildRoute, ROUTES } from '@/constants/routes'

const statusVariant = {
  [OrderStatus.PENDING]: 'warning',
  [OrderStatus.CONFIRMED]: 'primary',
  [OrderStatus.SHIPPED]: 'info',
  [OrderStatus.DELIVERED]: 'success',
  [OrderStatus.CANCELLED]: 'danger',
  [OrderStatus.REFUNDED]: 'default',
}

const FILTERS = [
  { label: 'All', value: 'all' },
  { label: 'Pending', value: OrderStatus.PENDING },
  { label: 'Confirmed', value: OrderStatus.CONFIRMED },
  { label: 'Shipped', value: OrderStatus.SHIPPED },
  { label: 'Delivered', value: OrderStatus.DELIVERED },
]

export default function OrdersPage() {
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState('all')
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['orders', page],
    queryFn: () => orderService.getAll({ page, pageSize: 10 }),
  })

  const orders = useMemo(() => data?.data?.items || [], [data])
  const totalPages = data?.data?.totalPages || 1
  const filteredOrders = useMemo(() => (
    statusFilter === 'all'
      ? orders
      : orders.filter((order) => order.status === statusFilter)
  ), [orders, statusFilter])

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <Breadcrumbs items={[{ label: 'Orders' }]} />

      <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-primary">Purchase history</p>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">My Orders</h1>
          <p className="mt-1 text-sm text-gray-500">
            Review recent purchases, track status, and manage pending orders.
          </p>
        </div>
        <div className="card px-4 py-3 flex items-center gap-3">
          <div className="p-2 rounded-lg bg-blue-50 text-primary">
            <Receipt size={18} />
          </div>
          <div>
            <p className="text-xs text-gray-500">Visible orders</p>
            <p className="text-sm font-bold text-gray-900">{orders.length}</p>
          </div>
        </div>
      </div>

      <div className="flex gap-2 overflow-x-auto pb-1 scrollbar-hide">
        {FILTERS.map((filter) => (
          <button
            key={filter.label}
            type="button"
            onClick={() => setStatusFilter(filter.value)}
            className={`px-4 py-2 rounded-full text-sm font-medium border whitespace-nowrap transition-colors ${
              statusFilter === filter.value
                ? 'bg-primary text-white border-primary'
                : 'bg-white text-gray-600 border-gray-200 hover:border-primary/40 hover:text-primary'
            }`}
          >
            {filter.label}
          </button>
        ))}
      </div>

      {isLoading && (
        <div className="flex justify-center py-24">
          <Spinner size="lg" />
        </div>
      )}

      {isError && (
        <div className="card p-8 text-center">
          <AlertCircle size={36} className="mx-auto text-red-500" />
          <p className="mt-3 font-semibold text-gray-900">Orders could not be loaded</p>
          <p className="mt-1 text-sm text-gray-500">
            {error?.response?.data?.message || 'Please try again in a moment.'}
          </p>
        </div>
      )}

      {!isLoading && !isError && !orders.length && (
        <div className="card p-10 sm:p-12 text-center">
          <div className="mx-auto w-14 h-14 rounded-full bg-blue-50 text-primary flex items-center justify-center">
            <PackageCheck size={28} />
          </div>
          <p className="mt-4 font-semibold text-gray-900">No orders yet</p>
          <p className="mt-1 text-sm text-gray-500">When you buy something, it will appear here.</p>
          <Link to={ROUTES.LISTINGS} className="mt-5 inline-flex">
            <span className="btn-primary">Start shopping</span>
          </Link>
        </div>
      )}

      {!isLoading && !isError && !!orders.length && !filteredOrders.length && (
        <div className="card p-10 text-center">
          <Search size={34} className="mx-auto text-gray-300" />
          <p className="mt-3 font-semibold text-gray-900">No orders match this status</p>
          <p className="mt-1 text-sm text-gray-500">Choose another filter to see more orders.</p>
        </div>
      )}

      {!isLoading && !isError && !!filteredOrders.length && (
        <>
          <div className="space-y-3">
            {filteredOrders.map((order) => (
              <Link
                key={order.id}
                to={buildRoute(ROUTES.ORDER_DETAIL, { id: order.id })}
                className="card p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center gap-4 hover:shadow-md hover:border-primary/30 transition-all group"
              >
                <div className="p-3 rounded-lg bg-gray-50 text-gray-500 shrink-0">
                  <ClipboardList size={22} />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2 mb-1">
                    <p className="text-sm font-bold text-gray-900">Order #{order.orderNumber}</p>
                    <Badge variant={statusVariant[order.status]}>{OrderStatusLabel[order.status]}</Badge>
                  </div>
                  <p className="text-sm text-gray-500">
                    {formatDate(order.createdAt)} {' | '} {order.itemCount} item{order.itemCount === 1 ? '' : 's'}
                  </p>
                </div>
                <div className="flex items-center justify-between sm:justify-end gap-4">
                  <p className="text-lg font-bold text-gray-900">{formatCurrency(order.totalAmount)}</p>
                  <ChevronRight size={18} className="text-gray-300 group-hover:text-primary transition-colors" />
                </div>
              </Link>
            ))}
          </div>
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}
    </div>
  )
}
