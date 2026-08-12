const EMOJI_MAP = {
  'Electronics': '📱',
  'Fashion': '👗',
  'Home & Garden': '🏡',
  'Vehicles': '🚗',
  'Sports': '⚽',
  'Toys': '🧸',
  'Books': '📚',
  'Collectibles': '🏆',
}

export default function CategoryBrowserGrid({ categories, onSelect }) {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5 gap-4">
      {categories.map((cat) => (
        <button
          key={cat.id}
          onClick={() => onSelect(cat.id, cat.name)}
          className="card p-5 flex flex-col items-center gap-3 hover:shadow-md hover:border-primary transition-all text-center group cursor-pointer"
        >
          <span className="text-4xl">{EMOJI_MAP[cat.name] ?? '🏷️'}</span>
          <div>
            <p className="text-sm font-semibold text-gray-800 group-hover:text-primary transition-colors leading-snug">
              {cat.name}
            </p>
            {cat.children?.length > 0 && (
              <p className="text-xs text-gray-400 mt-1">
                {cat.children.length} subcategor{cat.children.length === 1 ? 'y' : 'ies'}
              </p>
            )}
          </div>
        </button>
      ))}
    </div>
  )
}
