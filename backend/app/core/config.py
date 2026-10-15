"""Configuración local. El backend solo escucha en loopback."""
import os
import sys
from functools import lru_cache
from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict

APP_VERSION = "0.1.0"


def default_data_dir() -> Path:
    """Directorio de datos de la v2, separado de ``NezzenMusic`` usado por la app .NET."""
    if sys.platform == "win32":
        base = Path(os.environ.get("LOCALAPPDATA") or Path.home() / "AppData" / "Local")
        return base / "NezzenMusicV2"
    base = Path(os.environ.get("XDG_DATA_HOME") or Path.home() / ".local" / "share")
    return base / "nezzen-music-v2"


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="NEZZEN_", env_file=None)

    host: str = "127.0.0.1"
    port: int = 8765
    data_dir: Path = Field(default_factory=default_data_dir)
    # Orígenes del frontend de desarrollo (Vite) permitidos por CORS.
    cors_origins: list[str] = [
        "http://127.0.0.1:5173",
        "http://localhost:5173",
    ]
    # Carpeta con el frontend compilado; si existe, FastAPI lo sirve.
    frontend_dist: Path = Path(__file__).resolve().parents[3] / "frontend" / "dist"

    @property
    def database_path(self) -> Path:
        return self.data_dir / "nezzen_v2.db"


@lru_cache
def get_settings() -> Settings:
    return Settings()
