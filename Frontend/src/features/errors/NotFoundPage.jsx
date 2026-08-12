import { Link } from 'react-router-dom'
import Button from '@/components/common/Button'

export default function NotFoundPage() {
  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div className="text-center">
        <p className="text-8xl font-black text-primary">404</p>
        <h1 className="text-2xl font-bold text-gray-900 mt-4 mb-2">Page Not Found</h1>
        <p className="text-gray-500 mb-8">The page you're looking for doesn't exist or has been moved.</p>
        <Link to="/"><Button>Go to Homepage</Button></Link>
      </div>
    </div>
  )
}