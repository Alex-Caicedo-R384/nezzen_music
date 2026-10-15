import { Clock, Disc3, FolderTree, Heart, ListMusic, Mic2, Music2, Tags, type LucideIcon } from 'lucide-react'

export type ViewId = 'songs' | 'albums' | 'artists' | 'genres' | 'folders' | 'favorites' | 'playlists' | 'recent'

export interface ViewDef {
  id: ViewId
  label: string
  icon: LucideIcon
  blurb: string
  arrivesIn: string
}

export const VIEWS: ViewDef[] = [
  { id: 'songs', label: 'Todas las canciones', icon: Music2, blurb: 'Tu biblioteca completa en una tabla densa, ordenable y filtrable.', arrivesIn: 'Entrega B' },
  { id: 'albums', label: 'Álbumes', icon: Disc3, blurb: 'Cuadrícula de portadas agrupadas por álbum.', arrivesIn: 'Entrega C' },
  { id: 'artists', label: 'Artistas', icon: Mic2, blurb: 'Explora tu música por artista.', arrivesIn: 'Entrega C' },
  { id: 'genres', label: 'Géneros', icon: Tags, blurb: 'Agrupa por género a partir de las etiquetas.', arrivesIn: 'Entrega C' },
  { id: 'folders', label: 'Carpetas', icon: FolderTree, blurb: 'Carpetas registradas y su estado de escaneo.', arrivesIn: 'Entrega B' },
  { id: 'favorites', label: 'Favoritos', icon: Heart, blurb: 'Canciones que marcaste como favoritas.', arrivesIn: 'Entrega D' },
  { id: 'playlists', label: 'Listas de reproducción', icon: ListMusic, blurb: 'Crea y ordena tus listas.', arrivesIn: 'Entrega D' },
  { id: 'recent', label: 'Reproducciones recientes', icon: Clock, blurb: 'Lo último que escuchaste.', arrivesIn: 'Entrega D' },
]
