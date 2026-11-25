# HealthCareCopilot Web

Standalone Angular 22 frontend for the Domain Copilot API, branded as HealthCareCopilot. The app separates API contracts into `src/app/models`, HTTP/authentication code into `src/app/services`, and each screen into its own standalone component under `src/app/features`.

## Run locally

1. Start SQL LocalDB and the API from the repository root:

   ```powershell
   dotnet run --project src/DomainCopilot.Api --launch-profile http
   ```

2. In a second terminal, start Angular from this directory:

   ```powershell
   npm install
   npm start
   ```

3. Open `http://localhost:4200`. The dev server opens the browser and proxies `/api/*` to `http://localhost:5161`.

## Included flows

- Register and log in; access token is kept in tab-scoped `sessionStorage` and added by a functional HTTP interceptor.
- Search indexed documents and inspect retrieved passages with their source labels.
- Upload PDF/DOCX files for extraction, chunking, embeddings, and indexing.
- Create a case and watch live progress through retrieval, agents, and draft saving.
- Both `Researcher` and `Clinician` accounts can search, create cases and request drafts. `Researcher` can upload guidance; only `Clinician` can review and approve/reject drafts (server-enforced).
- The workspace tab lists all cases and draft decisions for clinicians, and only the signed-in researcher's own cases and runs for researchers. The document library and extracted document reader are clinician-only; researchers see the upload form but no system file list.
- Reviewing a draft immediately shows the saved decision and removes the review actions. The document reader displays extracted indexed text, since uploaded PDF/DOCX binaries are not retained.
- The researcher and drafter analyze retrieved evidence concurrently; the safety checker still reviews the draft before it is saved.

The UI is Arabic and right-to-left, with responsive layouts for desktop and mobile. Use synthetic/public documents only. For a production deployment, configure HTTPS, a secure identity provider, and an appropriate token storage strategy.
