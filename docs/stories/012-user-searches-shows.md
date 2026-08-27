# Story 012: User Searches for Shows

**As a** signed-in user, **I want** to search by show title **so that** I can find a show to follow.

## Acceptance Criteria

- Backend queries TVmaze through anti-corruption layer.
- Result shows title, premiere year, status, and image when available.
- TVmaze DTOs do not enter Domain.
- Empty, failed, and rate-limited searches have clear states.

## Dependencies

- Stories 001 and 007.

## Small Safe Steps

### Phase 1: Learn the Search Contract

#### Step 1: Capture provider response cases - 1 hour

**Type:** Learning
**Outcome:** Checked-in fixtures document successful, empty, rate-limited, and failed TVmaze searches without changing runtime behavior.
**Verify:** Fixture tests parse each captured response and reviewers can trace every acceptance state to a fixture.
**Rollback:** Remove fixtures and fixture-only tests.

### Phase 2: Add the Search Path

#### Step 2: Map TVmaze results into search records - 2 hours

**Type:** Earning
**Outcome:** Anti-corruption layer returns provider-independent title, premiere year, status, and optional image records.
**Verify:** Unit tests prove TVmaze DTO types and provider-only fields do not cross the adapter boundary.
**Rollback:** Revert mapper and tests; existing application paths remain unchanged.

#### Step 3: Expose authenticated search endpoint - 2 hours

**Type:** Earning
**Outcome:** Signed-in users can query mapped show results through a read-only backend endpoint.
**Verify:** Endpoint tests cover authentication, successful results, and empty query behavior.
**Rollback:** Remove route and handler without changing stored data.

### Phase 3: Present Safe User States

#### Step 4: Render successful and empty searches - 2 hours

**Type:** Earning
**Outcome:** Search UI displays available metadata and a clear no-results state.
**Verify:** UI tests assert result fields, missing-image fallback, and empty-state copy.
**Rollback:** Remove search view entry point; backend endpoint can remain unused.

#### Step 5: Render failure and rate-limit states - 1 hour

**Type:** Earning
**Outcome:** Provider failures and rate limits show distinct retry-safe messages instead of broken results.
**Verify:** UI tests inject both endpoint responses and assert clear states plus retry action.
**Rollback:** Revert error-state rendering to prior generic handling.

## Completion Checklist

- [ ] All four search states pass automated tests.
- [ ] Domain has no TVmaze DTO dependency.
- [ ] Search flow works for a signed-in user.
