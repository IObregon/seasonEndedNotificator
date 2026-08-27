# Story 010: Administrator Grants Admin Role

**As an** administrator, **I want** to grant admin role **so that** administration is not tied to one person.

## Acceptance Criteria

- [x] Active user can be promoted and demoted.
- [x] New admin can access admin endpoints.
- [x] System prevents removal of last active admin.
- [x] Role change is audited.

## Dependencies

- Story 009.

## Small Safe Steps

### Phase 1: Establish Role Safety Rules

#### Define Role Transition Scenarios - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios define promotion, demotion, inactive targets, unauthorized actors, concurrent last-admin changes, and audit content.

**Verify:** Every acceptance criterion maps to a scenario, including two concurrent attempts to remove the last active admin.

**Rollback:** Remove the scenarios; authorization and data remain unchanged.

#### Add Append-Only Role Audit Storage - 2 hours

**Type:** Earning

**Outcome:** An additive audit record can capture actor, target, previous role, new role, and timestamp without changing authorization yet.

**Verify:** Apply and roll back the migration on a disposable database; persistence tests reject mutation of an existing audit entry.

**Rollback:** Roll back before role changes are recorded, or retain the inert additive table.

### Phase 2: Change Roles Atomically

#### Implement Promotion with Audit - 3 hours

**Type:** Earning

**Outcome:** An authorized admin can promote an active user, atomically recording the role change.

**Verify:** Promote a user and assert immediate admin endpoint access plus one complete audit record; test unauthorized and inactive targets.

**Rollback:** Demote the promoted test user through the inverse command or revert the endpoint before wider use.

#### Implement Guarded Demotion with Audit - 3 hours

**Type:** Earning

**Outcome:** An authorized admin can demote an admin only when another active admin remains, with decision and audit committed atomically.

**Verify:** Test normal demotion, sole-admin rejection, and concurrent demotions; assert at least one active admin always remains.

**Rollback:** Re-promote the affected user from an existing admin account and revert demotion exposure.

## Stop Conditions

- Stop if last-admin validation occurs outside the role-change transaction.
- Stop if role state can change without a durable audit record.
- Stop if inactive users can gain admin authorization.

## Completion Checklist

- [x] Active user can be promoted and demoted.
- [x] Promotion grants admin endpoint access immediately.
- [x] Last active admin cannot be removed under the serializable role-change transaction.
- [x] Every successful role change is audited.

## Result

Completed on `2026-08-27`. Role-protected endpoint promotes or demotes active users under a serializable transaction. Demotion requires another active admin. Every successful transition appends an immutable audit record containing actor, target, previous/new roles, and timestamp. Cookie validation refreshes role claims from current persisted state.
