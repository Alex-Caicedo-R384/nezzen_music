import { Disc3 } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { fetchHealth } from '../../lib/api'

export function RightPanel() {
  const { data, isError, isPending } = useQuery({ queryKey: ['health'], queryFn: ({ signal }) => fetchHealth(signal), refetchInterval: 15000 })
  return (
    <aside aria-label="Panel de detalles" className="flex h-full flex-col gap-4 overflow-y-auto bg-surface p-4">
      <div className="grid aspect-square w-full place-items-center rounded-xl border border-line bg-surface-2 text-faint">
        <Disc3 size={56} strokeWidth={1.25} />
      </div>
      <div>
        <p className="font-medium">Sin canción seleccionada</p>
        <p className="text-[12px] text-faint">Los detalles y la portada aparecerán aquí.</p>
      </div>
      <section className="mt-auto rounded-lg border border-line p-3 text-[12px]" aria-label="Estado del backend">
        <p className="mb-2 flex items-center gap-2 font-medium">
          <span
            className={`size-2 rounded-full ${isError ? 'bg-red-500' : isPending ? 'bg-amber-400' : 'bg-emerald-500'}`}
            aria-hidden
          />
          {isError ? 'Backend no disponible' : isPending ? 'Conectando…' : 'Backend conectado'}
        </p>
        {isError && <p className="text-muted">Inicia el backend (scripts/windows/start-backend.ps1).</p>}
        {data && (
          <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-muted">
            <dt>Versión</dt><dd className="text-right text-fg">{data.version}</dd>
            <dt>Esquema</dt><dd className="text-right text-fg">{data.schema_revision}</dd>
            <dt>Canciones</dt><dd className="text-right text-fg">{data.tracks}</dd>
            <dt>Carpetas</dt><dd className="text-right text-fg">{data.folders}</dd>
          </dl>
        )}
      </section>
    </aside>
  )
}
