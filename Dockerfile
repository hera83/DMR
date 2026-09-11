# --- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, layered separately so dependency restore is cached across
# builds that only change application code.
COPY DMR.csproj ./
RUN dotnet restore DMR.csproj

COPY . .
RUN dotnet publish DMR.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# app_dbs (SQLite identity/DMR databases) and app_files (FTP downloads/temp/raws) are meant to be
# bind-mounted from the host for persistence — see docker-compose.yml. entrypoint.sh (re)creates the
# full folder structure on every container start, so a fresh host checkout with no app_dbs/app_files
# yet still comes up correctly on first `docker compose up`.
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production
# .NET 8+ container images listen on 8080/8081 by default; keep it explicit.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["./entrypoint.sh"]
