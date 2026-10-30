// Calculates which page numbers to actually show, collapsing the rest into "..."
// Example: current=47, total=100 → [1, '...', 45, 46, 47, 48, 49, '...', 100]
export function getPageNumbers(current, total) {
  const pages = new Set()

  pages.add(1)
  pages.add(total)

  for (let i = current - 2; i <= current + 2; i++) {
    if (i >= 1 && i <= total) {
      pages.add(i)
    }
  }

  const sorted = [...pages].sort((a, b) => a - b)

  const result = []
  for (let i = 0; i < sorted.length; i++) {
    if (i > 0 && sorted[i] - sorted[i - 1] > 1) {
      result.push('...')
    }
    result.push(sorted[i])
  }

  return result
}