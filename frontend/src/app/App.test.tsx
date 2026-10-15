import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSettingsStore } from '../features/settings/useSettingsStore'
import App from './App'

const health = { status: 'ok', version: '0.1.0', database: 'ok', database_path: 'x', schema_revision: '0001', tracks: 0, folders: 0 }

function renderApp() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={qc}>
      <App />
    </QueryClientProvider>,
  )
}

describe('shell', () => {
  beforeEach(() => {
    useSettingsStore.getState().reset()
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(health), { status: 200 })))
  })

  it('muestra las vistas de la barra lateral y cambia de vista', async () => {
    renderApp()
    expect(screen.getByRole('button', { name: /Todas las canciones/ })).toHaveAttribute('aria-current', 'page')
    await userEvent.click(screen.getByRole('button', { name: /Álbumes/ }))
    expect(screen.getByRole('heading', { name: 'Álbumes' })).toBeInTheDocument()
  })

  it('refleja backend conectado desde /api/health', async () => {
    renderApp()
    expect(await screen.findByText('Backend conectado')).toBeInTheDocument()
  })

  it('informa backend no disponible', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('', { status: 500 })))
    renderApp()
    expect(await screen.findByText('Backend no disponible')).toBeInTheDocument()
  })

  it('cambia el tema y lo aplica al documento', async () => {
    renderApp()
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar tema' }))
    expect(document.documentElement.dataset.theme).toBe('light')
  })

  it('los controles del reproductor están deshabilitados (no simulan reproducción)', () => {
    renderApp()
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Reproducir' })).toBeDisabled()
  })

  it('limita el ancho de la barra lateral', () => {
    useSettingsStore.getState().setSidebarWidth(9999)
    expect(useSettingsStore.getState().sidebarWidth).toBe(360)
  })
})
