#!/bin/sh
set -e

# app_dbs/ and app_files/ are bind-mounted from the host (see docker-compose.yml) so their data
# survives container rebuilds. A bind mount that doesn't exist on the host yet is created by Docker
# as an *empty* directory, which hides whatever the image itself created at build time — so create
# the full structure the app expects here, at container startup, every time, instead of relying on
# the one-off build-time mkdir. Cheap and idempotent (mkdir -p is a no-op once they exist).
mkdir -p app_dbs app_files/downloads app_files/temp app_files/raws

exec dotnet DMR.dll
