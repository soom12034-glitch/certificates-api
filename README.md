# Certificates.Api (SQLite + Minimal API)

## Endpoints
- `POST /v1/certificates` (multipart/form-data)
  - Headers: `X-Api-Key: <your-key>`
  - Fields: `file` (PDF), `fingerprint` (optional), `meta` (optional JSON)
  - Returns: `{ id, verifyUrl }`
- `GET /v1/verify/{id}`: Public verification page
- Static PDFs: `/files/{id}.pdf`

## Environment
- `API_KEY` (required)
- `PUBLIC_BASE_URL` (recommended, e.g. `https://api.cashierpro-cloud.com`)
- `STORAGE_DIR` (default inside app, on server set `/data/certificates`)
- `DB_PATH` (default inside app, on server set `/data/app.db`)

## Docker
Expose `8080`, mount `/data` as a persistent volume. Example env:
- `ASPNETCORE_URLS=http://0.0.0.0:8080`
- `API_KEY=<secure-random>`
- `PUBLIC_BASE_URL=https://api.cashierpro-cloud.com`
- `STORAGE_DIR=/data/certificates`
- `DB_PATH=/data/app.db`

