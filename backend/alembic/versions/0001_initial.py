"""esquema inicial v2

Revision ID: 0001
Revises:
"""
import sqlalchemy as sa
from alembic import op

revision = "0001"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "app_settings",
        sa.Column("key", sa.String, primary_key=True),
        sa.Column("value", sa.String, nullable=False),
        sa.Column("updated_utc", sa.DateTime(timezone=True)),
    )
    op.create_table(
        "music_folders",
        sa.Column("id", sa.String, primary_key=True),
        sa.Column("path", sa.String, nullable=False, unique=True),
        sa.Column("enabled", sa.Boolean, nullable=False, server_default=sa.true()),
        sa.Column("last_scan_utc", sa.DateTime(timezone=True)),
        sa.Column("created_utc", sa.DateTime(timezone=True)),
    )
    op.create_table(
        "tracks",
        sa.Column("id", sa.String, primary_key=True),
        sa.Column("folder_id", sa.String, sa.ForeignKey("music_folders.id"), nullable=False),
        sa.Column("path", sa.String, nullable=False, unique=True),
        sa.Column("title", sa.String, nullable=False),
        sa.Column("artist", sa.String),
        sa.Column("album_artist", sa.String),
        sa.Column("album", sa.String),
        sa.Column("genre", sa.String),
        sa.Column("year", sa.Integer),
        sa.Column("duration_ms", sa.Integer),
        sa.Column("size_bytes", sa.Integer),
        sa.Column("modified_utc", sa.DateTime(timezone=True)),
        sa.Column("added_utc", sa.DateTime(timezone=True)),
        sa.Column("enrichment_state", sa.String, nullable=False, server_default="provisional"),
        sa.Column("availability", sa.String, nullable=False, server_default="available"),
    )
    op.create_index("ix_tracks_folder_id", "tracks", ["folder_id"])


def downgrade() -> None:
    op.drop_index("ix_tracks_folder_id", "tracks")
    op.drop_table("tracks")
    op.drop_table("music_folders")
    op.drop_table("app_settings")
