import { useState, useEffect } from 'react'

export function useAuctionCountdown(endAt) {
  const [timeLeft, setTimeLeft] = useState(() => calcTimeLeft(endAt))

  useEffect(() => {
    if (!endAt) return
    const id = setInterval(() => setTimeLeft(calcTimeLeft(endAt)), 1000)
    return () => clearInterval(id)
  }, [endAt])

  return timeLeft
}

function calcTimeLeft(endAt) {
  if (!endAt) return null
  const diff = new Date(endAt) - Date.now()
  if (diff <= 0) return { ended: true, days: 0, hours: 0, minutes: 0, seconds: 0, label: 'Ended' }

  const days = Math.floor(diff / 86400000)
  const hours = Math.floor((diff % 86400000) / 3600000)
  const minutes = Math.floor((diff % 3600000) / 60000)
  const seconds = Math.floor((diff % 60000) / 1000)

  let label
  if (days > 0) label = `${days}d ${hours}h`
  else if (hours > 0) label = `${hours}h ${minutes}m`
  else label = `${minutes}m ${seconds}s`

  return { ended: false, days, hours, minutes, seconds, label, isUrgent: diff < 300000 }
}
