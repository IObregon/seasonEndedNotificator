# Story 014: User Follows a Show

**As a** user, **I want** to follow a show **so that** future completed seasons become notification candidates.

## Acceptance Criteria

- [x] Follow records user, show, and current timestamp.
- [x] Duplicate follow is idempotent.
- [x] Dashboard lists followed show.
- [x] Follow applies to future seasons without season selection.

## Dependencies

- Story 013.

## Small Safe Steps

### Phase 1: Establish Follow Rules

#### Step 1: Specify follow eligibility examples - 1 hour

**Type:** Learning
**Outcome:** Executable examples define first follow, duplicate follow, and future-season eligibility from one timestamp.
**Verify:** Examples run against a test seam and fail only because follow behavior is not implemented.
**Rollback:** Remove examples without changing production behavior.

### Phase 2: Persist Follows

#### Step 2: Add follow storage constraint - 2 hours

**Type:** Earning
**Outcome:** Follow records can store user, show, and creation timestamp with one active record per pair.
**Verify:** Migration test applies and reverses schema; constraint test rejects duplicate active pairs.
**Rollback:** Reverse migration before any follow writes are enabled.

#### Step 3: Implement idempotent follow command - 2 hours

**Type:** Earning
**Outcome:** Authenticated command creates one timestamped follow and returns existing follow on repetition.
**Verify:** Integration test submits command twice and observes one unchanged record.
**Rollback:** Remove command endpoint; table can remain unused.

### Phase 3: Make Following Visible

#### Step 4: Add follow action to show details - 1 hour

**Type:** Earning
**Outcome:** User can follow a show from details and receives stable feedback on repeated clicks.
**Verify:** UI test clicks twice and confirms one followed state without an error.
**Rollback:** Hide follow control; backend command remains compatible.

#### Step 5: List followed shows on dashboard - 2 hours

**Type:** Earning
**Outcome:** Dashboard displays current user's followed shows from a read-only query.
**Verify:** Query and UI tests prove user isolation and expected show rendering.
**Rollback:** Remove dashboard section without deleting follow records.

## Completion Checklist

- [x] Duplicate follow is idempotent.
- [x] Follow timestamp is persisted once.
- [x] Dashboard is isolated by user.

## Result

Completed on `2026-08-27`. `ShowFollow` stores one unique user/show pair and immutable follow timestamp. Repeated follow returns the existing record unchanged. Authenticated endpoints follow imported shows by provider ID and list only the current user's followed shows. Detail UI exposes stable follow feedback and dashboard refresh renders followed titles.
