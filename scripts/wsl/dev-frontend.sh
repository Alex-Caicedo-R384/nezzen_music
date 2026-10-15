#!/usr/bin/env bash
# Vite en 127.0.0.1:5173 con proxy /api -> backend (Windows, puerto 8765 por defecto).
set -euo pipefail
cd "$(dirname "$0")/../../frontend"
[ -d node_modules ] || npm ci
exec npm run dev
