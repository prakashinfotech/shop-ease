import { Link } from 'react-router-dom'
import { Trash2, Plus, Minus, ShoppingBag } from 'lucide-react'
import { useCartStore } from '@/store/cartStore'
import Button from '@/components/common/Button'
import { formatCurrency } from '@/utils/formatters'
import { ROUTES } from '@/constants/routes'

const listingPrice = (listing) => Number(listing.finalPrice ?? listing.price ?? 0)

export default function CartPage() {
  const { items, totalItems, totalPrice, removeItem, updateQuantity, clearCart } = useCartStore()

  if (!items.length) {
    return (
      <div className="text-center py-24">
        <ShoppingBag size={56} className="mx-auto text-gray-300 mb-4" />
        <h2 className="text-xl font-semibold text-gray-700 mb-2">Your cart is empty</h2>
        <p className="text-gray-500 mb-6">Start shopping to add items here</p>
        <Link to={ROUTES.LISTINGS}>
          <Button>Browse Listings</Button>
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">
        Shopping Cart <span className="text-gray-400 font-normal text-lg">({totalItems} items)</span>
      </h1>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Items */}
        <div className="lg:col-span-2 space-y-3">
          {items.map((item) => (
            <div key={item.listingId} className="card p-4 flex gap-4">
              <div className="w-20 h-20 bg-gray-100 rounded-lg overflow-hidden flex-shrink-0">
                {item.listing.primaryImageUrl ? (
                  <img src={item.listing.primaryImageUrl} alt={item.listing.title} className="w-full h-full object-cover" />
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-3xl">📦</div>
                )}
              </div>

              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900 line-clamp-2">{item.listing.title}</p>
                <div className="mt-1 flex items-baseline gap-2">
                  <p className="text-base font-bold text-gray-900">{formatCurrency(listingPrice(item.listing))}</p>
                  {Number(item.listing.discountAmount || 0) > 0 && (
                    <p className="text-xs text-gray-400 line-through">{formatCurrency(item.listing.price)}</p>
                  )}
                </div>

                <div className="flex items-center gap-3 mt-2">
                  <div className="flex items-center border border-gray-300 rounded-md">
                    <button onClick={() => updateQuantity(item.listingId, item.quantity - 1)} className="px-2 py-1 text-gray-600 hover:bg-gray-100">
                      <Minus size={14} />
                    </button>
                    <span className="px-3 text-sm font-medium">{item.quantity}</span>
                    <button onClick={() => updateQuantity(item.listingId, item.quantity + 1)} className="px-2 py-1 text-gray-600 hover:bg-gray-100">
                      <Plus size={14} />
                    </button>
                  </div>
                  <button
                    onClick={() => removeItem(item.listingId)}
                    className="text-red-400 hover:text-red-600 p-1"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>

              <div className="text-right flex-shrink-0">
                <p className="font-bold text-gray-900">{formatCurrency(listingPrice(item.listing) * item.quantity)}</p>
              </div>
            </div>
          ))}
        </div>

        {/* Summary */}
        <div className="card p-5 h-fit space-y-4">
          <h2 className="font-semibold text-gray-900">Order Summary</h2>
          <div className="space-y-2 text-sm">
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
          <Link to={ROUTES.CHECKOUT} className="block">
            <Button className="w-full" size="lg">Proceed to Checkout</Button>
          </Link>
          <button onClick={clearCart} className="w-full text-xs text-gray-400 hover:text-red-500 mt-1">
            Clear cart
          </button>
        </div>
      </div>
    </div>
  )
}
