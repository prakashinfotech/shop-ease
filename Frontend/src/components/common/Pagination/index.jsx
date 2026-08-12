import { ChevronLeft, ChevronRight } from 'lucide-react'

export default function Pagination({ page, totalPages, onPageChange }) {
  if (totalPages <= 1) return null

  const pages = []
  const delta = 2
  for (let i = Math.max(1, page - delta); i <= Math.min(totalPages, page + delta); i++) {
    pages.push(i)
  }

  return (
    <nav className="flex items-center justify-center gap-1 mt-6">
      <button
        onClick={() => onPageChange(page - 1)}
        disabled={page === 1}
        className="p-2 rounded-md border border-gray-300 text-gray-600 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
      >
        <ChevronLeft size={16} />
      </button>

      {pages[0] > 1 && (
        <>
          <PageBtn page={1} current={page} onClick={onPageChange} />
          {pages[0] > 2 && <span className="px-2 text-gray-400">…</span>}
        </>
      )}

      {pages.map((p) => (
        <PageBtn key={p} page={p} current={page} onClick={onPageChange} />
      ))}

      {pages[pages.length - 1] < totalPages && (
        <>
          {pages[pages.length - 1] < totalPages - 1 && <span className="px-2 text-gray-400">…</span>}
          <PageBtn page={totalPages} current={page} onClick={onPageChange} />
        </>
      )}

      <button
        onClick={() => onPageChange(page + 1)}
        disabled={page === totalPages}
        className="p-2 rounded-md border border-gray-300 text-gray-600 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
      >
        <ChevronRight size={16} />
      </button>
    </nav>
  )
}

function PageBtn({ page, current, onClick }) {
  return (
    <button
      onClick={() => onClick(page)}
      className={`w-9 h-9 rounded-md text-sm font-medium border transition-colors ${
        page === current
          ? 'bg-primary text-white border-primary'
          : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
      }`}
    >
      {page}
    </button>
  )
}