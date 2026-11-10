# Security — Week 4 Part D

## JWT and roles

ApiSecurity configures AddAuthentication().AddJwtBearer(). ValidateIssuer, ValidateAudience, ValidateLifetime and ValidateIssuerSigningKey are enabled, with signed/expiring HS256 tokens required and a 30-second clock skew. Issuer/audience/key come from configuration; inbound claims are not remapped. RoleClaimType is role and each courier has courierId. Setup generates random keys outside Git in API User Secrets; those secrets are a development convenience, not an encrypted production vault. Use a managed secret store in production.

All legacy, v1 and v2 shipment routes require authentication, including authenticated GET. Courier alone may upload scans and PATCH status; Admin alone may DELETE. Existing create/update now require authentication and preserve domain validation. CourierOwnershipFilter verifies every scan's courierId exactly matches the JWT before calling application services. Missing/invalid/expired tokens return 401; valid tokens with wrong role/owner return 403, both ProblemDetails. Existing transactional de-duplication and transitions remain authoritative.

Development-only /dev/token issues 15-minute fictional Courier/Admin tokens, requires a random X-Development-Key and a loopback caller, and is excluded from OpenAPI. It does not exist in Production and issues no refresh token. Clipboard scripts obtain tokens without console logging; do not expose this issuer/bootstrap key on a shared/public server. Rotate signing keys by configuration/restart; existing tokens then fail validation.

## Partner API keys

Authentication middleware invokes PartnerKeyHandler only for the read-only GET /api/v2/partner/shipments/{trackingNumber}. One X-Api-Key must match a configured Partners:ApiKeys entry; comparison uses fixed-time equality of SHA-256 hashes. Empty/multiple/invalid keys return 401 ProblemDetails. Keys never authenticate ordinary shipment routes or grant writes. Configure several entries temporarily during rotation, update the partner then remove the old key. Real production integrations need distinct identities, revocation, scopes and per-partner limits. OpenAPI describes Bearer and PartnerKey per operation; Swagger supports Authorize.

## OAuth2 and migration to a provider

For a public MAUI courier app, use Authorization Code + PKCE in the system browser: generate a random verifier and S256 challenge, send the challenge to the identity provider, verify state/nonce and registered redirect, receive a short-lived authorization code, exchange it with the verifier. PKCE prevents an intercepted code being redeemed by another app. The mobile app cannot protect a confidential client secret. Use an established OIDC/MSAL library rather than implementing password/browser/token flows yourself.

The identity provider authenticates users and grants roles/scopes. Its short-lived access JWT authorizes API calls; ApexPost validates signature using provider discovery/JWKS keys, issuer, audience and expiry, then checks scopes/roles/courier ownership. A longer-lived refresh token stays in the protected client token cache and is exchanged only with the identity provider, never sent to the ApexPost API. Rotate/revoke refresh tokens; unrecoverable 401 clears credentials and requests login.

Partners should use Client Credentials: a confidential backend authenticates to the provider with a certificate or securely managed secret and receives an app-only token with restricted scopes. It obtains a new token on expiry; normally there is no user/refresh token. API keys are weaker long-lived shared possession strings without standard issuer/audience/expiry/scopes or delegated identity, requiring custom rotation/revocation.

For a real provider, remove the development issuer/bootstrap endpoint; configure Authority/Audience, discovery/key rotation, trusted roles/courierId and scope policies; register PKCE redirect URIs; replace manual pasting with provider token acquisition; provision partner credentials separately. Never embed a confidential client secret/API key in the MAUI binary. Keep the server's ownership and domain checks.

## HTTPS, Windows and Android

Program uses HTTPS redirection everywhere and HSTS outside Development. Clients must start with HTTPS: redirecting cannot undo exposure of credentials already sent over plain HTTP. Bearer tokens/API keys are possession credentials; TLS protects them in transit. MAUI rejects non-HTTPS base addresses and disables automatic redirects. There is **no certificate-validation bypass** in Debug or Release. If adding a reverse proxy later, configure forwarded headers only for trusted proxies and secure traffic to the API.

Windows uses the trusted ASP.NET development certificate (`dotnet dev-certs https --trust`) and https://localhost:7268/. Embedded appsettings.json has public Windows/Android URLs only, no credentials.

Android is optional: install maui-android if needed and build `dotnet build ApexPost.Courier -f net10.0-android -p:EnableAndroid=true`. The default host-loopback address https://10.0.2.2:7268/ normally mismatches a localhost-only certificate. Provision a trusted development CA/server certificate with SAN IP 10.0.2.2, bind it to Kestrel through local secrets and install its CA in the emulator. Alternatively use `adb reverse tcp:7268 tcp:7268`, change AndroidEmulator to https://localhost:7268/ and install the localhost development CA/certificate in the emulator. Debug network security permits user-installed CAs but still checks hostname, expiry and signature. Release uses system trust and needs a real trusted endpoint. Never commit certificate private keys or disable Release validation. Do not install development CAs on production devices. Android activation/networking must be verified by an actual native run.

## SecureStorage and logs

SecureTokenStore uses only MAUI SecureStorage, never Preferences/plain files. HTTP 401 clears it, signs out and navigates to Login while retaining local scans filtered by courierId. Client-side JWT decoding is for display/filtering only; the API validates authenticity on each call. Logs contain retry numbers/status/delay, circuit transitions, document IDs and counts; all HttpClient headers are redacted and no bodies/tokens/keys are logged. ProblemDetails are bounded user messages. Avoid HTTP body logging, IdentityModel PII, PowerShell transcripts and clipboard recording during token issuance; mask tokens in evidence.
