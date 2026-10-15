import { useCallback } from 'react'

interface Props {
  width: number
  min: number
  max: number
  onChange: (width: number) => void
}

export function ResizeHandle({ width, min, max, onChange }: Props) {
  const onPointerDown = useCallback(
    (e: React.PointerEvent<HTMLDivElement>) => {
      e.currentTarget.setPointerCapture(e.pointerId)
      const startX = e.clientX
      const startW = width
      const move = (ev: PointerEvent) => onChange(startW + ev.clientX - startX)
      const up = () => {
        window.removeEventListener('pointermove', move)
        window.removeEventListener('pointerup', up)
      }
      window.addEventListener('pointermove', move)
      window.addEventListener('pointerup', up)
    },
    [width, onChange],
  )
  return (
    <div
      role="separator"
      aria-orientation="vertical"
      aria-label="Redimensionar barra lateral"
      aria-valuenow={width}
      aria-valuemin={min}
      aria-valuemax={max}
      tabIndex={0}
      onPointerDown={onPointerDown}
      onKeyDown={(e) => {
        if (e.key === 'ArrowLeft') onChange(width - 16)
        if (e.key === 'ArrowRight') onChange(width + 16)
      }}
      className="w-1 shrink-0 cursor-col-resize bg-line transition-colors hover:bg-accent focus-visible:bg-accent"
    />
  )
}
