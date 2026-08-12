# Design System

Framework: Tailwind CSS 3 (utilities only — no custom CSS files, no inline styles).  
Font: Inter (sans-serif via `font-sans`).  
Base: `body { font-sans text-gray-900 bg-gray-100 antialiased }`.

---

## Color Tokens

### Brand (custom in tailwind.config.js)

| Token | Hex | Usage |
|-------|-----|-------|
| `primary` | `#0064d2` | Buttons, links, active states, focus rings |
| `primary-600` | darker | Hover on primary button |
| `primary-700` | darker | Hover on links |
| `secondary` | `#e53238` | Danger button, cart badge, destructive actions |
| `secondary-600` | darker | Hover on danger button |
| `accent` | `#f5af02` | Highlights, star ratings, accent pills |
| `success` | `#86b817` | Positive badges, availability, verified states |

### ShopEase Aliases
| Token | Alias |
|-------|-------|
| `shopease-blue` | primary |
| `shopease-red` | secondary |
| `shopease-yellow` | accent |
| `shopease-green` | success |

### Structural (Tailwind defaults used)
| Purpose | Class |
|---------|-------|
| Page background | `bg-gray-100` (`#f3f3f3`) |
| Card background | `bg-white` |
| Primary text | `text-gray-900` (`#111820`) |
| Secondary text | `text-gray-500` |
| Muted text | `text-gray-400` |
| Border | `border-gray-200` (card), `border-gray-300` (input) |

---

## Breakpoints

| Name | Min width |
|------|-----------|
| `xs` | 480px (custom) |
| `sm` | 640px |
| `md` | 768px |
| `lg` | 1024px |
| `xl` | 1280px |
| `2xl` | 1536px |

---

## Global CSS Classes (`index.css`)

These are the only custom classes — use them before writing Tailwind utility chains.

### Buttons
```
.btn            base: inline-flex items-center justify-center gap-2 px-4 py-2
                rounded-md font-medium text-sm transition-all duration-150
                focus:ring-2 focus:ring-offset-1 disabled:opacity-50
.btn-primary    bg-primary text-white hover:bg-primary-600
.btn-secondary  bg-white text-primary border border-primary hover:bg-blue-50
.btn-danger     bg-secondary text-white hover:bg-secondary-600
.btn-ghost      bg-transparent text-gray-700 hover:bg-gray-100
```

### Forms
```
.form-input     w-full border border-gray-300 rounded-md px-3 py-2 text-sm
                focus:outline-none focus:ring-2 focus:ring-primary
.form-label     block text-sm font-medium text-gray-700 mb-1
.form-error     text-xs text-red-500 mt-1
```

### Cards
```
.card           bg-white rounded-lg border border-gray-200 shadow-sm
```

### Utility
```
.scrollbar-hide   hides scrollbar (cross-browser)
```

---

## Component Library (`src/components/common/`)

### Button
```jsx
<Button
  variant?   'primary' | 'secondary' | 'danger' | 'ghost'  // default: 'primary'
  size?      'sm' | 'md' | 'lg'                             // default: 'md'
  loading?   boolean
  disabled?  boolean
  type?      'button' | 'submit' | 'reset'
  className? string
/>
```

### Input
```jsx
<Input
  label?               string
  error?               string        // shown as .form-error below field
  helperText?          string
  required?            boolean
  showPasswordToggle?  boolean       // for type="password"
  type?                string        // default: 'text'
  className?           string
  // + all native input props via forwardRef
/>
```

### Select
```jsx
<Select
  label?       string
  error?       string
  options?     { value: string | number, label: string }[]
  placeholder? string
  required?    boolean
  className?   string
  // + all native select props via forwardRef
/>
```

### Badge
```jsx
<Badge
  variant?   'default' | 'primary' | 'success' | 'warning' | 'danger' | 'info'
             // default: 'default'
  className? string
>
  {children}
</Badge>
```

Badge variant → status mapping (listings):
| ListingStatus | Badge variant |
|---------------|--------------|
| Active | `success` |
| Draft | `default` |
| PendingApproval | `warning` |
| Rejected | `danger` |
| Sold | `info` |
| Ended | `default` |
| Removed | `danger` |

### Modal
```jsx
<Modal
  isOpen   boolean
  onClose  () => void
  title    string
  footer?  ReactNode
  size?    'sm' | 'md' | 'lg' | 'xl'  // default: 'md'
>
  {children}
</Modal>
```

### Pagination
```jsx
<Pagination
  page         number
  totalPages   number
  onPageChange (page: number) => void
/>
```

### Spinner
```jsx
<Spinner
  size?      'sm' | 'md' | 'lg' | 'xl'  // default: 'md'
  className? string
/>
```

### RatingStars
```jsx
<RatingStars
  rating?    number   // 0–5
  count?     number   // shows "(count)" label
  size?      'sm' | 'md' | 'lg'  // default: 'sm'
  className? string
/>
```

### Breadcrumbs
```jsx
<Breadcrumbs
  items? { label: string, to?: string }[]
/>
```

---

## Layouts

### MarketplaceLayout
Used for all public + authenticated non-admin routes.

Structure:
```
<Navbar>          sticky top-0 z-50, bg-white shadow-sm
  Logo            colored letters (e=red, b=blue, a=yellow, y=green) italic extrabold
  Search bar      flex-1, border-[2.5px] border-gray-900 rounded-full
                  left: category Select | center: text Input | right: primary Button
  Top bar (sm+)   utility links, Sign in/Register, Sell, cart badge
  Cart badge      absolute -top-1 -right-1.5 bg-secondary text-white text-[9px]
                  w-3.5 h-3.5 rounded-full
  Category strip  scrollable NavLink pills, active: bg-primary text-white rounded-full
  Mobile drawer   w-72 bg-white, slides from left
<main>            flex-1 max-w-7xl mx-auto w-full px-4 sm:px-6 py-6
<Footer>          4-column link grid, social icons, bg-gray-900 text-gray-300
```

### AdminLayout
Used for all `/admin/*` routes.

Structure:
```
<Sidebar>         w-64 bg-[#071b45] text-white, collapsible on mobile
  Logo area       brand name + subtitle
  Nav links       icon + label, active: bg-white/10 border-l-2 border-accent
<AdminNavbar>     h-16 sticky, bg-white border-b, breadcrumb + View Store link
                  + user avatar + sign out
  Header pills    4 colored pills: blue/red/yellow/green (decorative bar)
<main>            px-4 sm:px-6 py-5 max-w-7xl
```

---

## Page Patterns

### Listing Detail
```
Breadcrumbs row
Grid: md:grid-cols-2 gap-8
  Left:
    .card aspect-square → main image
    Gallery: overflow-x-auto flex gap-2, thumb hover:border-primary/50
             active: border-primary ring-2 ring-primary/25
  Right (space-y-5):
    Title (text-2xl font-bold) + Status Badge
    Price: text-4xl font-extrabold text-gray-900
           discount: line-through original + "You save $X" text-success
    Availability Badge (success/warning/danger)
    Qty picker: flex items-center, Minus/Plus Button ghost + count
    Add to Cart: Button primary lg, full width
    Wishlist: Button ghost
```

### Listing Card (grid item)
```
.card hover:shadow-md transition-shadow
  Image: aspect-square object-cover rounded-t-lg
  Body: p-3 space-y-1
    Title: text-sm font-medium line-clamp-2
    Price: text-lg font-bold text-primary
    FinalPrice with discount: line-through original text-xs text-gray-400
    Badges: status, FreeShipping
    RatingStars size=sm
```

### Search / Listing List Page
```
Top: filters bar (category, price range, sort)
Grid: grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4
Bottom: <Pagination>
Empty state: centered icon + heading + subtext
Loading: grid of Spinner or skeleton cards
```

### Admin Table Page
```
Header: h2 title + action Button (primary, top right)
Filters bar: search Input + Select filters inline
Table: w-full, thead bg-gray-50, th text-xs uppercase text-gray-500
       tr hover:bg-gray-50, td text-sm
       action buttons: ghost/danger icon buttons
Pagination: bottom right
Empty: centered text-gray-500
```

### Form Page
```
.card max-w-2xl mx-auto p-6
  h2 title mb-6
  form space-y-4
    <Input> / <Select> with label + error
    DynamicAttributeFields (for listing attribute section)
    Submit Button primary full-width or right-aligned
    Error summary at top if needed
```

---

## Patterns & Conventions

### Spacing
- Section spacing: `space-y-5` or `space-y-6`
- Card padding: `p-4` or `p-6`
- Grid gaps: `gap-4` (cards), `gap-8` (two-column detail)
- Page padding: `px-4 sm:px-6 py-6`

### Typography
| Role | Classes |
|------|---------|
| Page title | `text-2xl font-bold text-gray-900` |
| Section title | `text-lg font-semibold text-gray-900` |
| Body | `text-sm text-gray-700` |
| Muted | `text-sm text-gray-500` |
| Price | `text-xl font-bold text-primary` or `text-4xl font-extrabold` |
| Label | `text-sm font-medium text-gray-700` |
| Error | `text-xs text-red-500` |

### Focus & Interaction
- Focus rings: `focus:ring-2 focus:ring-primary focus:outline-none`
- Hover transitions: `transition-colors`, `transition-shadow duration-150`
- Disabled: `opacity-50 cursor-not-allowed`

### Images
- Always use `assetUrl(url)` from `utils/assets.js` for backend paths
- Placeholder: gray bg + centered icon when no image
- Object fit: `object-cover` for fixed-ratio containers

### Loading States
- Full page: centered `<Spinner size="lg">`
- Inline: `<Spinner size="sm">` inside button (use `loading` prop)
- List skeleton: repeat placeholder cards matching grid layout

### Empty States
- Centered container, gray icon (48px), bold heading, subtext, optional CTA Button
- Pattern: `flex flex-col items-center justify-center py-16 text-center`

### Error States
- Inline field: `.form-error` via Input/Select `error` prop
- Toast: `react-hot-toast` for success/error feedback after mutations
- Page-level: card with red border + error message

### Admin UI Conventions
- Dense tables: `text-xs` headers, `text-sm` cells
- Status badge in every table row
- Action buttons: icon-only ghost for edit/view, danger for delete
- Sidebar active link: `bg-white/10 border-l-2 border-accent`
- Stats cards: colored left border or top bar matching brand tokens
