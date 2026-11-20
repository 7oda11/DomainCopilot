# Document ingestion (first vertical slice)

`POST /api/documents` accepts a PDF or DOCX as multipart form data (`File`, `Version`). The request is capped at 25 MiB. The Application handler fingerprints the bytes with SHA-256, checks the `(fingerprint, version)` key for idempotency, extracts pages, normalizes whitespace, chunks within each page with configurable overlap, persists source metadata and chunks in SQL Server, then generates embeddings and writes them to the local SQLite index. A document is marked indexed only after both stores succeed; a repeat upload resumes indexing if the vector service failed midway.

## Boundaries

- `IDocumentTextExtractor` is the Application port; PdfPig and the Open XML ZIP/XML reader live in Infrastructure.
- `DocumentChunker` is deterministic Application logic, so page attribution and overlap can be tested without a PDF library or database.
- `IDocumentIngestionStore` hides SQL and transaction details from the use case.
- The API translates upload fields into an Application request and returns a small response DTO.

## Current limits

- Only text-based PDF and DOCX are supported. Scanned PDFs need OCR and are rejected if they contain no extractable text.
- DOCX page numbers use explicit/rendered page-break markers. Word's visual pagination can differ by Word version, fonts and printer settings; source citations should treat those locations as approximate until rendered-page extraction is added.
- The endpoint persists text chunks in SQL Server, then generates embeddings and writes vectors to the SQLite index. Hybrid retrieval and traceable citation responses are available through `POST /api/search`.
- Processing is synchronous in this first slice. The configured caps are 25 MiB upload, 500 extracted pages, 5 million extracted characters, and 20 MiB uncompressed DOCX body.

## Try locally

Start SQL Server and set `ConnectionStrings__DefaultConnection` in the environment. Apply EF migrations, run the API, then send a PDF or DOCX via multipart form data to `POST /api/documents`. No real patient data should be uploaded; use synthetic or public material only.
