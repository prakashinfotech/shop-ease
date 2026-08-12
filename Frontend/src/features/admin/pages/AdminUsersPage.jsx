import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import Badge from '@/components/common/Badge'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import { formatDate } from '@/utils/formatters'
import { AccountType, AccountTypeLabel, UserRole } from '@/constants/enums'
import AdminDataTable from '../components/AdminDataTable'

const pageSizeOptions = [15, 30, 50, 100]

export default function AdminUsersPage() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [search, setSearch] = useState('')
  const [accountType, setAccountType] = useState('')
  const [role, setRole] = useState('')
  const [status, setStatus] = useState('')
  const [sortKey, setSortKey] = useState('createdAt_desc')
  const [sortBy, sortDirection] = sortKey.split('_')

  useEffect(() => {
    setPage(1)
  }, [accountType, pageSize, role, search, sortKey, status])

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'users', page, pageSize, search, accountType, role, status, sortKey],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.USERS, {
      params: {
        page,
        pageSize,
        search: search || undefined,
        accountType: accountType !== '' ? Number(accountType) : undefined,
        role: role || undefined,
        status: status || undefined,
        sortBy,
        sortDirection,
      },
    }),
  })

  const users = data?.items || data?.data?.items || []
  const totalPages = data?.totalPages || data?.data?.totalPages || 1
  const totalCount = data?.totalCount || data?.data?.totalCount || 0
  const columns = [
    {
      key: 'name',
      label: 'Name',
      sortKey: 'name',
      cellClassName: 'font-medium text-gray-900',
      render: (user) => <span className="break-words">{user.firstName} {user.lastName}</span>,
    },
    {
      key: 'email',
      label: 'Email',
      sortKey: 'email',
      cellClassName: 'text-gray-500',
      render: (user) => <span className="break-all">{user.email}</span>,
    },
    {
      key: 'accountType',
      label: 'Account Type',
      sortKey: 'accountType',
      render: (user) => AccountTypeLabel[user.accountType],
    },
    {
      key: 'role',
      label: 'Role',
      sortKey: 'role',
      render: (user) => (
        <Badge variant={user.role === 'Admin' ? 'danger' : 'default'}>{user.role}</Badge>
      ),
    },
    {
      key: 'joined',
      label: 'Joined',
      sortKey: 'createdAt',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (user) => formatDate(user.createdAt),
    },
    {
      key: 'status',
      label: 'Status',
      sortKey: 'status',
      render: (user) => (
        <Badge variant={user.isDeleted || user.isSuspended ? 'danger' : 'success'}>
          {user.isDeleted ? 'Deleted' : user.isSuspended ? 'Suspended' : 'Active'}
        </Badge>
      ),
    },
  ]

  const resetFilters = () => {
    setSearch('')
    setAccountType('')
    setRole('')
    setStatus('')
    setSortKey('createdAt_desc')
    setPageSize(15)
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Users</h1>
        <p className="text-sm text-gray-500">{totalCount.toLocaleString()} total users</p>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(220px,1fr)_150px_140px_140px_110px_auto]">
          <input
            type="search"
            placeholder="Search name or email"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="form-input h-10 min-w-0 text-sm sm:col-span-2 xl:col-span-1"
          />
          <select value={accountType} onChange={(e) => setAccountType(e.target.value)} className="form-input h-10 min-w-0 bg-white text-sm">
            <option value="">All accounts</option>
            <option value={AccountType.PERSONAL}>Personal</option>
            <option value={AccountType.BUSINESS}>Business</option>
          </select>
          <select value={role} onChange={(e) => setRole(e.target.value)} className="form-input h-10 min-w-0 bg-white text-sm">
            <option value="">All roles</option>
            <option value={UserRole.USER}>User</option>
            <option value={UserRole.ADMIN}>Admin</option>
          </select>
          <select value={status} onChange={(e) => setStatus(e.target.value)} className="form-input h-10 min-w-0 bg-white text-sm">
            <option value="">All statuses</option>
            <option value="active">Active</option>
            <option value="suspended">Suspended</option>
            <option value="deleted">Deleted</option>
            <option value="unverified">Unverified</option>
          </select>
          <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))} className="form-input h-10 min-w-0 bg-white text-sm">
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>{size} / page</option>
            ))}
          </select>
          <button type="button" onClick={resetFilters} className="h-10 rounded-md border border-gray-200 px-3 text-sm font-medium text-gray-600 hover:border-primary hover:text-primary">
            Reset
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : (
        <>
          <AdminDataTable
            columns={columns}
            rows={users}
            getRowKey={(user) => user.id}
            sortKey={sortKey}
            onSortChange={setSortKey}
            emptyMessage="No users found"
          />
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}
    </div>
  )
}
