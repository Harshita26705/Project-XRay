export function FilterSelect({
  value,
  onChange,
  label,
  options,
  renderOption
}: {
  value: string;
  onChange: (v: string) => void;
  label: string;
  options: string[];
  renderOption?: (option: string) => string;
}) {
  const display = (o: string) => (renderOption ? renderOption(o) : o === 'ALL' ? `All ${label}` : o.charAt(0) + o.slice(1).toLowerCase());
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      aria-label={label}
      className="rounded border border-border bg-cardMuted px-2 py-1.5 text-xs text-text-secondary focus:outline-none focus:ring-1 focus:ring-primary"
    >
      {options.map((o) => (
        <option key={o} value={o}>
          {display(o)}
        </option>
      ))}
    </select>
  );
}
