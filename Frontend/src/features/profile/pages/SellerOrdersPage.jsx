import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import Badge from '@/components/common/Badge'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import Button from '@/components/common/Button'
import Input from '@/components/common/Input'
import Pagination from '@/components/common/Pagination'
import Spinner from '@/components/common/Spinner'
import { OrderStatus, OrderStatusLabel, PaymentStatusLabel } from '@/constants/enums'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { orderService } from '@/features/orders/services/orderService'

const statusVariant = {
  [OrderStatus.PENDING]: 'warning',
  [OrderStatus.CONFIRMED]: 'primary',
  [OrderStatus.SHIPPED]: 'info',
  [OrderStatus.DELIVERED]: 'success',
  [OrderStatus.CANCELLED]: 'danger',
  [OrderStatus.REFUNDED]: 'default',
}

export default function SellerOrdersPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [shipTarget, setShipTarget] = useState(null)
  const [carrier, setCarrier] = useState('')
  const [trackingNumber, setTrackingNumber] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['seller-orders', page],
    queryFn: () => orderService.getSellerOrders({ page, pageSize: 10 }),
  })

  const orders = data?.data?.items || []
  const totalPages = data?.data?.totalPages || 1

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }) => orderService.updateSellerStatus(id, payload),
    onSuccess: () => {
      toast.success('Order updated')
      queryClient.invalidateQueries({ queryKey: ['seller-orders'] })
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
    updateMutation.mutate({
      id: shipTarget.id,
      payload: { status: OrderStatus.SHIPPED, carrier, trackingNumber },
    })
  }

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <Breadcrumbs items={[{ label: 'Seller Orders' }]} />
      <div>
        <p className="text-sm font-medium text-primary">Seller fulfillment</p>
        <h1 className="text-2xl font-bold text-gray-900">Orders To Ship</h1>
        <p className="mt-1 text-sm text-gray-500">Manage orders that include your listings.</p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : !orders.length ? (
        <div className="card p-10 text-center text-sm text-gray-500">No seller orders yet.</div>
      ) : (
        <>
          <div className="space-y-3">
            {orders.map((order) => (
              <article key={order.id} className="card p-4 sm:p-5">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="text-sm font-bold text-gray-900">Order #{order.orderNumber}</h2>
                      <Badge variant={statusVariant[order.status]}>{OrderStatusLabel[order.status]}</Badge>
                    </div>
                    <p className="mt-1 text-sm text-gray-500">
                      {formatDate(order.createdAt)} | {order.itemCount} item{order.itemCount === 1 ? '' : 's'}
                    </p>
                    <p className="mt-1 text-sm text-gray-500">
                      Payment: {PaymentStatusLabel[order.paymentStatus]} via {order.paymentMethod}
                    </p>
                  </div>
                  <div className="text-left sm:text-right">
                    <p className="text-lg font-bold text-gray-900">{formatCurrency(order.totalAmount)}</p>
                    {order.trackingNumber ? (
                      <p className="mt-1 text-xs text-gray-500">{order.carrier}: {order.trackingNumber}</p>
                    ) : (
                      order.status === OrderStatus.CONFIRMED && (
                        <Button size="sm" onClick={() => setShipTarget(order)}>Mark Shipped</Button>
                      )
                    )}
                    {order.status === OrderStatus.SHIPPED && (
                      <Button
                        size="sm"
                        variant="secondary"
                        className="mt-2"
                        onClick={() => updateMutation.mutate({ id: order.id, payload: { status: OrderStatus.DELIVERED } })}
                      >
                        Mark Delivered
                      </Button>
                    )}
                  </div>
                </div>
                <div className="mt-4 divide-y divide-gray-100">
                  {order.items?.map((item) => (
                    <div key={item.id} className="py-2 text-sm flex justify-between gap-3">
                      <span className="text-gray-700">{item.listingTitle} x {item.quantity}</span>
                      <span className="font-medium text-gray-900">{formatCurrency(item.unitPrice * item.quantity)}</span>
                    </div>
                  ))}
                </div>
              </article>
            ))}
          </div>
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}

      {shipTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-bold text-gray-900">Mark Order Shipped</h2>
            <p className="mt-1 text-sm text-gray-500">Order #{shipTarget.orderNumber}</p>
            <div className="mt-4 space-y-3">
              <Input label="Carrier" value={carrier} onChange={(event) => setCarrier(event.target.value)} />
              <Input label="Tracking Number" value={trackingNumber} onChange={(event) => setTrackingNumber(event.target.value)} />
            </div>
            <div className="mt-5 flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setShipTarget(null)}>Cancel</Button>
              <Button onClick={shipOrder} loading={updateMutation.isPending}>Save</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
