import * as DM from '@radix-ui/react-dropdown-menu'
import type { ComponentProps } from 'react'
import { cn } from '../../lib/utils'

export const DropdownMenu = DM.Root
export const DropdownMenuTrigger = DM.Trigger

export function DropdownMenuContent({ className, sideOffset = 6, ...p }: ComponentProps<typeof DM.Content>) {
  return (
    <DM.Portal>
      <DM.Content
        sideOffset={sideOffset}
        className={cn('z-50 min-w-48 rounded-lg border border-line bg-surface-2 p-1 text-fg shadow-2xl shadow-black/40', className)}
        {...p}
      />
    </DM.Portal>
  )
}

export function DropdownMenuItem({ className, ...p }: ComponentProps<typeof DM.Item>) {
  return (
    <DM.Item
      className={cn(
        'flex cursor-default select-none items-center justify-between gap-6 rounded-md px-2.5 py-1.5 outline-none',
        'data-[highlighted]:bg-accent-soft data-[disabled]:text-faint',
        className,
      )}
      {...p}
    />
  )
}

export const DropdownMenuLabel = ({ className, ...p }: ComponentProps<typeof DM.Label>) => (
  <DM.Label className={cn('px-2.5 py-1 text-[11px] uppercase tracking-wider text-faint', className)} {...p} />
)
export const DropdownMenuSeparator = ({ className, ...p }: ComponentProps<typeof DM.Separator>) => (
  <DM.Separator className={cn('my-1 h-px bg-line', className)} {...p} />
)
