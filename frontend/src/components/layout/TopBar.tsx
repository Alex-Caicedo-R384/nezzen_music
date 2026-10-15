import { ChevronDown, Moon, PanelRight, RotateCcw, Search, Settings, Sun } from 'lucide-react'
import { ACCENTS, useSettingsStore } from '../../features/settings/useSettingsStore'
import { cn } from '../../lib/utils'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu'

const iconBtn =
  'grid size-8 place-items-center rounded-md text-muted transition-colors hover:bg-surface-2 hover:text-fg disabled:opacity-40 disabled:hover:bg-transparent'

function MenuButton({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger className="flex items-center gap-1 rounded-md px-2.5 py-1 text-muted outline-none hover:bg-surface-2 hover:text-fg data-[state=open]:bg-surface-2 data-[state=open]:text-fg">
        {label}
        <ChevronDown size={12} />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start">{children}</DropdownMenuContent>
    </DropdownMenu>
  )
}

const Pending = ({ children, when }: { children: string; when: string }) => (
  <DropdownMenuItem disabled>
    {children}
    <span className="text-[10px]">{when}</span>
  </DropdownMenuItem>
)

export function TopBar() {
  const { theme, setTheme, accent, setAccent, rightPanelOpen, toggleRightPanel, reset } = useSettingsStore()
  return (
    <header className="flex h-12 shrink-0 items-center gap-3 border-b border-line bg-surface px-3">
      <div className="flex items-center gap-2 pr-2">
        <div className="size-6 rounded-md bg-accent" aria-hidden style={{ clipPath: 'polygon(0 100%, 0 0, 28% 0, 72% 62%, 72% 0, 100% 0, 100% 100%, 72% 100%, 28% 38%, 28% 100%)' }} />
        <span className="text-[15px] font-semibold tracking-tight">Nezzen Music</span>
      </div>

      <nav className="flex items-center text-[13px]">
        <MenuButton label="Archivo">
          <Pending when="Entrega B">Añadir carpeta…</Pending>
          <Pending when="Entrega B">Escanear biblioteca</Pending>
        </MenuButton>
        <MenuButton label="Ver">
          <DropdownMenuItem onSelect={toggleRightPanel}>{rightPanelOpen ? 'Ocultar' : 'Mostrar'} panel derecho</DropdownMenuItem>
          <DropdownMenuItem onSelect={() => setTheme(theme === 'dark' ? 'light' : 'dark')}>
            Tema {theme === 'dark' ? 'claro' : 'oscuro'}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <Pending when="Entrega E">Modo cine</Pending>
        </MenuButton>
        <MenuButton label="Controles">
          <Pending when="Entrega D">Reproducir / Pausar</Pending>
          <Pending when="Entrega D">Siguiente</Pending>
        </MenuButton>
      </nav>

      <div className="relative mx-auto w-full max-w-md">
        <Search size={14} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-faint" />
        <input
          disabled
          aria-label="Buscar en la biblioteca"
          placeholder="Buscar — disponible en la Entrega B"
          className="h-8 w-full rounded-full border border-line bg-bg pl-9 pr-3 text-[13px] placeholder:text-faint disabled:cursor-not-allowed"
        />
      </div>

      <div className="flex items-center gap-1">
        <button className={iconBtn} onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')} aria-label="Cambiar tema" title="Cambiar tema">
          {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
        </button>
        <button className={cn(iconBtn, rightPanelOpen && 'text-accent')} onClick={toggleRightPanel} aria-label="Panel derecho" aria-pressed={rightPanelOpen} title="Panel derecho">
          <PanelRight size={16} />
        </button>
        <DropdownMenu>
          <DropdownMenuTrigger className={iconBtn} aria-label="Configuración" title="Configuración">
            <Settings size={16} />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-60">
            <DropdownMenuLabel>Color de acento</DropdownMenuLabel>
            <div className="flex gap-2 px-2.5 py-1.5">
              {ACCENTS.map((a) => (
                <button
                  key={a.id}
                  aria-label={a.label}
                  title={a.label}
                  onClick={() => setAccent(a.id)}
                  className={cn('size-6 rounded-full ring-offset-2 ring-offset-surface-2', accent === a.id && 'ring-2 ring-fg')}
                  style={{ background: `oklch(0.68 0.19 ${a.hue})` }}
                />
              ))}
            </div>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={reset}>
              Restaurar apariencia <RotateCcw size={13} />
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}
