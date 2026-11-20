param(
    [string]$ApiUrl = $env:DOMAINCOPILOT_API_URL,
    [string]$BearerToken = $env:DOMAINCOPILOT_BEARER_TOKEN
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ApiUrl)) {
    throw "Set DOMAINCOPILOT_API_URL, for example http://localhost:8080."
}
if ([string]::IsNullOrWhiteSpace($BearerToken)) {
    throw "Set DOMAINCOPILOT_BEARER_TOKEN to a test JWT with the role required by /api/documents."
}

$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $root "data/synthetic/corpus-manifest.json"
$corpusDirectory = Join-Path $root "data/synthetic/corpus"
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Corpus manifest not found. Run scripts/generate_synthetic_corpus.py first."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.documentCount -ne 30 -or $manifest.documents.Count -ne 30 -or $manifest.expectedPageCount -lt 150) {
    throw "Manifest must describe at least 30 documents and 150 pages."
}

$headers = @{ Authorization = "Bearer $BearerToken"; Accept = "application/json" }
$endpoint = "$($ApiUrl.TrimEnd('/'))/api/documents"
foreach ($document in $manifest.documents) {
    $path = Join-Path $corpusDirectory $document.fileName
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Corpus file missing: $($document.fileName). Regenerate the corpus first."
    }
    $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $document.sha256) {
        throw "Corpus checksum mismatch: $($document.fileName). Regenerate the corpus first."
    }

    $form = @{
        File = Get-Item -LiteralPath $path
        Version = $document.version
    }
    $result = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Form $form
    Write-Host ("{0}: indexed={1}, chunks={2}" -f $document.fileName, (-not $result.wasAlreadyIndexed), $result.chunkCount)
}

Write-Host "Synthetic corpus ingest finished. Re-running is supported by document fingerprint idempotency."
