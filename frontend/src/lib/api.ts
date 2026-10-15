import type { Health } from '../types/api'

export async function fetchHealth(signal?: AbortSignal): Promise<Health> {
  const res = await fetch('/api/health', { signal })
  if (!res.ok) throw new Error(`El backend respondió ${res.status}`)
  return res.json() as Promise<Health>
}
