import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/store/authStore'

export function useAuctionHub(listingId, { onBidPlaced, onAuctionEnded, onTimeExtended, onAuctionCancelled } = {}) {
  const connectionRef = useRef(null)
  const accessToken = useAuthStore((s) => s.accessToken)

  useEffect(() => {
    if (!listingId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/auction', {
        accessTokenFactory: () => useAuthStore.getState().accessToken,
        skipNegotiation: false,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    if (onBidPlaced) connection.on('bid-placed', onBidPlaced)
    if (onAuctionEnded) connection.on('auction-ended', onAuctionEnded)
    if (onTimeExtended) connection.on('time-extended', onTimeExtended)
    if (onAuctionCancelled) connection.on('auction-cancelled', onAuctionCancelled)

    connection.start()
      .then(() => connection.invoke('JoinAuction', listingId.toString()))
      .catch((err) => console.warn('SignalR connect error:', err))

    return () => {
      connection
        .invoke('LeaveAuction', listingId.toString())
        .catch(() => {})
        .finally(() => connection.stop())
    }
  }, [listingId])
}
