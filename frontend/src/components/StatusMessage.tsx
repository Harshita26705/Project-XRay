interface Props {
  status: string;
  error: string;
}

export default function StatusMessage({ status, error }: Props) {
  if (!status && !error) return null;
  return (
    <div className="space-y-1">
      {status && <p className="text-xs text-slate-400">{status}</p>}
      {error && (
        <p className="rounded border border-red-800 bg-red-950/60 px-2 py-1 text-xs text-red-300">
          {error}
        </p>
      )}
    </div>
  );
}
