# Story 006: Invitee Activates an Account

**As an** invitee, **I want** to accept my invitation **so that** my account becomes active.

## Acceptance Criteria

- [x] Valid unused token activates matching account.
- [x] Used, expired, or invalid token is rejected.
- [x] Token cannot be reused.
- [x] Acceptance signs user in with secure cookie.

## Dependencies

- Story 005.

## Small Safe Steps

### Phase 1: Establish Consumption Rules

#### Define Token and Session Scenarios - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios define valid, invalid, expired, used, concurrent-use, and secure-cookie behavior.

**Verify:** Scenarios cover every acceptance criterion and identify one atomic token-consumption boundary.

**Rollback:** Remove the scenarios; existing invitation behavior remains unchanged.

#### Add Atomic Invitation Consumption - 3 hours

**Type:** Earning

**Outcome:** One transaction marks a valid token used and activates its matching account exactly once.

**Verify:** Repository tests prove valid activation and that two concurrent attempts yield only one success.

**Rollback:** Revert the consumption service before exposing its endpoint; additive invitation data remains compatible.

### Phase 2: Activate and Sign In

#### Expose Invitation Acceptance - 2 hours

**Type:** Earning

**Outcome:** The acceptance endpoint activates valid invitations and returns a uniform rejection for invalid, expired, or used tokens.

**Verify:** Exercise all token states through the HTTP boundary and confirm no rejection leaks account details.

**Rollback:** Remove or disable the route; unconsumed invitations remain valid until expiry.

#### Create Secure Session After Commit - 2 hours

**Type:** Earning

**Outcome:** Successful activation creates a secure, HTTP-only, same-site session cookie only after token consumption commits.

**Verify:** Inspect cookie attributes, access an authenticated endpoint, and confirm failed/replayed tokens create no session.

**Rollback:** Revert session issuance while preserving successful account activation and requiring later sign-in.

## Stop Conditions

- Stop if token use and activation cannot commit atomically.
- Stop if concurrent acceptance can create two successful sessions.
- Stop if rejected tokens reveal whether an invitation or account exists.

## Completion Checklist

- [x] Valid token activates matching account once.
- [x] Invalid, expired, and used tokens fail uniformly.
- [x] Replay creates no session.
- [x] Successful acceptance sets a secure cookie.

## Result

Completed on `2026-08-27`. `AcceptInvitationCommand` atomically marks invitation as accepted and creates active user. Token hash lookup ensures replay is rejected. `POST /api/invitations/accept` endpoint creates a secure `HttpOnly`, `SameSite=Lax`, persistent cookie session via ASP.NET Core cookie authentication.
