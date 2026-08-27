# Story 007: User Signs In by Magic Link

**As an** active user, **I want** an emailed magic link **so that** I can sign in without a password.

## Acceptance Criteria

- Request response does not reveal account existence.
- Active user receives short-lived single-use link.
- Consuming valid link creates secure session.
- Disabled users and reused links cannot authenticate.

## Dependencies

- Story 006.

## Small Safe Steps

### Phase 1: Define Non-Disclosure and Token Rules

#### Specify Magic-Link Threat Scenarios - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios define uniform request responses, token lifetime, hashing, replay resistance, and disabled-user behavior.

**Verify:** Compare response status, body, and observable timing envelope for known and unknown emails; map all acceptance criteria.

**Rollback:** Remove the scenarios; authentication behavior remains unchanged.

#### Add Hashed Magic-Link Persistence - 3 hours

**Type:** Earning

**Outcome:** An additive migration stores token hash, user reference, expiry, and consumed time without plaintext secrets.

**Verify:** Apply and roll back on a disposable database; persistence tests prove raw tokens are absent.

**Rollback:** Roll back before issuing links, or leave the unused additive table in place.

### Phase 2: Request a Link Safely

#### Add Non-Disclosing Request Endpoint - 3 hours

**Type:** Earning

**Outcome:** Every syntactically valid request receives the same response, while only active users get a short-lived token and email.

**Verify:** Compare known, unknown, and disabled requests; inspect Mailpit and persisted hashes for expected side effects.

**Rollback:** Disable the endpoint; existing sessions and invitation authentication continue working.

### Phase 3: Consume and Establish Session

#### Consume Link Atomically - 3 hours

**Type:** Earning

**Outcome:** A valid unused link is consumed once and creates a secure session only if the user remains active.

**Verify:** Test valid, expired, replayed, disabled-after-issue, and concurrent requests through the HTTP boundary.

**Rollback:** Disable consumption and invalidate outstanding records; existing authenticated sessions remain unaffected.

## Stop Conditions

- Stop if account existence differs in response status or body.
- Stop if plaintext tokens enter storage or logs.
- Stop if consumption and session creation permit replay or a disabled account.

## Completion Checklist

- [ ] Request response does not disclose account existence.
- [ ] Active users receive expiring single-use links.
- [ ] Valid consumption creates a secure session.
- [ ] Disabled users and replayed links cannot authenticate.
