import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Eye, Pencil, Plus, Power, PowerOff, Trash2, X } from 'lucide-react'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import { EmailTemplateType, EmailTemplateTypeLabel } from '@/constants/enums'
import Badge from '@/components/common/Badge'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import { formatDate } from '@/utils/formatters'
import toast from 'react-hot-toast'
import AdminDataTable from '../components/AdminDataTable'

const TYPE_OPTIONS = Object.entries(EmailTemplateTypeLabel).map(([value, label]) => ({ value: Number(value), label }))

const typeVariant = {
  [EmailTemplateType.EMAIL_VERIFICATION]: 'primary',
  [EmailTemplateType.FORGOT_PASSWORD]: 'warning',
  [EmailTemplateType.PASSWORD_CHANGED]: 'default',
  [EmailTemplateType.LISTING_PENDING_APPROVAL]: 'warning',
  [EmailTemplateType.LISTING_APPROVED]: 'success',
  [EmailTemplateType.LISTING_REJECTED]: 'danger',
}

// ── Form modal ─────────────────────────────────────────────────────────────

function TemplateFormModal({ template, onClose, onSaved }) {
  const isEdit = Boolean(template?.id)
  const [form, setForm] = useState({
    name: template?.name ?? '',
    subject: template?.subject ?? '',
    htmlBody: template?.htmlBody ?? '',
    templateType: template?.templateType ?? EmailTemplateType.EMAIL_VERIFICATION,
  })
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setSaving(true)
    try {
      if (isEdit) {
        await api.put(API_ENDPOINTS.ADMIN.EMAIL_TEMPLATE_BY_ID(template.id), {
          name: form.name,
          subject: form.subject,
          htmlBody: form.htmlBody,
        })
        toast.success('Template updated')
      } else {
        await api.post(API_ENDPOINTS.ADMIN.EMAIL_TEMPLATES, form)
        toast.success('Template created')
      }
      onSaved()
    } catch {
      toast.error('Failed to save template')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 px-4 py-8">
      <div className="w-full max-w-3xl rounded-xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
          <h2 className="text-lg font-bold text-gray-900">{isEdit ? 'Edit Template' : 'New Template'}</h2>
          <button type="button" onClick={onClose} className="text-gray-400 hover:text-gray-600"><X size={20} /></button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="form-label">Template Name</label>
              <input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                className="form-input" required maxLength={200} />
            </div>
            <div>
              <label className="form-label">Template Type</label>
              <select value={form.templateType} onChange={e => setForm(p => ({ ...p, templateType: Number(e.target.value) }))}
                className="form-input bg-white" disabled={isEdit}>
                {TYPE_OPTIONS.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
              </select>
            </div>
          </div>

          <div>
            <label className="form-label">Subject</label>
            <input value={form.subject} onChange={e => setForm(p => ({ ...p, subject: e.target.value }))}
              className="form-input" required maxLength={500} />
          </div>

          <div>
            <label className="form-label">
              HTML Body
              <span className="ml-2 text-xs font-normal text-gray-400">
                Placeholders: {'{{UserName}}'}, {'{{VerificationLink}}'}, {'{{ResetLink}}'}, {'{{ListingTitle}}'}, {'{{ListingUrl}}'}, {'{{RejectionReason}}'}
              </span>
            </label>
            <textarea value={form.htmlBody} onChange={e => setForm(p => ({ ...p, htmlBody: e.target.value }))}
              className="form-input font-mono text-xs" rows={14} required />
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" disabled={saving}>{saving ? 'Saving...' : isEdit ? 'Save Changes' : 'Create Template'}</Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Preview modal ──────────────────────────────────────────────────────────

function PreviewModal({ template, onClose }) {
  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 px-4 py-8">
      <div className="w-full max-w-3xl rounded-xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-gray-900">{template.name}</h2>
            <p className="text-sm text-gray-500">Subject: {template.subject}</p>
          </div>
          <button type="button" onClick={onClose} className="text-gray-400 hover:text-gray-600"><X size={20} /></button>
        </div>
        <div className="p-4">
          <iframe
            srcDoc={template.htmlBody}
            title="Email Preview"
            className="w-full rounded border border-gray-200"
            style={{ height: 600 }}
            sandbox="allow-same-origin"
          />
        </div>
      </div>
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────

export default function AdminEmailTemplatesPage() {
  const queryClient = useQueryClient()
  const [typeFilter, setTypeFilter] = useState('')
  const [sortKey, setSortKey] = useState('updatedAt_desc')
  const [formModal, setFormModal] = useState(null) // null | {} (new) | template (edit)
  const [previewTemplate, setPreviewTemplate] = useState(null)

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'email-templates', typeFilter],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.EMAIL_TEMPLATES, {
      params: { pageSize: 100, type: typeFilter !== '' ? Number(typeFilter) : undefined },
    }),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'email-templates'] })

  const activateMutation = useMutation({
    mutationFn: (id) => api.patch(API_ENDPOINTS.ADMIN.EMAIL_TEMPLATE_ACTIVATE(id)),
    onSuccess: () => { toast.success('Template activated'); invalidate() },
    onError: () => toast.error('Failed to activate'),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id) => api.patch(API_ENDPOINTS.ADMIN.EMAIL_TEMPLATE_DEACTIVATE(id)),
    onSuccess: () => { toast.success('Template deactivated'); invalidate() },
    onError: () => toast.error('Failed to deactivate'),
  })

  const deleteMutation = useMutation({
    mutationFn: (id) => api.delete(API_ENDPOINTS.ADMIN.EMAIL_TEMPLATE_BY_ID(id)),
    onSuccess: () => { toast.success('Template deleted'); invalidate() },
    onError: () => toast.error('Failed to delete'),
  })

  const templates = data?.items || data?.data?.items || []
  const sortedTemplates = useMemo(() => {
    const [sortBy, sortDirection] = sortKey.split('_')
    const factor = sortDirection === 'asc' ? 1 : -1

    const valueFor = (template) => {
      switch (sortBy) {
        case 'name':
          return template.name || ''
        case 'type':
          return template.templateTypeName || ''
        case 'subject':
          return template.subject || ''
        case 'version':
          return Number(template.version || 0)
        case 'status':
          return template.isActive ? 1 : 0
        case 'createdAt':
          return new Date(template.createdAt).getTime()
        default:
          return new Date(template.updatedAt).getTime()
      }
    }

    return [...templates].sort((a, b) => {
      const aValue = valueFor(a)
      const bValue = valueFor(b)
      if (typeof aValue === 'number' && typeof bValue === 'number') {
        return (aValue - bValue) * factor
      }
      return String(aValue).localeCompare(String(bValue)) * factor
    })
  }, [sortKey, templates])

  const columns = [
    {
      key: 'name',
      label: 'Name',
      sortKey: 'name',
      cellClassName: 'font-medium text-gray-900',
      render: (template) => <span className="break-words">{template.name}</span>,
    },
    {
      key: 'type',
      label: 'Type',
      sortKey: 'type',
      render: (template) => (
        <Badge variant={typeVariant[template.templateType]}>{template.templateTypeName}</Badge>
      ),
    },
    {
      key: 'subject',
      label: 'Subject',
      sortKey: 'subject',
      cellClassName: 'text-gray-500',
      render: (template) => <span className="break-words">{template.subject}</span>,
    },
    {
      key: 'version',
      label: 'Version',
      sortKey: 'version',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (template) => `v${template.version}`,
    },
    {
      key: 'status',
      label: 'Status',
      sortKey: 'status',
      defaultDirection: 'desc',
      render: (template) => (
        <Badge variant={template.isActive ? 'success' : 'default'}>{template.isActive ? 'Active' : 'Inactive'}</Badge>
      ),
    },
    {
      key: 'updated',
      label: 'Updated',
      sortKey: 'updatedAt',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (template) => formatDate(template.updatedAt),
    },
    {
      key: 'actions',
      label: 'Actions',
      mobileLabel: false,
      render: (template) => (
        <div className="flex items-center gap-1">
          <button
            title="Preview"
            onClick={() => setPreviewTemplate(template)}
            className="rounded p-1.5 text-gray-400 transition-colors hover:bg-blue-50 hover:text-primary"
          >
            <Eye size={15} />
          </button>
          <button
            title="Edit"
            onClick={() => setFormModal(template)}
            className="rounded p-1.5 text-gray-400 transition-colors hover:bg-blue-50 hover:text-primary"
          >
            <Pencil size={15} />
          </button>
          {template.isActive ? (
            <button
              title="Deactivate"
              onClick={() => deactivateMutation.mutate(template.id)}
              className="rounded p-1.5 text-gray-400 transition-colors hover:bg-yellow-50 hover:text-yellow-600"
            >
              <PowerOff size={15} />
            </button>
          ) : (
            <button
              title="Activate"
              onClick={() => activateMutation.mutate(template.id)}
              className="rounded p-1.5 text-gray-400 transition-colors hover:bg-green-50 hover:text-green-600"
            >
              <Power size={15} />
            </button>
          )}
          <button
            title="Delete"
            onClick={() => { if (confirm('Delete this template?')) deleteMutation.mutate(template.id) }}
            className="rounded p-1.5 text-gray-400 transition-colors hover:bg-red-50 hover:text-red-600"
          >
            <Trash2 size={15} />
          </button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      {formModal !== null && (
        <TemplateFormModal
          template={formModal?.id ? formModal : null}
          onClose={() => setFormModal(null)}
          onSaved={() => { setFormModal(null); invalidate() }}
        />
      )}
      {previewTemplate && (
        <PreviewModal template={previewTemplate} onClose={() => setPreviewTemplate(null)} />
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Email Templates</h1>
          <p className="text-sm text-gray-500">Manage database-driven email templates</p>
        </div>
        <Button onClick={() => setFormModal({})}><Plus size={16} /> New Template</Button>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)} className="form-input h-10 w-full bg-white text-sm sm:w-64">
          <option value="">All types</option>
          {TYPE_OPTIONS.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
        </select>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : (
        <AdminDataTable
          columns={columns}
          rows={sortedTemplates}
          getRowKey={(template) => template.id}
          sortKey={sortKey}
          onSortChange={setSortKey}
          emptyMessage="No templates found"
        />
      )}
    </div>
  )
}
