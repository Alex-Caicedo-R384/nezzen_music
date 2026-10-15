import { useEffect, useState } from 'react'
import { EmptyState } from '../components/layout/EmptyState'
import { PlayerBar } from '../components/layout/PlayerBar'
import { ResizeHandle } from '../components/layout/ResizeHandle'
import { RightPanel } from '../components/layout/RightPanel'
import { Sidebar } from '../components/layout/Sidebar'
import { TopBar } from '../components/layout/TopBar'
import { VIEWS, type ViewId } from '../features/library/views'
import { ACCENTS, SIDEBAR_MAX, SIDEBAR_MIN, useSettingsStore } from '../features/settings/useSettingsStore'

export default function App() {
  const [view, setView] = useState<ViewId>('songs')
  const { theme, accent, sidebarWidth, setSidebarWidth, rightPanelOpen } = useSettingsStore()

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    const hue = ACCENTS.find((a) => a.id === accent)?.hue ?? 275
    document.documentElement.style.setProperty('--accent-h', String(hue))
  }, [theme, accent])

  const current = VIEWS.find((v) => v.id === view)!

  return (
    <div className="flex h-full flex-col">
      <TopBar />
      <div className="flex min-h-0 flex-1">
        <div style={{ width: sidebarWidth }} className="shrink-0">
          <Sidebar active={view} onSelect={setView} />
        </div>
        <ResizeHandle width={sidebarWidth} min={SIDEBAR_MIN} max={SIDEBAR_MAX} onChange={setSidebarWidth} />
        <main className="min-w-0 flex-1 overflow-y-auto" aria-label={current.label}>
          <EmptyState
            key={current.id}
            icon={current.icon}
            title={current.label}
            description={current.blurb}
            badge={`Disponible en la ${current.arrivesIn}`}
          />
        </main>
        {rightPanelOpen && (
          <div className="w-72 shrink-0 border-l border-line">
            <RightPanel />
          </div>
        )}
      </div>
      <PlayerBar />
    </div>
  )
}
