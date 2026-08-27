# Story 003: Capture a Local Email

**As a** developer, **I want** application email captured locally **so that** recipients and rendering can be checked without sending internet email.

## Acceptance Criteria

- Development Compose starts Mailpit on SMTP port `1025` and UI port `8025`.
- A development-only test action sends HTML and plain-text email through `IEmailSender`.
- Message appears at `http://localhost:8025`.
- Production credentials are neither required nor used.

## Dependencies

- Story 002.

## Small Safe Steps

### Phase 1: Establish the Email Boundary

#### Document Development Email Constraints - 1 hour

**Type:** Learning

**Outcome:** A verifiable note records SMTP host, ports, multipart requirements, and the production isolation rule.

**Verify:** Compare the note with current Compose and configuration; every setting has one named source.

**Rollback:** Remove the note; application behavior is unchanged.

#### Add Mailpit to Development Compose - 2 hours

**Type:** Earning

**Outcome:** Development Compose exposes Mailpit SMTP on `1025` and UI on `8025` without changing production services.

**Verify:** Start the development profile and load `http://localhost:8025`; confirm production Compose output excludes Mailpit.

**Rollback:** Stop and remove the Mailpit service and its development-only configuration.

### Phase 2: Send Through the Application

#### Wire Development SMTP Behind IEmailSender - 2 hours

**Type:** Earning

**Outcome:** The development implementation sends through Mailpit while production configuration remains credential-driven and untouched.

**Verify:** Exercise `IEmailSender` against Mailpit and inspect one captured message; run configuration tests for both environments.

**Rollback:** Restore the previous dependency registration and remove development SMTP settings.

#### Add Development-Only Multipart Test Action - 3 hours

**Type:** Earning

**Outcome:** An environment-guarded action sends one HTML and plain-text test message through `IEmailSender`.

**Verify:** Invoke it in development, inspect both MIME parts in Mailpit, and confirm the route is absent outside development.

**Rollback:** Remove the action and route; SMTP capture remains available for later stories.

## Stop Conditions

- Stop if any production configuration references Mailpit or permits the test action.
- Stop if validation requires real internet delivery or production credentials.

## Completion Checklist

- [ ] Development Compose exposes both Mailpit ports.
- [ ] Test email contains HTML and plain-text parts.
- [ ] Production neither starts Mailpit nor exposes the test action.
