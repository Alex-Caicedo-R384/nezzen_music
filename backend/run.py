"""Punto de entrada: arranca Uvicorn solo en loopback."""
import uvicorn

from app.core.config import get_settings

if __name__ == "__main__":
    s = get_settings()
    if s.host not in ("127.0.0.1", "localhost", "::1"):
        raise SystemExit(f"Host {s.host!r} rechazado: Nezzen solo escucha en loopback.")
    uvicorn.run("app.main:app_factory", factory=True, host=s.host, port=s.port, log_level="info")
