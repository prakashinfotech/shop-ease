export default function ReserveIndicator({ reservePrice, currentBid }) {
  if (!reservePrice) {
    return <span className="text-xs text-green-600 font-medium">No reserve</span>
  }

  const met = Number(currentBid || 0) >= Number(reservePrice)
  return (
    <span className={`text-xs font-medium ${met ? 'text-green-600' : 'text-amber-600'}`}>
      Reserve {met ? 'met ✓' : 'not met'}
    </span>
  )
}
