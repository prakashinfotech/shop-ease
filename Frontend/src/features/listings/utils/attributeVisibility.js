export const isAttributeVisible = (attribute, values = {}) => {
  if (!attribute.conditionAttributeId) return true

  const actual = values?.[attribute.conditionAttributeId]
  const expected = attribute.conditionValue ?? ''

  if (actual === undefined || actual === null || actual === '') return false

  switch (attribute.conditionOperator) {
    case 1:
      return String(actual).toLowerCase() !== expected.toLowerCase()
    case 2:
      return String(actual).toLowerCase().includes(expected.toLowerCase())
    case 3:
      return Number(actual) > Number(expected)
    case 4:
      return Number(actual) < Number(expected)
    default:
      return String(actual).toLowerCase() === expected.toLowerCase()
  }
}
