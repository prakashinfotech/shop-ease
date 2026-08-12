import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import Badge from '@/components/common/Badge'
import Button from '@/components/common/Button'
import Input from '@/components/common/Input'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { OrderStatus, OrderStatusLabel, PaymentStatusLabel } from '@/constants/enums'
import AdminDataTable from '../components/AdminDataTable'

const statusVariant = {
  [OrderStatus.PENDING]: 'warning',
  [OrderStatus.CONFIRMED]: 'primary',
  [OrderStatus.SHIPPED]: 'info',
  [OrderStatus.DELIVERED]: 'success',
  [OrderStatus.CANCELLED]: 'danger',
  [OrderStatus.REFUNDED]: 'default',
}

const pageSizeOptions = [15, 30, 50, 100]

export default function AdminOrdersPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [sortKey, setSortKey] = useState('createdAt_desc')
  const [shipTarget, setShipTarget] = useState(null)
  const [carrier, setCarrier] = useState('')
  const [trackingNumber, setTrackingNumber] = useState('')
  const [sortBy, sortDirection] = sortKey.split('_')

  useEffect(() => {
    setPage(1)
  }, [pageSize, search, sortKey, status])

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'orders', page, pageSize, search, status, sortKey],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.ORDERS, {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status !== '' ? Number(status) : undefined,
        sortBy,
        sortDirection,
      },
    }),
  })

  const orders = data?.items || data?.data?.items || []
  const totalPages = data?.totalPages || data?.data?.totalPages || 1
  const totalCount = data?.totalCount || data?.data?.totalCount || 0
  const updateStatus = useMutation({
    mutationFn: ({ id, payload }) => api.put(API_ENDPOINTS.ADMIN.ORDER_STATUS(id), payload),
    onSuccess: () => {
      toast.success('Order updated')
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] })
      setShipTarget(null)
      setCarrier('')
      setTrackingNumber('')
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Update failed'),
  })

  const shipOrder = () => {
    if (!carrier.trim() || !trackingNumber.trim()) {
      toast.error('Carrier and tracking number are required')
      return
    }
    updateStatus.mutate({
      id: shipTarget.id,
      payload: { status: OrderStatus.SHIPPED, carrier, trackingNumber },
    })
  }

  const columns = [
    {
      key: 'orderNumber',
      label: 'Order #',
      sortKey: 'orderNumber',
      defaultDirection: 'desc',
      cellClassName: 'font-mono text-xs text-gray-600',
      render: (order) => <span className="break-all">#{order.orderNumber}</span>,
    },
    {
      key: 'buyer',
      label: 'Buyer',
      sortKey: 'buyer',
      cellClassName: 'font-medium text-gray-900',
      render: (order) => <span className="break-words">{order.buyerName}</span>,
    },
    {
      key: 'items',
      label: 'Items',
      sortKey: 'itemCount',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (order) => order.itemCount,
    },
    {
      key: 'total',
      label: 'Total',
      sortKey: 'totalAmount',
      defaultDirection: 'desc',
      cellClassName: 'font-semibold',
      render: (order) => formatCurrency(order.totalAmount),
    },
    {
      key: 'status',
      label: 'Status',
      sortKey: 'status',
      render: (order) => (
        <Badge variant={statusVariant[order.status]}>{OrderStatusLabel[order.status]}</Badge>
      ),
    },
    {
      key: 'payment',
      label: 'Payment',
      render: (order) => (
        <div className="text-xs">
          <div className="font-medium text-gray-900">{PaymentStatusLabel[order.paymentStatus]}</div>
          <div className="text-gray-400">{order.paymentMethod}</div>
        </div>
      ),
    },
    {
      key: 'tracking',
      label: 'Tracking',
      render: (order) => order.trackingNumber
        ? <span className="text-xs text-gray-600">{order.carrier}: {order.trackingNumber}</span>
        : <span className="text-xs text-gray-400">Not shipped</span>,
    },
    {
      key: 'date',
      label: 'Date',
      sortKey: 'createdAt',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (order) => formatDate(order.createdAt),
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (order) => (
        <div className="flex flex-wrap gap-2">
          {order.status === OrderStatus.PENDING && (
            <Button size="sm" onClick={() => updateStatus.mutate({ id: order.id, payload: { status: OrderStatus.CONFIRMED } })}>
              Confirm
            </Button>
          )}
          {order.status === OrderStatus.CONFIRMED && (
            <Button size="sm" onClick={() => setShipTarget(order)}>Ship</Button>
          )}
          {order.status === OrderStatus.SHIPPED && (
            <Button size="sm" variant="secondary" onClick={() => updateStatus.mutate({ id: order.id, payload: { status: OrderStatus.DELIVERED } })}>
              Deliver
            </Button>
          )}
          {order.status !== OrderStatus.CANCELLED && order.status !== OrderStatus.REFUNDED && (
            <Button size="sm" variant="danger" onClick={() => updateStatus.mutate({ id: order.id, payload: { status: OrderStatus.REFUNDED } })}>
              Refund
            </Button>
          )}
        </div>
      ),
    },
  ]

  const resetFilters = () => {
    setSearch('')
    setStatus('')
    setSortKey('createdAt_desc')
    setPageSize(15)
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Orders</h1>
        <p className="text-sm text-gray-500">{totalCount.toLocaleString()} total orders</p>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(220px,1fr)_160px_110px_auto]">
          <input
            type="search"
            placeholder="Search order, buyer, email"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="form-input h-10 min-w-0 text-sm sm:col-span-2 xl:col-span-1"
          />
          <select value={status} onChange={(e) => setStatus(e.target.value)} className="form-input h-10 min-w-0 bg-white text-sm">
            <option value="">All statuses</option>
            {Object.values(OrderStatus).map((value) => (
              <option key={value} value={value}>{OrderStatusLabel[value]}</option>
            ))}
          </select>
          <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))} className="form-input h-10 min-w-0 bg-white text-sm">
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>{size} / page</option>
            ))}
          </select>
          <button type="button" onClick={resetFilters} className="h-10 rounded-md border border-gray-200 px-3 text-sm font-medium text-gray-600 hover:border-primary hover:text-primary">
            Reset
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : (
        <>
          <AdminDataTable
            columns={columns}
            rows={orders}
            getRowKey={(order) => order.id}
            sortKey={sortKey}
            onSortChange={setSortKey}
            emptyMessage="No orders found"
          />
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}

      {shipTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-bold text-gray-900">Ship Order</h2>
            <p className="mt-1 text-sm text-gray-500">Order #{shipTarget.orderNumber}</p>
            <div className="mt-4 space-y-3">
              <Input label="Carrier" value={carrier} onChange={(event) => setCarrier(event.target.value)} />
              <Input label="Tracking Number" value={trackingNumber} onChange={(event) => setTrackingNumber(event.target.value)} />
            </div>
            <div className="mt-5 flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setShipTarget(null)}>Cancel</Button>
              <Button onClick={shipOrder} loading={updateStatus.isPending}>Save</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
