import { Link } from 'react-router-dom'
import Button from '@/components/common/Button'

export default function ForbiddenPage() {
  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div className="text-center">
        <p className="text-8xl font-black text-secondary">403</p>
        <h1 className="text-2xl font-bold text-gray-900 mt-4 mb-2">Access Denied</h1>
        <p className="text-gray-500 mb-8">You don't have permission to access this page.</p>
        <Link to="/"><Button>Go to Homepage</Button></Link>
      </div>
    </div>
  )
}