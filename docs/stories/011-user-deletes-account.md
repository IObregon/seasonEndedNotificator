# Story 011: User Deletes Own Account

**As a** user, **I want** to delete my account **so that** retained personal data is removed.

## Acceptance Criteria

- Recent authentication and explicit confirmation are required.
- Sessions, tokens, follows, preferences, and channel connections are removed.
- Operational records are deleted or anonymized.
- Last active admin cannot self-delete without transferring admin responsibility.

## Dependencies

- Story 010.

## Small Safe Steps

### Phase 1: Design Deletion Boundaries

#### Inventory Personal and Operational Data - 3 hours

**Type:** Learning

**Outcome:** A checked-in deletion matrix names every user-linked table, delete/anonymize policy, retention reason, verification query, and dependency order.

**Verify:** Compare schema and application references with the matrix; every user identifier has an explicit disposition.

**Rollback:** Remove the matrix; no user data changes.

#### Define Reauthentication and Admin-Guard Scenarios - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios define recent-authentication window, explicit confirmation, stale sessions, and last-active-admin rejection.

**Verify:** Scenarios fail against missing deletion behavior and cover concurrent admin transfer/deletion attempts.

**Rollback:** Remove the scenarios; production behavior remains unchanged.

### Phase 2: Build a Reversible Deletion Path

#### Add Disabled Pending-Deletion State - 3 hours

**Type:** Earning

**Outcome:** Confirmed eligible users can enter a disabled pending-deletion state that immediately blocks sessions and new authentication.

**Verify:** Request deletion with fresh and stale authentication, test confirmation, and prove pending users cannot authenticate.

**Rollback:** Clear pending state and reactivate the account before destructive erasure runs.

#### Revoke Sessions and Ephemeral Credentials - 2 hours

**Type:** Earning

**Outcome:** Entering pending deletion removes sessions, invitation/magic-link tokens, and channel credentials in one transaction.

**Verify:** Seed each credential type, request deletion, and assert none can authorize or deliver afterward.

**Rollback:** Cancel pending deletion and require fresh sign-in or channel reconnection; deleted credentials are not restored.

### Phase 3: Erase and Prove

#### Delete Owned Product Data - 3 hours

**Type:** Earning

**Outcome:** A retry-safe worker deletes follows, preferences, channel connections, and user-owned records for pending accounts.

**Verify:** Run against a complete fixture twice and confirm all targeted rows are absent with unrelated users untouched.

**Rollback:** Disable the worker; restore only from the documented backup path if verification detects incorrect scope.

#### Anonymize Retained Operational Records and Remove Account - 3 hours

**Type:** Earning

**Outcome:** Required operational records lose user identifiers, the account is removed, and a non-personal completion marker prevents duplicate work.

**Verify:** Run deletion-matrix queries proving no personal identifier remains and operational records still satisfy integrity constraints.

**Rollback:** Disable finalization before additional runs and restore the affected account/data from backup if anonymization scope is wrong.

## Stop Conditions

- Stop before destructive work if the deletion matrix has an unknown data owner or retention rule.
- Stop if last-active-admin protection is not enforced in the same transaction as pending deletion.
- Stop if a restorable backup and tested restoration procedure are unavailable.
- Stop if verification queries find another user's records in the deletion set.

## Completion Checklist

- [ ] Recent authentication and explicit confirmation are required.
- [ ] Last active admin must transfer responsibility first.
- [ ] Sessions, tokens, product data, preferences, and connections are removed.
- [ ] Retained operational records contain no user identity.
- [ ] Retry and verification prove deletion is scoped and complete.
