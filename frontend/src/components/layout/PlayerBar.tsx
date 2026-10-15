import { ListMusic, Music, Play, Repeat, Shuffle, SkipBack, SkipForward, Volume2 } from 'lucide-react'

// Entrega A: el reproductor es solo estructura. Todos los controles están deshabilitados
// de forma explícita; la reproducción real llega en la Entrega D.
const ctl = 'grid size-8 place-items-center rounded-full text-muted disabled:opacity-40'

export function PlayerBar() {
  return (
    <footer
      aria-label="Reproductor"
      className="grid h-[72px] shrink-0 grid-cols-[1fr_minmax(320px,560px)_1fr] items-center gap-4 border-t border-line bg-surface px-4"
    >
      <div className="flex min-w-0 items-center gap-3">
        <div className="grid size-12 shrink-0 place-items-center rounded-md border border-line bg-surface-2 text-faint">
          <Music size={20} />
        </div>
        <div className="min-w-0">
          <p className="truncate font-medium">Nada en reproducción</p>
          <p className="truncate text-[12px] text-faint">La reproducción llega en la Entrega D</p>
        </div>
      </div>

      <div className="flex flex-col items-center gap-1">
        <div className="flex items-center gap-1">
          <button disabled className={ctl} aria-label="Aleatorio"><Shuffle size={15} /></button>
          <button disabled className={ctl} aria-label="Anterior"><SkipBack size={16} /></button>
          <button disabled className="grid size-9 place-items-center rounded-full bg-accent text-accent-fg opacity-40" aria-label="Reproducir"><Play size={16} /></button>
          <button disabled className={ctl} aria-label="Siguiente"><SkipForward size={16} /></button>
          <button disabled className={ctl} aria-label="Repetir"><Repeat size={15} /></button>
        </div>
        <div className="flex w-full items-center gap-2 text-[11px] text-faint tabular-nums">
          <span>0:00</span>
          <div className="h-1 flex-1 rounded-full bg-surface-2" />
          <span>0:00</span>
        </div>
      </div>

      <div className="flex items-center justify-end gap-1 text-faint">
        <button disabled className={ctl} aria-label="Cola"><ListMusic size={16} /></button>
        <Volume2 size={16} />
        <div className="h-1 w-24 rounded-full bg-surface-2" />
      </div>
    </footer>
  )
}
