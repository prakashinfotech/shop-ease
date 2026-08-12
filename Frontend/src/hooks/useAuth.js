import { useAuthStore } from '@/store/authStore'
import { useCartStore } from '@/store/cartStore'
import { UserRole } from '@/constants/enums'

export const useAuth = () => {
  const { user, isAuthenticated, accessToken, clearAuth } = useAuthStore()
  const role = String(user?.role || '').toLowerCase()

  const logout = () => {
    clearAuth()
    useCartStore.getState().clearCart()
  }

  return {
    user,
    isAuthenticated,
    accessToken,
    isAdmin: role === UserRole.ADMIN.toLowerCase(),
    logout,
  }
}
