"""Esquema de la v2. Entrega A solo define estructura; la lógica llega en B/C."""
import uuid
from datetime import UTC, datetime

from sqlalchemy import Boolean, DateTime, ForeignKey, Integer, String
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base import Base


def _uuid() -> str:
    return str(uuid.uuid4())


def _now() -> datetime:
    return datetime.now(UTC)


class AppSetting(Base):
    __tablename__ = "app_settings"
    key: Mapped[str] = mapped_column(String, primary_key=True)
    value: Mapped[str] = mapped_column(String, nullable=False)
    updated_utc: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_now, onupdate=_now)


class MusicFolder(Base):
    __tablename__ = "music_folders"
    id: Mapped[str] = mapped_column(String, primary_key=True, default=_uuid)
    path: Mapped[str] = mapped_column(String, nullable=False, unique=True)
    enabled: Mapped[bool] = mapped_column(Boolean, nullable=False, default=True)
    last_scan_utc: Mapped[datetime | None] = mapped_column(DateTime(timezone=True))
    created_utc: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_now)


class Track(Base):
    __tablename__ = "tracks"
    id: Mapped[str] = mapped_column(String, primary_key=True, default=_uuid)
    folder_id: Mapped[str] = mapped_column(ForeignKey("music_folders.id"), nullable=False, index=True)
    path: Mapped[str] = mapped_column(String, nullable=False, unique=True)
    title: Mapped[str] = mapped_column(String, nullable=False)
    artist: Mapped[str | None] = mapped_column(String)
    album_artist: Mapped[str | None] = mapped_column(String)
    album: Mapped[str | None] = mapped_column(String)
    genre: Mapped[str | None] = mapped_column(String)
    year: Mapped[int | None] = mapped_column(Integer)
    duration_ms: Mapped[int | None] = mapped_column(Integer)
    size_bytes: Mapped[int | None] = mapped_column(Integer)
    modified_utc: Mapped[datetime | None] = mapped_column(DateTime(timezone=True))
    added_utc: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_now)
    # provisional = título derivado del nombre de archivo; enriched = leído de etiquetas
    enrichment_state: Mapped[str] = mapped_column(String, nullable=False, default="provisional")
    # available | missing | offline (nunca se borra por indisponibilidad)
    availability: Mapped[str] = mapped_column(String, nullable=False, default="available")
