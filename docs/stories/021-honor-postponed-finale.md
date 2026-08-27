# Story 021: Honor a Postponed Finale

**As a** user, **I want** latest schedule checked before notification **so that** postponed finales do not trigger stale messages.

## Acceptance Criteria

- Pre-send refresh can replace unconfirmed schedule.
- Future revised airtime leaves season incomplete.
- Old candidate creates no digest item.
- Revised airtime can later complete season normally.

## Dependencies

- Stories 017 and 020.

## Small Safe Steps

### Phase 1: Learn Refresh Outcomes

#### Step 1: Capture schedule revision scenarios - 1 hour

**Type:** Learning
**Outcome:** Fixtures cover unchanged, postponed, earlier, missing, contradictory, and provider-failure refreshes.
**Verify:** Each fixture has expected candidate action and completion decision reviewed against Stories 017 and 020.
**Rollback:** Remove fixtures without changing send behavior.

### Phase 2: Refresh Before Sending

#### Step 2: Add read-only pre-send schedule refresh - 2 hours

**Type:** Earning
**Outcome:** Digest preparation can fetch latest normalized schedule before consuming a candidate.
**Verify:** Adapter tests prove refresh maps fixtures without mutating candidate or season state.
**Rollback:** Disable refresh call; existing candidate remains queued and unsent during rollout.

#### Step 3: Replace unconfirmed schedule atomically - 2 hours

**Type:** Earning
**Outcome:** Valid revised schedule replaces unconfirmed evidence while preserving candidate and completion history.
**Verify:** Integration test commits revised evidence and rolls back fully on injected storage failure.
**Rollback:** Disable replacement writes; retain previous evidence for inspection.

### Phase 3: Gate Digest Items

#### Step 4: Suppress stale postponed candidate - 2 hours

**Type:** Earning
**Outcome:** Revised future airtime keeps season incomplete and excludes old candidate from digest items.
**Verify:** Integration test refreshes to future airtime and observes no digest item or completion event.
**Rollback:** Disable sending for refreshed candidates while retaining them for later re-evaluation.

#### Step 5: Complete normally at revised airtime - 2 hours

**Type:** Earning
**Outcome:** Later evaluation at revised airtime creates one completion and one eligible digest item.
**Verify:** Fixed-clock end-to-end test advances past revised airtime and asserts exactly one event and item.
**Rollback:** Disable revised-schedule candidate processing; persisted schedule remains available for retry.

## Stop Conditions

- Stop sending if refresh fails, returns contradictory data, or cannot persist a valid revision.
- Stop rollout if old candidate reaches a digest after airtime moves into future.

## Completion Checklist

- [ ] Every candidate is refreshed before send.
- [ ] Postponed finale produces no stale digest item.
- [ ] Revised airtime can complete exactly once.
