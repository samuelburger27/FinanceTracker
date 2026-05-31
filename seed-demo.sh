#!/usr/bin/env bash
# seed-demo.sh — populate the FinanceTracker SQLite database with demo data.
#
#   ./seed-demo.sh                       # seed default user (demo / demo123)
#   ./seed-demo.sh --reset               # wipe existing demo data, then seed
#   ./seed-demo.sh --user alice --password wonderland
#   ./seed-demo.sh --db /path/to/custom.db
#
# Any flag passed through is forwarded to FinanceTracker.DevSeed. See
# `dotnet run --project FinanceTracker.DevSeed -- --help` for the full list.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

exec dotnet run --project FinanceTracker.DevSeed -- "$@"
