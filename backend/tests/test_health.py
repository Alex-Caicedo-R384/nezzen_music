from fastapi.testclient import TestClient

from app.core.config import Settings
from app.main import create_app


def test_health_ok_and_new_database(client, settings):
    r = client.get("/api/health")
    assert r.status_code == 200
    body = r.json()
    assert body["status"] == "ok"
    assert body["schema_revision"] == "0001"
    assert body["tracks"] == 0 and body["folders"] == 0
    assert settings.database_path.name == "nezzen_v2.db"
    assert settings.database_path.exists()


def test_default_data_dir_is_not_legacy_dotnet_dir():
    s = Settings()
    assert s.host == "127.0.0.1"
    assert "NezzenMusic" not in s.data_dir.name or s.data_dir.name.endswith("V2")
    assert s.database_path.name != "library.db"


def test_cors_allows_only_local_dev_origin(client):
    ok = client.options("/api/health", headers={"Origin": "http://127.0.0.1:5173", "Access-Control-Request-Method": "GET"})
    assert ok.headers.get("access-control-allow-origin") == "http://127.0.0.1:5173"
    bad = client.options("/api/health", headers={"Origin": "https://evil.example", "Access-Control-Request-Method": "GET"})
    assert "access-control-allow-origin" not in bad.headers


def test_serves_built_frontend_without_shadowing_api(tmp_path):
    dist = tmp_path / "dist"
    (dist / "assets").mkdir(parents=True)
    (dist / "index.html").write_text("<html>nezzen</html>")
    s = Settings(data_dir=tmp_path / "d", frontend_dist=dist)
    with TestClient(create_app(s)) as c:
        assert "nezzen" in c.get("/").text
        assert "nezzen" in c.get("/albums").text
        assert c.get("/api/health").json()["status"] == "ok"
        assert c.get("/api/nope").status_code == 404
        assert "nezzen" in c.get("/../../etc/passwd").text  # nunca sirve fuera de dist


def test_run_rejects_non_loopback(monkeypatch):
    import runpy

    monkeypatch.setenv("NEZZEN_HOST", "0.0.0.0")
    from app.core.config import get_settings

    get_settings.cache_clear()
    try:
        import pytest

        with pytest.raises(SystemExit):
            runpy.run_path("run.py", run_name="__main__")
    finally:
        get_settings.cache_clear()
