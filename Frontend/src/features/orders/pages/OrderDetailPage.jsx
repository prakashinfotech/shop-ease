import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  ClipboardCheck,
  CreditCard,
  MapPin,
  Package,
  Receipt,
  RotateCcw,
  Truck,
} from 'lucide-react'
import toast from 'react-hot-toast'
import { orderService } from '../services/orderService'
import Badge from '@/components/common/Badge'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { OrderStatus, OrderStatusLabel, PaymentStatusLabel } from '@/constants/enums'
import { ROUTES } from '@/constants/routes'

const statusVariant = {
  [OrderStatus.PENDING]: 'warning',
  [OrderStatus.CONFIRMED]: 'primary',
  [OrderStatus.SHIPPED]: 'info',
  [OrderStatus.DELIVERED]: 'success',
  [OrderStatus.CANCELLED]: 'danger',
  [OrderStatus.REFUNDED]: 'default',
}

const STATUS_STEPS = [
  { status: OrderStatus.PENDING, label: 'Pending', icon: Receipt },
  { status: OrderStatus.CONFIRMED, label: 'Confirmed', icon: ClipboardCheck },
  { status: OrderStatus.SHIPPED, label: 'Shipped', icon: Truck },
  { status: OrderStatus.DELIVERED, label: 'Delivered', icon: CheckCircle2 },
]

const getStepState = (orderStatus, stepStatus) => {
  if (orderStatus === OrderStatus.CANCELLED || orderStatus === OrderStatus.REFUNDED) return 'muted'
  if (orderStatus >= stepStatus) return 'active'
  return 'pending'
}

const parsePaymentNotes = (notes) => {
  if (!notes) return null
  const entries = Object.fromEntries(
    notes
      .split('\n')
      .map((line) => line.split(':').map((part) => part.trim()))
      .filter(([key, value]) => key && value)
  )
  if (!entries['Payment Method']) return null
  return {
    method: entries['Payment Method'],
    upiId: entries['UPI ID'],
    reference: entries['Payment Reference'],
    status: entries['Payment Status'],
  }
}

export default function OrderDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['orders', id],
    queryFn: () => orderService.getById(id),
    enabled: !!id,
  })

  const { mutate: cancel, isPending: cancelling } = useMutation({
    mutationFn: () => orderService.cancel(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['orders'] })
      qc.invalidateQueries({ queryKey: ['orders', id] })
      toast.success('Order cancelled')
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Cancel failed'),
  })

  if (isLoading) {
    return (
      <div className="flex justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="max-w-3xl mx-auto">
        <div className="card p-10 text-center">
          <AlertCircle size={38} className="mx-auto text-red-500" />
          <p className="mt-3 font-semibold text-gray-900">Order could not be loaded</p>
          <p className="mt-1 text-sm text-gray-500">
            {error?.response?.data?.message || 'Please try again in a moment.'}
          </p>
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="mt-5 text-sm font-medium text-primary hover:text-primary-700"
          >
            Go back
          </button>
        </div>
      </div>
    )
  }

  const order = data?.data
  if (!order) {
    return (
      <div className="max-w-3xl mx-auto">
        <div className="card p-10 text-center">
          <Package size={38} className="mx-auto text-gray-300" />
          <p className="mt-3 font-semibold text-gray-900">Order not found</p>
          <Link to={ROUTES.ORDERS} className="mt-3 inline-flex text-sm font-medium">
            Back to orders
          </Link>
        </div>
      </div>
    )
  }

  const canCancel = order.status === OrderStatus.PENDING || order.status === OrderStatus.CONFIRMED
  const itemsTotal = order.items?.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0) ?? order.totalAmount
  const payment = order.paymentMethod
    ? {
        method: order.paymentMethod,
        upiId: order.upiId,
        reference: order.paymentReference,
        status: PaymentStatusLabel[order.paymentStatus],
      }
    : parsePaymentNotes(order.notes)

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <Breadcrumbs
        items={[
          { label: 'Orders', to: ROUTES.ORDERS },
          { label: `Order #${order.orderNumber}` },
        ]}
      />

      <button
        type="button"
        onClick={() => navigate(-1)}
        className="inline-flex items-center gap-1.5 text-sm text-gray-500 hover:text-primary"
      >
        <ArrowLeft size={16} /> Back to Orders
      </button>

      <section className="card p-5 sm:p-6">
        <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
          <div>
            <p className="text-sm font-medium text-primary">Order details</p>
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">Order #{order.orderNumber}</h1>
            <p className="mt-1 text-sm text-gray-500">Placed on {formatDate(order.createdAt)}</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={statusVariant[order.status]}>{OrderStatusLabel[order.status]}</Badge>
            {canCancel && (
              <Button variant="danger" onClick={() => cancel()} loading={cancelling}>
                <RotateCcw size={16} /> Cancel Order
              </Button>
            )}
          </div>
        </div>
      </section>

      <div className="grid lg:grid-cols-[minmax(0,1fr)_22rem] gap-5">
        <main className="space-y-5">
          <section className="card p-5 sm:p-6">
            <h2 className="text-lg font-bold text-gray-900">Order status</h2>
            <div className="mt-5 grid grid-cols-2 sm:grid-cols-4 gap-3">
              {STATUS_STEPS.map(({ status, label, icon: Icon }) => {
                const state = getStepState(order.status, status)
                return (
                  <div
                    key={status}
                    className={`rounded-lg border p-4 ${
                      state === 'active'
                        ? 'border-primary/30 bg-blue-50 text-primary'
                        : 'border-gray-200 bg-white text-gray-400'
                    }`}
                  >
                    <Icon size={20} />
                    <p className="mt-2 text-sm font-semibold">{label}</p>
                  </div>
                )
              })}
            </div>
            {(order.status === OrderStatus.CANCELLED || order.status === OrderStatus.REFUNDED) && (
              <p className="mt-4 text-sm text-gray-500">
                This order is {OrderStatusLabel[order.status]?.toLowerCase()} and no longer moves through fulfillment.
              </p>
            )}
          </section>

          <section className="card p-5 sm:p-6">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-lg font-bold text-gray-900">Items</h2>
              <span className="text-sm text-gray-500">
                {order.itemCount} item{order.itemCount === 1 ? '' : 's'}
              </span>
            </div>

            <div className="mt-4 divide-y divide-gray-100">
              {order.items?.map((item) => (
                <div key={item.id} className="py-4 flex gap-4">
                  <div className="w-16 h-16 rounded-lg bg-gray-50 text-gray-400 border border-gray-100 flex items-center justify-center shrink-0">
                    <Package size={26} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-semibold text-gray-900 truncate">{item.listingTitle}</p>
                    <p className="mt-1 text-sm text-gray-500">Qty: {item.quantity}</p>
                    <p className="mt-1 text-xs text-gray-400">{formatCurrency(item.unitPrice)} each</p>
                  </div>
                  <p className="text-sm font-bold text-gray-900 shrink-0">
                    {formatCurrency(item.unitPrice * item.quantity)}
                  </p>
                </div>
              ))}
            </div>
          </section>
        </main>

        <aside className="space-y-5">
          <section className="card p-5">
            <div className="flex items-center gap-2">
              <MapPin size={18} className="text-primary" />
              <h2 className="text-base font-bold text-gray-900">Shipping address</h2>
            </div>
            <p className="mt-3 text-sm text-gray-600 whitespace-pre-wrap">
              {order.shippingAddress || 'No shipping address provided'}
            </p>
            {order.trackingNumber && (
              <div className="mt-4 rounded-lg bg-gray-50 p-3 text-sm">
                <p className="font-semibold text-gray-900">{order.carrier || 'Carrier'}</p>
                <p className="mt-1 text-gray-600">Tracking: {order.trackingNumber}</p>
              </div>
            )}
          </section>

          <section className="card p-5">
            <div className="flex items-center gap-2">
              <CreditCard size={18} className="text-primary" />
              <h2 className="text-base font-bold text-gray-900">Payment summary</h2>
            </div>
            <dl className="mt-4 space-y-3 text-sm">
              {payment && (
                <>
                  <div className="flex justify-between gap-4">
                    <dt className="text-gray-500">Method</dt>
                    <dd className="font-medium text-gray-900">{payment.method}</dd>
                  </div>
                  <div className="flex justify-between gap-4">
                    <dt className="text-gray-500">UPI ID</dt>
                    <dd className="font-medium text-gray-900">{payment.upiId}</dd>
                  </div>
                  <div className="flex justify-between gap-4">
                    <dt className="text-gray-500">Status</dt>
                    <dd className="font-medium text-green-700">{payment.status}</dd>
                  </div>
                  <div className="flex justify-between gap-4">
                    <dt className="text-gray-500">Reference</dt>
                    <dd className="font-medium text-gray-900 break-all text-right">{payment.reference}</dd>
                  </div>
                </>
              )}
              <div className="flex justify-between gap-4">
                <dt className="text-gray-500">Items total</dt>
                <dd className="font-medium text-gray-900">{formatCurrency(itemsTotal)}</dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-gray-500">Shipping</dt>
                <dd className="font-medium text-gray-900">Included</dd>
              </div>
              <div className="border-t border-gray-100 pt-3 flex justify-between gap-4 text-base">
                <dt className="font-bold text-gray-900">Order total</dt>
                <dd className="font-extrabold text-gray-900">{formatCurrency(order.totalAmount)}</dd>
              </div>
            </dl>
          </section>

          <section className="card p-5">
            <p className="text-sm font-semibold text-gray-900">Need help?</p>
            <p className="mt-1 text-sm text-gray-500">
              Keep this order number handy when contacting support.
            </p>
            <p className="mt-3 text-sm font-bold text-gray-900">#{order.orderNumber}</p>
          </section>
        </aside>
      </div>
    </div>
  )
}
