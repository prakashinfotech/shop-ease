import { useState, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import Input from '@/components/common/Input'
import Button from '@/components/common/Button'
import Badge from '@/components/common/Badge'
import Spinner from '@/components/common/Spinner'
import {
  useBusinessProfile,
  useSubmitBusinessProfile,
  useUpdateBusinessProfile,
  useSubmitForReview,
  useUploadDocument,
  useDeleteDocument,
} from '../hooks/useBusinessProfile'
import { VerificationStatus, VerificationStatusLabel, DocumentType, DocumentTypeLabel } from '@/constants/enums'
import { ROUTES } from '@/constants/routes'

const profileSchema = z.object({
  companyName: z.string().min(2, 'Company name must be at least 2 characters'),
  gstNumber: z
    .string()
    .regex(/^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/, 'Invalid GST format (e.g. 27AAPFU0939F1ZV)'),
  panNumber: z
    .string()
    .regex(/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/, 'Invalid PAN format (e.g. AAPFU0939F)'),
  businessAddress: z.string().optional(),
  businessPhone: z.string().optional(),
  businessEmail: z.string().email('Invalid email').optional().or(z.literal('')),
  businessWebsite: z.string().optional(),
})

const statusConfig = {
  [VerificationStatus.DRAFT]: { variant: 'default', label: 'Draft' },
  [VerificationStatus.PENDING]: { variant: 'warning', label: VerificationStatusLabel[VerificationStatus.PENDING] },
  [VerificationStatus.UNDER_REVIEW]: { variant: 'info', label: VerificationStatusLabel[VerificationStatus.UNDER_REVIEW] },
  [VerificationStatus.VERIFIED]: { variant: 'success', label: VerificationStatusLabel[VerificationStatus.VERIFIED] },
  [VerificationStatus.REJECTED]: { variant: 'danger', label: VerificationStatusLabel[VerificationStatus.REJECTED] },
}

export default function BusinessProfilePage() {
  const { data: profile, isLoading } = useBusinessProfile()
  const { mutate: submit, isPending: isSubmitting } = useSubmitBusinessProfile()
  const { mutate: update, isPending: isUpdating } = useUpdateBusinessProfile()
  const { mutate: submitForReview, isPending: isSubmittingForReview } = useSubmitForReview()

  const isEditable = !profile ||
    profile.verificationStatus === VerificationStatus.DRAFT ||
    profile.verificationStatus === VerificationStatus.REJECTED

  const canSubmitForReview =
    profile &&
    (profile.verificationStatus === VerificationStatus.DRAFT ||
     profile.verificationStatus === VerificationStatus.REJECTED) &&
    (profile.documents?.length ?? 0) > 0

  const awaitingReview =
    profile?.verificationStatus === VerificationStatus.PENDING ||
    profile?.verificationStatus === VerificationStatus.UNDER_REVIEW

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(profileSchema),
    defaultValues: profile
      ? {
          companyName: profile.companyName,
          gstNumber: profile.gstNumber,
          panNumber: profile.panNumber,
          businessAddress: profile.businessAddress ?? '',
          businessPhone: profile.businessPhone ?? '',
          businessEmail: profile.businessEmail ?? '',
          businessWebsite: profile.businessWebsite ?? '',
        }
      : {},
  })

  const onSubmit = (data) => {
    const payload = {
      ...data,
      businessEmail: data.businessEmail || null,
      businessAddress: data.businessAddress || null,
      businessPhone: data.businessPhone || null,
      businessWebsite: data.businessWebsite || null,
    }
    if (!profile) submit(payload)
    else update(payload)
  }

  const handleSubmitForReview = () => submitForReview()

  if (isLoading) {
    return (
      <div className="flex justify-center py-20">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="max-w-3xl mx-auto px-4 py-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Business Profile</h1>
          <p className="text-sm text-gray-500 mt-1">
            Complete your business details to unlock seller features
          </p>
        </div>
        {profile && (
          <StatusBadge status={profile.verificationStatus} />
        )}
      </div>

      {profile?.verificationStatus === VerificationStatus.DRAFT && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 space-y-1">
          <p className="text-sm font-semibold text-blue-800">Step 1 done — details saved</p>
          <p className="text-sm text-blue-700">
            Upload at least one supporting document below, then click <strong>Submit for Review</strong>.
          </p>
        </div>
      )}

      {profile?.verificationStatus === VerificationStatus.REJECTED && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-sm font-medium text-red-800">Profile Rejected</p>
          <p className="text-sm text-red-700 mt-1">{profile.rejectionReason}</p>
          <p className="text-xs text-red-600 mt-2">
            Update details and/or upload documents, then submit for review again.
          </p>
        </div>
      )}

      {awaitingReview && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
          <p className="text-sm font-semibold text-amber-800">Under admin review</p>
          <p className="text-sm text-amber-700 mt-1">
            Your profile and documents are being reviewed. You can create listings once approved.
          </p>
        </div>
      )}

      {profile?.verificationStatus === VerificationStatus.VERIFIED && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <p className="text-sm font-medium text-green-800">
            Business verified on {new Date(profile.verifiedAt).toLocaleDateString()}
          </p>
        </div>
      )}

      {/* Profile Form */}
      <div className="card p-6">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Business Details</h2>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Company Name"
            placeholder="Acme Pvt Ltd"
            required
            disabled={!isEditable}
            error={errors.companyName?.message}
            {...register('companyName')}
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              label="GST Number"
              placeholder="27AAPFU0939F1ZV"
              required
              disabled={!isEditable}
              error={errors.gstNumber?.message}
              {...register('gstNumber')}
            />
            <Input
              label="PAN Number"
              placeholder="AAPFU0939F"
              required
              disabled={!isEditable}
              error={errors.panNumber?.message}
              {...register('panNumber')}
            />
          </div>

          <Input
            label="Business Address"
            placeholder="123 Business Park, Mumbai"
            disabled={!isEditable}
            error={errors.businessAddress?.message}
            {...register('businessAddress')}
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              label="Business Phone"
              type="tel"
              placeholder="+91 98765 43210"
              disabled={!isEditable}
              error={errors.businessPhone?.message}
              {...register('businessPhone')}
            />
            <Input
              label="Business Email"
              type="email"
              placeholder="info@acme.com"
              disabled={!isEditable}
              error={errors.businessEmail?.message}
              {...register('businessEmail')}
            />
          </div>

          <Input
            label="Website"
            placeholder="https://www.acme.com"
            disabled={!isEditable}
            error={errors.businessWebsite?.message}
            {...register('businessWebsite')}
          />

          {isEditable && (
            <Button
              type="submit"
              loading={isSubmitting || isUpdating}
              className="w-full sm:w-auto"
            >
              {profile ? 'Save Changes' : 'Save Details'}
            </Button>
          )}
        </form>
      </div>

      {/* Documents — visible once profile is saved */}
      {profile && (
        <DocumentsSection
          documents={profile.documents}
          profileStatus={profile.verificationStatus}
        />
      )}

      {/* Submit for Review */}
      {profile && (profile.verificationStatus === VerificationStatus.DRAFT || profile.verificationStatus === VerificationStatus.REJECTED) && (
        <div className="card p-5 border-2 border-dashed border-gray-200">
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
            <div>
              <p className="text-sm font-semibold text-gray-800">Ready to submit?</p>
              <p className="text-xs text-gray-500 mt-0.5">
                {(profile.documents?.length ?? 0) === 0
                  ? 'Upload at least one document above before submitting.'
                  : `${profile.documents.length} document${profile.documents.length > 1 ? 's' : ''} attached — you can submit for review.`}
              </p>
            </div>
            <Button
              onClick={handleSubmitForReview}
              loading={isSubmittingForReview}
              disabled={!canSubmitForReview}
              className="whitespace-nowrap"
            >
              Submit for Review
            </Button>
          </div>
        </div>
      )}

      <p className="text-sm text-gray-500 text-center">
        <Link to={ROUTES.PROFILE} className="text-primary hover:underline">
          ← Back to profile
        </Link>
      </p>
    </div>
  )
}

function StatusBadge({ status }) {
  const cfg = statusConfig[status] ?? { variant: 'default', label: status }
  return <Badge variant={cfg.variant}>{cfg.label}</Badge>
}

function DocumentsSection({ documents, profileStatus }) {
  const fileRef = useRef(null)
  const [selectedType, setSelectedType] = useState(DocumentType.GST_CERTIFICATE)
  const { mutate: upload, isPending: isUploading } = useUploadDocument()
  const { mutate: deleteDoc } = useDeleteDocument()

  const canUpload = profileStatus !== VerificationStatus.VERIFIED

  const handleUpload = (e) => {
    const file = e.target.files?.[0]
    if (!file) return
    upload({ file, documentType: selectedType })
    e.target.value = ''
  }

  return (
    <div className="card p-6">
      <h2 className="text-base font-semibold text-gray-900 mb-1">Supporting Documents</h2>
      <p className="text-xs text-gray-500 mb-4">
        Upload GST certificate, PAN card, or other business documents (PDF, JPEG, PNG · max 10 MB)
      </p>

      {canUpload && (
        <div className="flex flex-col sm:flex-row gap-3 mb-5">
          <select
            value={selectedType}
            onChange={(e) => setSelectedType(Number(e.target.value))}
            className="form-input text-sm flex-1"
          >
            {Object.entries(DocumentTypeLabel).map(([val, label]) => (
              <option key={val} value={val}>{label}</option>
            ))}
          </select>
          <Button
            variant="secondary"
            onClick={() => fileRef.current?.click()}
            loading={isUploading}
            disabled={isUploading}
          >
            Upload Document
          </Button>
          <input
            ref={fileRef}
            type="file"
            className="hidden"
            accept=".pdf,.jpg,.jpeg,.png"
            onChange={handleUpload}
          />
        </div>
      )}

      {documents.length === 0 ? (
        <p className="text-sm text-gray-400 text-center py-6">No documents uploaded yet</p>
      ) : (
        <ul className="divide-y divide-gray-100">
          {documents.map((doc) => (
            <li key={doc.id} className="py-3 flex items-center justify-between gap-3">
              <div className="min-w-0">
                <p className="text-sm font-medium text-gray-900 truncate">{doc.fileName}</p>
                <p className="text-xs text-gray-500">
                  {doc.documentType} · {(doc.fileSizeBytes / 1024).toFixed(1)} KB
                </p>
              </div>
              <div className="flex items-center gap-2 flex-shrink-0">
                <a
                  href={`${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'}${doc.fileUrl}`}
                  target="_blank"
                  rel="noreferrer"
                  className="text-xs text-primary hover:underline"
                >
                  View
                </a>
                {canUpload && (
                  <button
                    onClick={() => deleteDoc(doc.id)}
                    className="text-xs text-red-500 hover:underline"
                  >
                    Remove
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
