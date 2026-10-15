import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type Theme = 'dark' | 'light'

export const ACCENTS = [
  { id: 'violet', label: 'Violeta', hue: 275 },
  { id: 'rose', label: 'Rosa', hue: 355 },
  { id: 'amber', label: 'Ámbar', hue: 70 },
  { id: 'emerald', label: 'Esmeralda', hue: 160 },
  { id: 'azure', label: 'Azul', hue: 240 },
] as const

export type AccentId = (typeof ACCENTS)[number]['id']

export const SIDEBAR_MIN = 180
export const SIDEBAR_MAX = 360
export const SIDEBAR_DEFAULT = 232

interface SettingsState {
  theme: Theme
  accent: AccentId
  sidebarWidth: number
  rightPanelOpen: boolean
  setTheme: (theme: Theme) => void
  setAccent: (accent: AccentId) => void
  setSidebarWidth: (width: number) => void
  toggleRightPanel: () => void
  reset: () => void
}

const defaults = {
  theme: 'dark' as Theme,
  accent: 'violet' as AccentId,
  sidebarWidth: SIDEBAR_DEFAULT,
  rightPanelOpen: true,
}

// Preferencias visuales: solo UI, persistidas en localStorage (la persistencia SQLite llega en Entrega B).
export const useSettingsStore = create<SettingsState>()(
  persist(
    (set) => ({
      ...defaults,
      setTheme: (theme) => set({ theme }),
      setAccent: (accent) => set({ accent }),
      setSidebarWidth: (w) => set({ sidebarWidth: Math.min(SIDEBAR_MAX, Math.max(SIDEBAR_MIN, Math.round(w))) }),
      toggleRightPanel: () => set((s) => ({ rightPanelOpen: !s.rightPanelOpen })),
      reset: () => set(defaults),
    }),
    { name: 'nezzen.settings.v1' },
  ),
)
