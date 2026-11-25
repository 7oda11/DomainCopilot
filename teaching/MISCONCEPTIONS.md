# Five misconceptions to correct

1. **“If retrieval found a source, the answer is safe.”** Retrieval relevance does not prove each generated claim is supported.
2. **“A citation makes a claim true.”** A citation can be mismatched or irrelevant; the cited passage must entail the claim.
3. **“A prompt saying ignore instructions can control the model.”** User and document text are untrusted; server authorization and tool boundaries remain authoritative.
4. **“Ollama fallback means the app is offline-ready.”** Models, embeddings, database, migrations, ingestion and the whole workflow must work with the network disabled.
5. **“The AI can approve if the safety score is high.”** A model check is not a human decision. Only the authorized Clinician endpoint can approve or reject.
