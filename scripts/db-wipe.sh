#!/usr/bin/env bash
# Drops the Development database and reapplies all EF Core migrations.
# Requires: PostgreSQL reachable (e.g. `make db-up`), dotnet-ef CLI.
# Uses appsettings.Development.json via ASPNETCORE_ENVIRONMENT=Development.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development

INFRA="Esatto.Outreach.Infrastructure/Esatto.Outreach.Infrastructure.csproj"
API="Esatto.Outreach.Api/Esatto.Outreach.Api.csproj"

echo "Wiping Development database (drop + migrate). All data will be removed."
dotnet ef database drop --force \
  --project "$INFRA" \
  --startup-project "$API"

# Recreate an empty database before migrate so EF does not log a failed first
# connection to a database that does not exist yet (harmless but confusing).
create_outreach_dev_db() {
  local CONTAINER="${POSTGRES_CONTAINER:-outreach-postgres}"
  if command -v docker >/dev/null 2>&1 && docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "$CONTAINER"; then
    docker exec "$CONTAINER" psql -U postgres -d postgres -v ON_ERROR_STOP=1 \
      -c "CREATE DATABASE outreach_dev;" 2>/dev/null && return
  fi
  if command -v psql >/dev/null 2>&1; then
    export PGPASSWORD="${PGPASSWORD:-localdevpassword}"
    psql -h localhost -p 5432 -U postgres -d postgres -v ON_ERROR_STOP=1 \
      -c "CREATE DATABASE outreach_dev;" 2>/dev/null && return
  fi
}

echo "Creating empty database outreach_dev..."
create_outreach_dev_db || true

echo "Applying migrations..."
dotnet ef database update \
  --project "$INFRA" \
  --startup-project "$API"

echo "Database is empty and schema is up to date."
