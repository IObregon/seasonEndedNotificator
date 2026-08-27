# Story 017: Confirm a Timestamped Finale

**As a** notification system, **I want** to confirm an explicitly identified regular finale after it finishes airing **so that** one trustworthy completion event exists.

## Acceptance Criteria

- [x] Completion policy excludes season zero and specials.
- [x] Completion policy requires explicit finale identity from authority selected in Story 001a.
- [x] Completion policy compares current time with episode end (`airstamp + runtime`) or a conservative fallback buffer.
- [x] A regular episode without finale authority remains incomplete after airing.
- [x] Transition persists one season-completion event.
- [x] Re-evaluation cannot create a duplicate completion event.

## Dependencies

- Stories 001 and 001a.

## Small Safe Steps

### Phase 1: Lock Down Evidence Rules

#### Step 1: Document timestamp and exclusion cases - 1 hour

**Type:** Learning
**Outcome:** Executable examples cover finale authority, ordinary regular episodes, before/at/after episode end, original zones, season zero, and specials.
**Verify:** Example table has one expected completion decision per case and runs deterministically with a fixed clock.
**Rollback:** Remove examples without changing completion behavior.

### Phase 2: Add Completion Policy

#### Step 2: Introduce regular-season eligibility value - 1 hour

**Type:** Earning
**Outcome:** `TrackedSeason` construction rejects season zero and specials without changing event emission.
**Verify:** Domain tests accept numbered regular seasons and reject excluded inputs.
**Rollback:** Revert value-object validation; no persisted format changes are required.

#### Step 3: Evaluate precise episode end in original zone - 2 hours

**Type:** Earning
**Outcome:** Pure completion policy reports eligible only for an authoritative finale when current time reaches `airstamp + runtime` or conservative fallback buffer.
**Verify:** Fixed-clock tests cover boundary instants, missing-runtime fallback, absent finale authority, ordinary regular episodes, and multiple offsets.
**Rollback:** Remove policy from runtime wiring; existing state remains untouched.

### Phase 3: Emit Exactly Once

#### Step 4: Persist completion and event atomically - 3 hours

**Type:** Earning
**Outcome:** First eligible transition stores completed state and one `SeasonCompleted` event in one transaction.
**Verify:** Integration test proves state and event commit together and both roll back on injected failure.
**Rollback:** Disable transition handler and revert schema addition before processing production seasons.

#### Step 5: Make re-evaluation idempotent - 1 hour

**Type:** Earning
**Outcome:** Repeated evaluation of a completed season creates no additional event.
**Verify:** Integration test evaluates same season twice and counts exactly one persisted event.
**Rollback:** Revert idempotency guard only while transition handler remains disabled.

## Stop Conditions

- Stop rollout if season zero or specials can enter `TrackedSeason`.
- Stop rollout if TVmaze data alone can establish finale identity or broadcast start can establish completion.
- Stop processing if transaction-failure or duplicate-evaluation tests produce an event count other than zero or one as expected.

## Completion Checklist

- [x] Finale authority and original-zone episode-end boundaries pass.
- [x] Completion and event persist atomically.
- [x] Re-evaluation emits no duplicate.

## Result

Completed on `2026-08-27`. Pure completion policy requires positive season number, regular episode type, explicit finale authority, and elapsed episode end. Missing runtime uses a conservative two-hour buffer. Eligible evaluation stores season completion and one uniquely constrained event in one save; repeated evaluation is idempotent. TMDB adapter activation remains subject to ADR 0002 credentialed pre-production verification.
