import pytest
from fastapi.testclient import TestClient

from app.core.config import Settings
from app.main import create_app


@pytest.fixture
def settings(tmp_path):
    return Settings(data_dir=tmp_path / "data", frontend_dist=tmp_path / "no-dist")


@pytest.fixture
def client(settings):
    with TestClient(create_app(settings)) as c:
        yield c
