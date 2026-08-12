const sizes = {
  sm: 'h-4 w-4 border-2',
  md: 'h-6 w-6 border-2',
  lg: 'h-10 w-10 border-4',
  xl: 'h-16 w-16 border-4',
}

export default function Spinner({ size = 'md', className = '' }) {
  return (
    <div
      className={`inline-block rounded-full border-gray-200 border-t-primary animate-spin ${sizes[size]} ${className}`}
      role="status"
      aria-label="Loading"
    />
  )
}