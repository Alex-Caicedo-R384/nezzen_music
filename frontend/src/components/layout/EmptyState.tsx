import type { LucideIcon } from 'lucide-react'
import { motion } from 'motion/react'

interface Props {
  icon: LucideIcon
  title: string
  description: string
  badge?: string
}

export function EmptyState({ icon: Icon, title, description, badge }: Props) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25 }}
      className="mx-auto flex h-full max-w-md flex-col items-center justify-center gap-3 px-6 text-center"
    >
      <div className="grid size-16 place-items-center rounded-2xl border border-line bg-surface text-accent">
        <Icon size={28} strokeWidth={1.5} />
      </div>
      <h2 className="text-lg font-semibold">{title}</h2>
      <p className="text-muted">{description}</p>
      {badge && <span className="rounded-full bg-accent-soft px-2.5 py-0.5 text-[11px] font-medium text-accent">{badge}</span>}
    </motion.div>
  )
}
