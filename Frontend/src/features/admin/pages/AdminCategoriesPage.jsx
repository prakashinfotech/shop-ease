import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, Settings2, X, CornerDownRight, ChevronDown, ChevronRight } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import Modal from '@/components/common/Modal'
import Input from '@/components/common/Input'
import Select from '@/components/common/Select'
import { AttributeDataType } from '@/constants/enums'
import { categoryService } from '@/features/categories/services/categoryService'

const typeOptions = [
  { value: AttributeDataType.TEXT, label: 'Text' },
  { value: AttributeDataType.NUMBER, label: 'Number' },
  { value: AttributeDataType.DECIMAL, label: 'Decimal' },
  { value: AttributeDataType.BOOLEAN, label: 'Checkbox' },
  { value: AttributeDataType.DATE, label: 'Date' },
  { value: AttributeDataType.DROPDOWN, label: 'Dropdown' },
  { value: AttributeDataType.MULTI_SELECT, label: 'Multi-select' },
]

const emptyAttribute = () => ({
  name: '',
  displayName: '',
  dataType: AttributeDataType.TEXT,
  isRequired: false,
  isFilterable: false,
  sortOrder: 0,
  minLength: '',
  maxLength: '',
  minValue: '',
  maxValue: '',
  placeholder: '',
  optionsText: '',
})

const flattenCategories = (items = [], depth = 0) =>
  items.flatMap((item) => [
    { value: item.id, label: `${'-- '.repeat(depth)}${item.name}`, parentCategoryId: item.parentCategoryId },
    ...flattenCategories(item.children || [], depth + 1),
  ])

const normalizeAttribute = (attribute) => ({
  name: attribute.name,
  displayName: attribute.displayName,
  dataType: attribute.dataType,
  isRequired: attribute.isRequired,
  isFilterable: attribute.isFilterable,
  sortOrder: attribute.sortOrder,
  description: attribute.description || '',
  placeholder: attribute.placeholder || '',
  unit: attribute.unit || '',
  minLength: attribute.minLength ?? '',
  maxLength: attribute.maxLength ?? '',
  minValue: attribute.minValue ?? '',
  maxValue: attribute.maxValue ?? '',
  regexPattern: attribute.regexPattern || '',
  conditionAttributeId: attribute.conditionAttributeId || '',
  conditionOperator: attribute.conditionOperator ?? '',
  conditionValue: attribute.conditionValue || '',
  optionsText: (attribute.options || []).map((option) => `${option.value}:${option.label}`).join('\n'),
})

const parseOptions = (text = '') =>
  text
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line, index) => {
      const [value, ...labelParts] = line.split(':')
      const label = labelParts.join(':').trim() || value.trim()
      return { value: value.trim(), label, sortOrder: index, isActive: true }
    })

export default function AdminCategoriesPage() {
  const qc = useQueryClient()
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState(null)
  const [attributes, setAttributes] = useState([])
  const [expandedCategories, setExpandedCategories] = useState(() => new Set())

  const { data, isLoading } = useQuery({
    queryKey: ['categories', 'tree'],
    queryFn: categoryService.getTree,
  })

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm()
  const categories = useMemo(() => data?.data || [], [data])
  const flatCategories = useMemo(() => flattenCategories(categories), [categories])
  const selectedParentCategoryId = watch('parentCategoryId')
  const isChildCategory = Boolean(selectedParentCategoryId)

  const save = useMutation({
    mutationFn: (values) => {
      const activeAttributes = values.parentCategoryId ? attributes : []
      const partiallyFilledAttribute = activeAttributes.find((attribute) =>
        Boolean(attribute.name?.trim()) !== Boolean(attribute.displayName?.trim())
      )

      if (partiallyFilledAttribute) {
        throw new Error('Each attribute needs both a key and a label.')
      }

      const payload = {
        ...values,
        name: values.name?.trim(),
        description: values.description?.trim() || null,
        imageUrl: values.imageUrl?.trim() || null,
        sortOrder: Number(values.sortOrder || 0),
        parentCategoryId: values.parentCategoryId || null,
        attributes: activeAttributes
          .filter((attribute) => attribute.name?.trim() && attribute.displayName?.trim())
          .map((attribute) => ({
          ...attribute,
          name: attribute.name.trim(),
          displayName: attribute.displayName.trim(),
          dataType: Number(attribute.dataType),
          sortOrder: Number(attribute.sortOrder || 0),
          minLength: attribute.minLength === '' ? null : Number(attribute.minLength),
          maxLength: attribute.maxLength === '' ? null : Number(attribute.maxLength),
          minValue: attribute.minValue === '' ? null : Number(attribute.minValue),
          maxValue: attribute.maxValue === '' ? null : Number(attribute.maxValue),
          conditionAttributeId: attribute.conditionAttributeId || null,
          conditionOperator: attribute.conditionOperator === '' ? null : Number(attribute.conditionOperator),
          conditionValue: attribute.conditionValue || null,
          options: parseOptions(attribute.optionsText),
        })),
      }

      return editing
        ? categoryService.update(editing.id, payload)
        : categoryService.create(payload)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success(editing ? 'Category updated' : 'Category created')
      setModalOpen(false)
      setEditing(null)
      setAttributes([])
      reset()
    },
    onError: (err) => toast.error(err?.response?.data?.message || err?.message || 'Operation failed'),
  })

  const remove = useMutation({
    mutationFn: categoryService.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Category deleted')
    },
  })

  const openCreate = (parentCategoryId = '') => {
    setEditing(null)
    setAttributes([])
    reset({ name: '', description: '', imageUrl: '', parentCategoryId, sortOrder: 0 })
    setModalOpen(true)
  }

  const openEdit = (cat) => {
    setEditing(cat)
    setAttributes((cat.attributes || []).map(normalizeAttribute))
    reset({
      name: cat.name,
      description: cat.description || '',
      imageUrl: cat.imageUrl || '',
      parentCategoryId: cat.parentCategoryId || '',
      sortOrder: cat.sortOrder || 0,
    })
    setModalOpen(true)
  }

  const updateAttribute = (index, patch) => {
    setAttributes((current) => current.map((attribute, i) => i === index ? { ...attribute, ...patch } : attribute))
  }

  const toggleChildren = (categoryId) => {
    setExpandedCategories((current) => {
      const next = new Set(current)
      if (next.has(categoryId)) next.delete(categoryId)
      else next.add(categoryId)
      return next
    })
  }

  const renderCategory = (cat, depth = 0) => {
    const hasChildren = Boolean(cat.children?.length)
    const isExpanded = expandedCategories.has(cat.id)

    return (
      <div key={cat.id} className="border-b border-gray-100 last:border-b-0">
        <div className="flex items-center justify-between px-4 py-3" style={{ paddingLeft: `${16 + depth * 24}px` }}>
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-medium text-gray-900">{cat.name}</p>
              {hasChildren && (
                <button
                  type="button"
                  onClick={() => toggleChildren(cat.id)}
                  className="inline-flex items-center gap-1 rounded-md border border-gray-200 px-2 py-1 text-xs font-medium text-gray-600 hover:border-primary/40 hover:text-primary"
                >
                  {isExpanded ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
                  {isExpanded ? 'Hide child' : `Show child (${cat.children.length})`}
                </button>
              )}
              {!!cat.attributes?.length && (
                <span className="inline-flex items-center gap-1 text-xs text-gray-500">
                  <Settings2 size={13} /> {cat.attributes.length}
                </span>
              )}
            </div>
            {cat.description && <p className="text-xs text-gray-500">{cat.description}</p>}
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => openCreate(cat.id)}
              className="p-1.5 text-gray-400 hover:text-primary hover:bg-blue-50 rounded-md"
              title="Add child category"
            >
              <CornerDownRight size={15} />
            </button>
            <button onClick={() => openEdit(cat)} className="p-1.5 text-gray-400 hover:text-primary hover:bg-blue-50 rounded-md" title="Edit category">
              <Pencil size={15} />
            </button>
            <button
              onClick={() => { if (confirm('Delete category?')) remove.mutate(cat.id) }}
              className="p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-md"
              title="Delete category"
            >
              <Trash2 size={15} />
            </button>
          </div>
        </div>
        {hasChildren && isExpanded && cat.children.map((child) => renderCategory(child, depth + 1))}
      </div>
    )
  }

  const parentOptions = flatCategories
    .filter((cat) => cat.value !== editing?.id)
    .map(({ value, label }) => ({ value, label }))

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Categories</h1>
        <Button onClick={() => openCreate()}><Plus size={16} /> Add Category</Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : (
        <div className="card overflow-hidden">
          {categories.map((cat) => renderCategory(cat))}
          {!categories.length && (
            <p className="px-4 py-8 text-center text-sm text-gray-500">No categories yet</p>
          )}
        </div>
      )}

      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title={editing ? 'Edit Category' : 'New Category'}
        size="lg"
        closeOnOutsideClick={false}
        footer={
          <>
            <Button variant="ghost" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button onClick={handleSubmit((d) => save.mutate(d))} loading={save.isPending}>
              {editing ? 'Save Changes' : 'Create'}
            </Button>
          </>
        }
      >
        <form className="space-y-5">
          <div className="grid sm:grid-cols-2 gap-4">
            <Input label="Name" required error={errors.name?.message} {...register('name', { required: 'Required' })} />
            <Input label="Sort Order" type="number" {...register('sortOrder')} />
          </div>
          <div>
            <label className="form-label">Parent Category</label>
            <select className="form-input bg-white" {...register('parentCategoryId')}>
              <option value="">Top-level category</option>
              {parentOptions.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </div>
          <Input label="Image URL" {...register('imageUrl')} />
          <div>
            <label className="form-label">Description</label>
            <textarea rows={3} className="form-input resize-none" {...register('description')} />
          </div>

          {isChildCategory ? (
            <div className="border-t border-gray-100 pt-4 space-y-3">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-bold text-gray-900">Child Category Form</h2>
                <Button size="sm" variant="secondary" onClick={() => setAttributes((current) => [...current, emptyAttribute()])}>
                  <Plus size={14} /> Add Field
                </Button>
              </div>

              <div className="space-y-3">
                {attributes.map((attribute, index) => (
                  <div key={index} className="rounded-md border border-gray-200 p-3 space-y-3">
                    <div className="flex items-start justify-between gap-3">
                      <div className="grid sm:grid-cols-3 gap-3 flex-1">
                        <Input label="Key" value={attribute.name} onChange={(e) => updateAttribute(index, { name: e.target.value })} placeholder="brand" />
                        <Input label="Label" value={attribute.displayName} onChange={(e) => updateAttribute(index, { displayName: e.target.value })} placeholder="Brand" />
                        <Select label="Type" options={typeOptions} value={attribute.dataType} onChange={(e) => updateAttribute(index, { dataType: Number(e.target.value) })} />
                      </div>
                      <button type="button" onClick={() => setAttributes((current) => current.filter((_, i) => i !== index))} className="p-1.5 text-gray-400 hover:text-red-500">
                        <X size={16} />
                      </button>
                    </div>

                    <div className="grid sm:grid-cols-4 gap-3">
                      <Input label="Sort" type="number" value={attribute.sortOrder} onChange={(e) => updateAttribute(index, { sortOrder: e.target.value })} />
                      <Input label="Min Length" type="number" value={attribute.minLength} onChange={(e) => updateAttribute(index, { minLength: e.target.value })} />
                      <Input label="Max Length" type="number" value={attribute.maxLength} onChange={(e) => updateAttribute(index, { maxLength: e.target.value })} />
                      <Input label="Placeholder" value={attribute.placeholder} onChange={(e) => updateAttribute(index, { placeholder: e.target.value })} />
                    </div>

                    <div className="grid sm:grid-cols-2 gap-3">
                      <label className="flex items-center gap-2 text-sm text-gray-700">
                        <input type="checkbox" checked={attribute.isRequired} onChange={(e) => updateAttribute(index, { isRequired: e.target.checked })} />
                        Required
                      </label>
                      <label className="flex items-center gap-2 text-sm text-gray-700">
                        <input type="checkbox" checked={attribute.isFilterable} onChange={(e) => updateAttribute(index, { isFilterable: e.target.checked })} />
                        Filterable
                      </label>
                    </div>

                    {[AttributeDataType.DROPDOWN, AttributeDataType.MULTI_SELECT].includes(Number(attribute.dataType)) && (
                      <div>
                        <label className="form-label">Options</label>
                        <textarea
                          rows={3}
                          className="form-input resize-none"
                          value={attribute.optionsText}
                          onChange={(e) => updateAttribute(index, { optionsText: e.target.value })}
                          placeholder={'new:New\nused:Used'}
                        />
                      </div>
                    )}
                  </div>
                ))}
                {!attributes.length && <p className="text-sm text-gray-500">No fields defined for this child category.</p>}
              </div>
            </div>
          ) : (
            <div className="border-t border-gray-100 pt-4">
              <p className="text-sm text-gray-500">Parent categories are grouping only. Select a parent or use the child-category action to configure a seller form.</p>
            </div>
          )}
        </form>
      </Modal>
    </div>
  )
}
