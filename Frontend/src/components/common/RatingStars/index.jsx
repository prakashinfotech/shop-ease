import { Star } from 'lucide-react'

const sizeMap = { sm: 13, md: 17, lg: 22 }

export default function RatingStars({ rating = 0, count, size = 'sm', className = '' }) {
  const px = sizeMap[size] ?? sizeMap.sm

  return (
    <div className={`flex items-center gap-1 ${className}`}>
      <div className="flex items-center gap-0.5">
        {Array.from({ length: 5 }, (_, i) => (
          <Star
            key={i}
            size={px}
            className={
              i < Math.round(rating)
                ? 'text-accent fill-accent'
                : 'text-gray-300 fill-gray-100'
            }
          />
        ))}
      </div>
      {count !== undefined && (
        <span className={`text-gray-500 ${size === 'sm' ? 'text-xs' : 'text-sm'}`}>
          ({count.toLocaleString()})
        </span>
      )}
    </div>
  )
}
