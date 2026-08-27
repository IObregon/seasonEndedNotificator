# Story 015: User Unfollows a Show

**As a** user, **I want** to unfollow a show **so that** future season notifications stop.

## Acceptance Criteria

- [x] Active follow can be removed from dashboard.
- [x] Unfollow does not delete show metadata or past delivery history.
- [x] Re-follow records a new eligibility timestamp.
- [x] Operation is idempotent.

## Dependencies

- Story 014.

## Small Safe Steps

### Phase 1: Protect Historical Data

#### Step 1: Characterize follow history boundaries - 1 hour

**Type:** Learning
**Outcome:** Tests document which show metadata and delivery-history records must survive unfollow and re-follow.
**Verify:** Tests seed related records and identify only active follow state as removable.
**Rollback:** Remove characterization tests without changing data.

### Phase 2: Remove Active Follow Safely

#### Step 2: Implement idempotent unfollow command - 2 hours

**Type:** Earning
**Outcome:** Command removes active user-show follow while leaving show metadata and delivery history intact.
**Verify:** Integration test calls command twice and confirms protected records remain unchanged.
**Rollback:** Remove command endpoint; no schema or historical data rollback is needed.

#### Step 3: Expose unfollow on dashboard - 1 hour

**Type:** Earning
**Outcome:** User can remove a followed show and immediately sees it leave the dashboard.
**Verify:** UI test performs unfollow and asserts success plus absent dashboard row.
**Rollback:** Hide action; existing follows and backend behavior remain valid.

### Phase 3: Verify Re-follow Semantics

#### Step 4: Record a new timestamp on re-follow - 2 hours

**Type:** Earning
**Outcome:** Following after unfollow creates a new eligibility timestamp rather than restoring the old one.
**Verify:** Clock-controlled integration test proves new timestamp is later and old delivery history remains.
**Rollback:** Revert re-follow path while retaining unfollow behavior.

#### Step 5: Add end-to-end follow cycle coverage - 1 hour

**Type:** Learning
**Outcome:** Automated artifact documents follow, unfollow, repeated unfollow, and re-follow behavior as one user journey.
**Verify:** End-to-end test passes and checks both dashboard state and persisted timestamps.
**Rollback:** Remove end-to-end test; covered production behavior remains unchanged.

## Completion Checklist

- [x] Repeated unfollow succeeds safely.
- [x] Metadata survives; delivery history remains untouched when introduced.
- [x] Re-follow gets a new eligibility timestamp.

## Result

Completed on `2026-08-27`. Idempotent unfollow removes only the active `ShowFollow`; show and season metadata remain. Re-follow creates a new record with a later eligibility timestamp. Authenticated DELETE endpoint succeeds even when no active follow exists, and dashboard removes the unfollowed row immediately.
