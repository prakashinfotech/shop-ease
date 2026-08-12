import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import AdminSidebar from './AdminSidebar'
import AdminNavbar from './AdminNavbar'

export default function AdminLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <div className="flex h-screen bg-[#f4f6f9] overflow-hidden text-gray-900">
      <AdminSidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <AdminNavbar onMenuToggle={() => setSidebarOpen(true)} />
        <main className="flex-1 overflow-auto">
          <div className="border-b border-gray-200 bg-white px-4 sm:px-6 py-2">
            <div className="flex h-1 max-w-7xl gap-1">
              <span className="w-16 rounded-full bg-shopease-blue" />
              <span className="w-16 rounded-full bg-shopease-red" />
              <span className="w-16 rounded-full bg-shopease-yellow" />
              <span className="w-16 rounded-full bg-shopease-green" />
            </div>
          </div>
          <div className="max-w-7xl mx-auto w-full px-4 sm:px-6 py-5">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
