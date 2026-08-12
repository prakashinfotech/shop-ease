import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { CreditCard, MapPin, ShoppingBag } from 'lucide-react'
import toast from 'react-hot-toast'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import Button from '@/components/common/Button'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import Input from '@/components/common/Input'
import { useCartStore } from '@/store/cartStore'
import { formatCurrency } from '@/utils/formatters'
import { ROUTES } from '@/constants/routes'

const listingPrice = (listing) => Number(listing.finalPrice ?? listing.price ?? 0)
const upiPattern = /^[a-zA-Z0-9.\-_]{2,256}@[a-zA-Z]{2,64}$/

export default function CheckoutPage() {
  const navigate = useNavigate()
  const { items, totalItems, totalPrice, clearCart } = useCartStore()
  const [upiId, setUpiId] = useState('')
  const [shippingAddress, setShippingAddress] = useState('')
  const [notes, setNotes] = useState('')

  const { mutate: checkout, isPending } = useMutation({
    mutationFn: () => api.post(API_ENDPOINTS.ORDERS.CHECKOUT, {
      items: items.map((item) => ({ listingId: item.listingId, quantity: item.quantity })),
      shippingAddress: shippingAddress.trim(),
      notes: notes.trim() || null,
      paymentMethod: 'UPI',
      upiId: upiId.trim(),
    }),
    onSuccess: (data) => {
      clearCart()
      toast.success('UPI demo payment successful. Order placed!')
      navigate(buildOrderUrl(data.data.id))
    },
    onError: (err) => toast.error(err?.response?.data?.message || 'Checkout failed'),
  })

  const handlePlaceOrder = () => {
    if (!shippingAddress.trim()) {
      toast.error('Shipping address is required')
      return
    }
    if (!upiPattern.test(upiId.trim())) {
      toast.error('Enter a valid UPI ID, for example name@upi')
      return
    }
    checkout()
  }

  if (!items.length) {
    return (
      <div className="text-center py-24">
        <ShoppingBag size={56} className="mx-auto text-gray-300 mb-4" />
        <h2 className="text-xl font-semibold text-gray-700 mb-2">Your cart is empty</h2>
        <p className="text-gray-500 mb-6">Add an item before checkout.</p>
        <Link to={ROUTES.LISTINGS}>
          <Button>Browse Listings</Button>
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <Breadcrumbs items={[{ label: 'Cart', to: ROUTES.CART }, { label: 'Checkout' }]} />
      <div>
        <p className="text-sm font-medium text-primary">Secure demo payment</p>
        <h1 className="text-2xl font-bold text-gray-900">Checkout</h1>
      </div>

      <div className="grid lg:grid-cols-[minmax(0,1fr)_22rem] gap-6">
        <main className="space-y-5">
          <section className="card p-5">
            <div className="flex items-center gap-2">
              <MapPin size={18} className="text-primary" />
              <h2 className="font-semibold text-gray-900">Shipping address</h2>
            </div>
            <textarea
              rows={4}
              value={shippingAddress}
              onChange={(event) => setShippingAddress(event.target.value)}
              className="form-input mt-4 resize-none"
              placeholder="House number, street, city, state, PIN"
            />
          </section>

          <section className="card p-5">
            <div className="flex items-center gap-2">
              <CreditCard size={18} className="text-primary" />
              <h2 className="font-semibold text-gray-900">UPI payment</h2>
            </div>
            <div className="mt-4 rounded-lg border border-green-100 bg-green-50 p-3 text-sm text-green-800">
              Demo mode: any valid-looking UPI ID is accepted and marked as paid instantly.
            </div>
            <div className="mt-4">
              <Input
                label="UPI ID"
                placeholder="yourname@upi"
                value={upiId}
                onChange={(event) => setUpiId(event.target.value)}
                required
              />
            </div>
          </section>

          <section className="card p-5">
            <h2 className="font-semibold text-gray-900">Order notes</h2>
            <textarea
              rows={3}
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              className="form-input mt-4 resize-none"
              placeholder="Delivery instructions"
            />
          </section>
        </main>

        <aside className="card p-5 h-fit space-y-4">
          <h2 className="font-semibold text-gray-900">Order summary</h2>
          <div className="space-y-3">
            {items.map((item) => (
              <div key={item.listingId} className="flex justify-between gap-3 text-sm">
                <div className="min-w-0">
                  <p className="truncate font-medium text-gray-800">{item.listing.title}</p>
                  <p className="text-xs text-gray-500">Qty {item.quantity}</p>
                </div>
                <p className="font-semibold text-gray-900">
                  {formatCurrency(listingPrice(item.listing) * item.quantity)}
                </p>
              </div>
            ))}
          </div>
          <div className="border-t border-gray-200 pt-3 space-y-2 text-sm">
            <div className="flex justify-between text-gray-600">
              <span>Subtotal ({totalItems} items)</span>
              <span>{formatCurrency(totalPrice)}</span>
            </div>
            <div className="flex justify-between text-gray-600">
              <span>Shipping</span>
              <span className="text-success">Free</span>
            </div>
          </div>
          <div className="border-t border-gray-200 pt-3 flex justify-between font-bold text-lg">
            <span>Total</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          <Button onClick={handlePlaceOrder} loading={isPending} className="w-full" size="lg">
            Pay with UPI Demo
          </Button>
        </aside>
      </div>
    </div>
  )
}

const buildOrderUrl = (id) => ROUTES.ORDER_DETAIL.replace(':id', id)
