import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronRight, ChevronDown } from 'lucide-react'
import { categoryService } from '@/features/categories/services/categoryService'

function CategoryItem({ category, selectedId, onSelect, level = 0 }) {
  const hasChildren = category.children?.length > 0
  const isSelected = category.id === selectedId
  const isChildSelected = hasChildren && category.children.some((s) => s.id === selectedId)

  // Level-0 with children: always expanded. Deeper levels: expand when selected/has selected child.
  const [expanded, setExpanded] = useState(
    level === 0 ? hasChildren : (isSelected || isChildSelected),
  )

  const handleClick = () => {
    // All categories navigate — ListingsPage decides browse vs listings
    onSelect(category.id, category.name)
    // Toggle expand for deeper levels only (level-0 stays always open)
    if (hasChildren && level > 0) setExpanded((e) => !e)
  }

  return (
    <div>
      <button
        onClick={handleClick}
        className={[
          'w-full text-left flex items-center gap-1.5 px-2 py-1.5 text-sm rounded-lg transition-colors',
          level > 0 ? 'pl-5' : '',
          isSelected
            ? 'bg-primary/10 text-primary font-semibold'
            : isChildSelected
              ? 'text-gray-800 font-medium bg-gray-50'
              : 'text-gray-700 hover:bg-gray-50',
        ].join(' ')}
      >
        {/* Chevron only for collapsible deeper levels */}
        {hasChildren && level > 0 ? (
          expanded
            ? <ChevronDown size={12} className="shrink-0 text-gray-400" />
            : <ChevronRight size={12} className="shrink-0 text-gray-400" />
        ) : (
          <span className="w-3 shrink-0" />
        )}
        <span className="truncate">{category.name}</span>
      </button>

      {/* Level-0 always shows children; deeper levels only when expanded */}
      {hasChildren && (level === 0 || expanded) && (
        <div>
          {category.children.map((sub) => (
            <CategoryItem
              key={sub.id}
              category={sub}
              selectedId={selectedId}
              onSelect={onSelect}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export default function CategoryFilter({ selectedId, onSelect }) {
  const { data: tree = [] } = useQuery({
    queryKey: ['categories', 'tree'],
    queryFn: categoryService.getTree,
    staleTime: 5 * 60_000,
    select: (res) => res?.data ?? [],
  })

  if (!tree.length) return null

  return (
    <div className="space-y-0.5">
      <button
        onClick={() => onSelect(null, null)}
        className={[
          'w-full text-left px-2 py-1.5 text-sm rounded-lg transition-colors',
          !selectedId
            ? 'bg-primary/10 text-primary font-semibold'
            : 'text-gray-700 hover:bg-gray-50',
        ].join(' ')}
      >
        All Categories
      </button>
      {tree.map((cat) => (
        <CategoryItem
          key={cat.id}
          category={cat}
          selectedId={selectedId}
          onSelect={onSelect}
        />
      ))}
    </div>
  )
}
