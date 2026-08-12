import { AttributeDataType } from '@/constants/enums'
import { isAttributeVisible } from '../utils/attributeVisibility'

export default function DynamicAttributeFields({ attributes = [], register, errors = {}, watch }) {
  const values = watch('attributes') || {}
  const visibleAttributes = attributes.filter((attribute) => isAttributeVisible(attribute, values))

  if (!visibleAttributes.length) return null

  return (
    <div className="space-y-4">
      <div className="border-t border-gray-100 pt-5">
        <h2 className="text-base font-bold text-gray-900">Category Details</h2>
      </div>

      <div className="grid sm:grid-cols-2 gap-4">
        {visibleAttributes.map((attribute) => {
          const fieldName = `attributes.${attribute.id}`
          const error = errors?.attributes?.[attribute.id]?.message
          const rules = {
            required: attribute.isRequired ? `${attribute.displayName} is required` : false,
            minLength: attribute.minLength
              ? { value: attribute.minLength, message: `Minimum ${attribute.minLength} characters` }
              : undefined,
            maxLength: attribute.maxLength
              ? { value: attribute.maxLength, message: `Maximum ${attribute.maxLength} characters` }
              : undefined,
            min: attribute.minValue !== null && attribute.minValue !== undefined
              ? { value: attribute.minValue, message: `Minimum ${attribute.minValue}` }
              : undefined,
            max: attribute.maxValue !== null && attribute.maxValue !== undefined
              ? { value: attribute.maxValue, message: `Maximum ${attribute.maxValue}` }
              : undefined,
          }

          if (attribute.dataType === AttributeDataType.BOOLEAN) {
            return (
              <label key={attribute.id} className="flex items-center gap-3 rounded-md border border-gray-200 px-3 py-2">
                <input type="checkbox" className="w-4 h-4 text-primary rounded border-gray-300" {...register(fieldName)} />
                <span className="text-sm font-medium text-gray-700">{attribute.displayName}</span>
              </label>
            )
          }

          if (attribute.dataType === AttributeDataType.DROPDOWN) {
            return (
              <div key={attribute.id}>
                <label className="form-label">
                  {attribute.displayName}
                  {attribute.isRequired && <span className="text-red-500 ml-0.5">*</span>}
                </label>
                <select className={`form-input bg-white ${error ? 'border-red-400' : ''}`} {...register(fieldName, rules)}>
                  <option value="">Select {attribute.displayName}</option>
                  {attribute.options?.filter((o) => o.isActive).map((option) => (
                    <option key={option.id || option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
                {error && <p className="form-error">{error}</p>}
              </div>
            )
          }

          if (attribute.dataType === AttributeDataType.MULTI_SELECT) {
            return (
              <div key={attribute.id} className="sm:col-span-2">
                <label className="form-label">
                  {attribute.displayName}
                  {attribute.isRequired && <span className="text-red-500 ml-0.5">*</span>}
                </label>
                <div className="grid sm:grid-cols-3 gap-2">
                  {attribute.options?.filter((o) => o.isActive).map((option) => (
                    <label key={option.id || option.value} className="flex items-center gap-2 rounded-md border border-gray-200 px-3 py-2 text-sm">
                      <input
                        type="checkbox"
                        value={option.value}
                        className="w-4 h-4 text-primary rounded border-gray-300"
                        {...register(fieldName, rules)}
                      />
                      {option.label}
                    </label>
                  ))}
                </div>
                {error && <p className="form-error">{error}</p>}
              </div>
            )
          }

          const inputType = attribute.dataType === AttributeDataType.DATE
            ? 'date'
            : attribute.dataType === AttributeDataType.NUMBER || attribute.dataType === AttributeDataType.DECIMAL
              ? 'number'
              : 'text'

          return (
            <div key={attribute.id}>
              <label className="form-label">
                {attribute.displayName}
                {attribute.isRequired && <span className="text-red-500 ml-0.5">*</span>}
              </label>
              <input
                type={inputType}
                step={attribute.dataType === AttributeDataType.DECIMAL ? '0.01' : undefined}
                placeholder={attribute.placeholder || ''}
                className={`form-input ${error ? 'border-red-400' : ''}`}
                {...register(fieldName, rules)}
              />
              {attribute.unit && !error && <p className="text-xs text-gray-500 mt-1">{attribute.unit}</p>}
              {error && <p className="form-error">{error}</p>}
            </div>
          )
        })}
      </div>
    </div>
  )
}
