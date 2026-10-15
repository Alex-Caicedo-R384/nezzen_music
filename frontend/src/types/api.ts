export interface Health {
  status: string
  version: string
  database: string
  database_path: string
  schema_revision: string | null
  tracks: number
  folders: number
}
