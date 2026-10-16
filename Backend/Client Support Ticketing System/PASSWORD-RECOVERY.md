# Email password recovery

Implemented: Forgot password link, email request page, reset form, 15-minute encrypted tokens, single-use atomic password replacement, session invalidation, bounded background email queue and request limits. No database schema migration is required.

## Local setup

Configure-RecoveryMail.ps1 is placed next to the backend solution. Run it in PowerShell and enter your project Gmail address, frontend URL and Gmail app password. It uses smtp.gmail.com on port 587 with STARTTLS and stores credentials through .NET user-secrets, outside the repository. Use an app password, never your ordinary mailbox password. Restart the backend in Development mode. SMTP delivery requires outbound network access and a supported account configuration. OAuth-only mail providers require a different sender implementation.

Without valid settings, the endpoint returns 503 and the UI clearly says recovery is unavailable. It does not pretend to send mail. A queued request returns a generic message regardless of whether an active account exists. SMTP failures produce a sanitized server warning; they cannot be reported per recipient without exposing account existence.

Test with an account whose stored email you control. Open the received link, choose a new password, confirm old login fails and new login works, then verify link reuse fails. Do not change an account's email to someone else's address just to test.

## Deployment limits

FrontendUrl must be HTTPS except for local development; it is configured by the operator, never taken from the request Host header. Links keep tokens in the URL fragment; the page removes the fragment after reading it. Reloading that page requires reopening the email link. The queue and request limits are in-memory for this single-server training project: restart clears pending email and counters. Multiple servers need shared rate limits and a durable queue. Persist and protect ASP.NET Data Protection keys across deployments; never commit keys. Existing sessions issued before this update require a fresh login.

No secrets, reset links, passwords or email bodies are written to application logs. Developer user-secrets are local configuration, not an encrypted production secret store. Production should use protected deployment secrets for RecoveryMail settings. The existing .NET 6 end-of-support issue remains.

No real email delivery is confirmed until SMTP setup and a mailbox test succeed. Automated checks use fake users and do not modify the project's database. The conditional password update is specific to the project's SQL Server Users table.

Reference: https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html
