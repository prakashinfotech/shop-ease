import { forwardRef, useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'

const Input = forwardRef(function Input(
  { label, error, helperText, required, className = '', showPasswordToggle = false, type = 'text', ...props },
  ref
) {
  const [showPassword, setShowPassword] = useState(false)
  const canTogglePassword = showPasswordToggle && type === 'password'
  const inputType = canTogglePassword && showPassword ? 'text' : type

  return (
    <div className="w-full">
      {label && (
        <label className="form-label">
          {label}
          {required && <span className="text-red-500 ml-0.5">*</span>}
        </label>
      )}
      <div className={canTogglePassword ? 'relative' : undefined}>
        <input
          ref={ref}
          type={inputType}
          className={`form-input ${canTogglePassword ? 'pr-10' : ''} ${error ? 'border-red-400 focus:ring-red-400' : ''} ${className}`}
          {...props}
        />
        {canTogglePassword && (
          <button
            type="button"
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            title={showPassword ? 'Hide password' : 'Show password'}
            className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-gray-500 hover:text-gray-700 focus:outline-none focus:text-primary"
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => setShowPassword((current) => !current)}
          >
            {showPassword ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}
          </button>
        )}
      </div>
      {error && <p className="form-error">{error}</p>}
      {helperText && !error && <p className="text-xs text-gray-500 mt-1">{helperText}</p>}
    </div>
  )
})

export default Input
