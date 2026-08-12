import { create } from 'zustand'
import { persist } from 'zustand/middleware'

const listingPrice = (listing) => Number(listing.finalPrice ?? listing.price ?? 0)

export const useCartStore = create(
  persist(
    (set, get) => ({
      items: [],
      totalItems: 0,
      totalPrice: 0,

      addItem: (listing, quantity = 1) => {
        const { items } = get()
        const existingIndex = items.findIndex((i) => i.listingId === listing.id)
        let newItems

        if (existingIndex >= 0) {
          newItems = items.map((item, idx) =>
            idx === existingIndex
              ? { ...item, quantity: item.quantity + quantity }
              : item
          )
        } else {
          newItems = [...items, { listingId: listing.id, listing, quantity }]
        }

        set({
          items: newItems,
          totalItems: newItems.reduce((sum, i) => sum + i.quantity, 0),
          totalPrice: newItems.reduce((sum, i) => sum + listingPrice(i.listing) * i.quantity, 0),
        })
      },

      removeItem: (listingId) => {
        const newItems = get().items.filter((i) => i.listingId !== listingId)
        set({
          items: newItems,
          totalItems: newItems.reduce((sum, i) => sum + i.quantity, 0),
          totalPrice: newItems.reduce((sum, i) => sum + listingPrice(i.listing) * i.quantity, 0),
        })
      },

      updateQuantity: (listingId, quantity) => {
        if (quantity <= 0) {
          get().removeItem(listingId)
          return
        }
        const newItems = get().items.map((i) =>
          i.listingId === listingId ? { ...i, quantity } : i
        )
        set({
          items: newItems,
          totalItems: newItems.reduce((sum, i) => sum + i.quantity, 0),
          totalPrice: newItems.reduce((sum, i) => sum + listingPrice(i.listing) * i.quantity, 0),
        })
      },

      clearCart: () => set({ items: [], totalItems: 0, totalPrice: 0 }),
    }),
    {
      name: 'cart-storage',
    }
  )
)
