from fastapi import APIRouter, Request
from sqlalchemy import func, select, text

from app.core.config import APP_VERSION
from app.models import MusicFolder, Track
from app.schemas.health import HealthOut

router = APIRouter(tags=["health"])


@router.get("/health", response_model=HealthOut)
def health(request: Request) -> HealthOut:
    state = request.app.state
    with state.session_factory() as session:
        session.execute(text("SELECT 1"))
        revision = session.execute(text("SELECT version_num FROM alembic_version")).scalar()
        tracks = session.scalar(select(func.count()).select_from(Track)) or 0
        folders = session.scalar(select(func.count()).select_from(MusicFolder)) or 0
    return HealthOut(
        status="ok",
        version=APP_VERSION,
        database="ok",
        database_path=str(state.settings.database_path),
        schema_revision=revision,
        tracks=tracks,
        folders=folders,
    )
