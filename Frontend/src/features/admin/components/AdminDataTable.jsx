import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react'

export const getNextSortKey = (currentSortKey, columnSortKey, defaultDirection = 'asc') => {
  const [currentColumn, currentDirection] = currentSortKey.split('_')
  if (currentColumn !== columnSortKey) return `${columnSortKey}_${defaultDirection}`
  return `${columnSortKey}_${currentDirection === 'asc' ? 'desc' : 'asc'}`
}

function SortIcon({ active, direction }) {
  if (!active) return <ArrowUpDown size={13} className="text-gray-300" />
  return direction === 'asc'
    ? <ArrowUp size={13} className="text-primary" />
    : <ArrowDown size={13} className="text-primary" />
}

export default function AdminDataTable({
  columns,
  rows,
  getRowKey,
  sortKey,
  onSortChange,
  emptyMessage,
  rowClassName = '',
}) {
  const [activeSortColumn, activeSortDirection] = sortKey?.split('_') ?? []

  const renderHeader = (column) => {
    const sortable = Boolean(column.sortKey && onSortChange)
    const active = sortable && activeSortColumn === column.sortKey

    if (!sortable) {
      return (
        <span className="block text-left font-semibold text-gray-600">
          {column.label}
        </span>
      )
    }

    return (
      <button
        type="button"
        onClick={() => onSortChange(getNextSortKey(sortKey, column.sortKey, column.defaultDirection))}
        className="inline-flex h-8 items-center gap-1.5 rounded-md px-1.5 text-left font-semibold text-gray-600 hover:bg-gray-100 hover:text-gray-900"
      >
        <span>{column.label}</span>
        <SortIcon active={active} direction={activeSortDirection} />
      </button>
    )
  }

  return (
    <div className="overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[760px] table-fixed text-sm">
          <thead className="border-b border-gray-200 bg-gray-50">
            <tr>
              {columns.map((column) => (
                <th
                  key={column.key}
                  className={`px-4 py-3 text-left align-middle ${column.headerClassName || ''}`}
                >
                  {renderHeader(column)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {rows.map((row) => (
              <tr key={getRowKey(row)} className={`hover:bg-gray-50 ${rowClassName}`}>
                {columns.map((column) => (
                  <td key={column.key} className={`px-4 py-3 align-middle ${column.cellClassName || ''}`}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="divide-y divide-gray-100 md:hidden">
        {rows.map((row) => (
          <div key={getRowKey(row)} className="space-y-3 p-4">
            {columns.map((column) => (
              <div key={column.key} className={column.mobileClassName || ''}>
                {column.mobileLabel !== false && (
                  <p className="mb-1 text-[11px] font-bold uppercase tracking-wide text-gray-400">
                    {column.mobileLabel || column.label}
                  </p>
                )}
                <div className="min-w-0 text-sm text-gray-800">{column.render(row)}</div>
              </div>
            ))}
          </div>
        ))}
      </div>

      {!rows.length && (
        <p className="px-4 py-8 text-center text-sm text-gray-500">{emptyMessage}</p>
      )}
    </div>
  )
}
