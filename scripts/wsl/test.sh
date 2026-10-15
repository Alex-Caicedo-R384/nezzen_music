#!/usr/bin/env bash
# Pruebas de backend (pytest) y frontend (tsc + vitest). Crea el venv local de WSL si falta.
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$root/backend"
[ -x .venv/bin/python ] || { python3 -m venv .venv; .venv/bin/pip install -q -r requirements-dev.txt; }
.venv/bin/python -m pytest -q
cd "$root/frontend"
[ -d node_modules ] || npm ci
npx tsc -b
npm run test
