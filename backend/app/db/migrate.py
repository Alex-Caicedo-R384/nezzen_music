"""Aplica migraciones Alembic programáticamente al arrancar."""
from pathlib import Path

from alembic import command
from alembic.config import Config

BACKEND_ROOT = Path(__file__).resolve().parents[2]


def upgrade_to_head(db_path: Path) -> None:
    cfg = Config(str(BACKEND_ROOT / "alembic.ini"))
    cfg.set_main_option("script_location", str(BACKEND_ROOT / "alembic"))
    cfg.set_main_option("sqlalchemy.url", f"sqlite:///{db_path.as_posix()}")
    db_path.parent.mkdir(parents=True, exist_ok=True)
    command.upgrade(cfg, "head")
