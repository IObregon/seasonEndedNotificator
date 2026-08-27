# Story 004: Bootstrap the First Administrator

**As an** operator, **I want** to seed one administrator **so that** private-group setup can begin without public registration.

## Acceptance Criteria

- Deployment command creates one active admin from configured email.
- Repeating command is idempotent.
- Seeded admin can request authentication.
- No general registration endpoint exists.

## Dependencies

- Story 002.

## Small Safe Steps

### Phase 1: Define Safe Bootstrap Semantics

#### Specify Bootstrap States and Invariants - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios define first creation, repeated execution, conflicting email state, missing configuration, and absence of public registration.

**Verify:** Review scenarios against every acceptance criterion and run them as initially failing tests where practical.

**Rollback:** Remove the scenarios; database and runtime behavior remain unchanged.

#### Add User and Role Persistence for Bootstrap - 3 hours

**Type:** Earning

**Outcome:** An additive migration can persist an active user with an admin role without exposing creation over HTTP.

**Verify:** Apply migration to an empty database, insert/read the modeled admin in a test, and roll migration back on a disposable database.

**Rollback:** Roll back the additive migration before any production bootstrap data exists.

### Phase 2: Deliver the Operator Command

#### Implement Idempotent Bootstrap Command - 3 hours

**Type:** Earning

**Outcome:** A deployment command creates the configured active administrator or reports that the same administrator already exists.

**Verify:** Run twice against a clean database and confirm exactly one active admin with unchanged identity.

**Rollback:** Revert the command; retain the valid admin row or remove it manually before the account is used.

#### Connect Seeded Admin to Authentication Request - 2 hours

**Type:** Earning

**Outcome:** Existing authentication request logic recognizes the seeded active admin without adding registration.

**Verify:** Request authentication for the seeded email and assert no anonymous account-creation endpoint exists.

**Rollback:** Revert authentication integration while leaving bootstrap data intact for diagnosis.

## Stop Conditions

- Stop if rerunning the command creates, demotes, or mutates an existing account unexpectedly.
- Stop if bootstrap requires a public registration endpoint.
- Stop on an existing non-admin account with the configured email; require explicit operator resolution.

## Completion Checklist

- [ ] Command creates exactly one active admin.
- [ ] Repeated execution is idempotent.
- [ ] Seeded admin can request authentication.
- [ ] No general registration endpoint exists.
