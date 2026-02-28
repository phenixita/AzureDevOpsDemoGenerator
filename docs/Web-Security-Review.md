# Web Security Review (OWASP-focused)

Date: 2026-02-28

Scope reviewed:
- `/src/VstsDemoBuilder/Controllers/GitHubController.cs`
- `/src/VstsRestAPI/Git/GitHubImportRepo.cs`

Findings and actions:
1. **OAuth state validation weakness (CSRF risk)**  
   - Issue: GitHub OAuth flow generated a static/shared `state` value and did not validate callback `state`.
   - OWASP mapping: A01 Broken Access Control / OAuth CSRF class of issues.
   - Action taken: `state` is now generated per request, stored in session, and validated in callback before token exchange.

2. **Legacy/insecure TLS protocol configuration**  
   - Issue: GitHub REST calls explicitly enabled `Ssl3`, `Tls`, and `Tls11`.
   - OWASP mapping: A02 Cryptographic Failures.
   - Action taken: protocol setting has been restricted to `Tls12`.

Notes:
- Full broader hardening (security headers, dependency updates, and additional authz checks) should be tracked separately to keep this change set minimal.
