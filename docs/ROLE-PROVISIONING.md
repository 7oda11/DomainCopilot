# Local demo role provisioning

Public registration intentionally grants only `Researcher`. Never let a caller choose a privileged role in the registration payload. To create a local demo Clinician:

1. Register a second account through `POST /api/auth/register`.
2. Using an authorized SQL Server administrator connection to the demo database, promote that account:

```sql
UPDATE dbo.UserAccounts
SET Role = N'Clinician'
WHERE NormalizedEmail = UPPER(N'clinician@example.test');

SELECT Email, Role
FROM dbo.UserAccounts
WHERE NormalizedEmail = UPPER(N'clinician@example.test');
```

3. Log out and log in again so the new JWT contains the Clinician role.
4. Confirm a Researcher token receives 403 from `POST /api/drafts/{draftId}/review` and the Clinician token succeeds.

This is a local assessment procedure, not a production provisioning design. Production needs a trusted administrator workflow, audit event, identity lifecycle and least-privilege controls. Never commit passwords or tokens.
