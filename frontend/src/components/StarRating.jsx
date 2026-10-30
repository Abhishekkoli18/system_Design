// Display mode: <StarRating value={4.3} />
// Input mode:   <StarRating value={rating} onChange={setRating} />
export default function StarRating({ value = 0, onChange, size = 18 }) {
  return (
    <span className="stars" aria-label={`${value} out of 5 stars`}>
      {[1, 2, 3, 4, 5].map((s) => (
        <span
          key={s}
          className={s <= Math.round(value) ? 'star filled' : 'star'}
          style={{ fontSize: size, cursor: onChange ? 'pointer' : 'default' }}
          onClick={onChange ? () => onChange(s) : undefined}
        >
          ★
        </span>
      ))}
    </span>
  )
}