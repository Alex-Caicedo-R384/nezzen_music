import { VIEWS, type ViewId } from '../../features/library/views'
import { cn } from '../../lib/utils'

interface Props {
  active: ViewId
  onSelect: (id: ViewId) => void
}

export function Sidebar({ active, onSelect }: Props) {
  return (
    <nav aria-label="Biblioteca" className="flex h-full flex-col gap-0.5 overflow-y-auto bg-surface p-2">
      <p className="px-2.5 pb-1 pt-2 text-[11px] uppercase tracking-wider text-faint">Biblioteca</p>
      {VIEWS.map(({ id, label, icon: Icon }) => (
        <button
          key={id}
          onClick={() => onSelect(id)}
          aria-current={active === id ? 'page' : undefined}
          className={cn(
            'flex items-center gap-2.5 rounded-md px-2.5 py-2 text-left text-muted transition-colors hover:bg-surface-2 hover:text-fg',
            active === id && 'bg-accent-soft font-medium text-fg',
          )}
        >
          <Icon size={16} className={cn(active === id && 'text-accent')} />
          <span className="truncate">{label}</span>
        </button>
      ))}
    </nav>
  )
}
