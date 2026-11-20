$ErrorActionPreference = 'Stop'

docker compose up -d ollama
docker compose exec ollama ollama pull qwen3:8b
docker compose exec ollama ollama pull nomic-embed-text

Write-Host 'Both local models are present in the persistent Ollama volume.'
