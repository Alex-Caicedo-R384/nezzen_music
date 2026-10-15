from pydantic import BaseModel


class HealthOut(BaseModel):
    status: str
    version: str
    database: str
    database_path: str
    schema_revision: str | None
    tracks: int
    folders: int
