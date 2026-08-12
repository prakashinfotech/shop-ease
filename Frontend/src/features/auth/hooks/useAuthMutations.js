import { useMutation } from '@tanstack/react-query'
import { useNavigate, useLocation } from 'react-router-dom'
import toast from 'react-hot-toast'
import { authService } from '../services/authService'
import { useAuthStore } from '@/store/authStore'
import { ROUTES } from '@/constants/routes'
import { UserRole } from '@/constants/enums'

export const useLoginMutation = () => {
  const { setAuth } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()

  return useMutation({
    mutationFn: authService.login,
    onSuccess: (data) => {
      const { user, accessToken, refreshToken } = data.data
      setAuth(user, accessToken, refreshToken)
      toast.success(`Welcome back, ${user.firstName}!`)
      const from = location.state?.from?.pathname
      const fallback = String(user?.role || '').toLowerCase() === UserRole.ADMIN.toLowerCase()
        ? ROUTES.ADMIN_DASHBOARD
        : ROUTES.HOME
      const target = from && ![ROUTES.LOGIN, ROUTES.REGISTER].includes(from) ? from : fallback
      navigate(target, { replace: true })
    },
    onError: (error) => {
      toast.error(error?.response?.data?.message || 'Login failed. Please try again.')
    },
  })
}

export const useRegisterMutation = () => {
  const { setAuth } = useAuthStore()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authService.register,
    onSuccess: (data) => {
      const { user, accessToken, refreshToken } = data.data
      setAuth(user, accessToken, refreshToken)
      toast.success('Account created! Please check your email to verify your address.')
      navigate('/')
    },
    onError: (error) => {
      toast.error(error?.response?.data?.message || 'Registration failed. Please try again.')
    },
  })
}

export const useForgotPasswordMutation = () => {
  return useMutation({
    mutationFn: authService.forgotPassword,
    onError: (error) => {
      toast.error(error?.response?.data?.message || 'Something went wrong. Please try again.')
    },
  })
}

export const useResetPasswordMutation = () => {
  const navigate = useNavigate()

  return useMutation({
    mutationFn: authService.resetPassword,
    onSuccess: () => {
      toast.success('Password reset successfully!')
      navigate(ROUTES.LOGIN)
    },
    onError: (error) => {
      toast.error(error?.response?.data?.message || 'Reset failed. The link may have expired.')
    },
  })
}

export const useVerifyEmailMutation = () => {
  const { setUser } = useAuthStore()

  return useMutation({
    mutationFn: authService.verifyEmail,
    onSuccess: (data) => {
      if (data?.data) setUser(data.data)
    },
  })
}

export const useResendVerificationMutation = () => {
  return useMutation({
    mutationFn: authService.resendVerification,
    onSuccess: () => {
      toast.success('Verification email sent! Please check your inbox.')
    },
    onError: (error) => {
      toast.error(error?.response?.data?.message || 'Could not send email. Please try again.')
    },
  })
}
