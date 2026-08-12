import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      retry: (failureCount, error) => {
        if (error?.response?.status === 401 || error?.response?.status === 403) return false
        return failureCount < 2
      },
    },
    mutations: {
      retry: false,
    },
  },
})