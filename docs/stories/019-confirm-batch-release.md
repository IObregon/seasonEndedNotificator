# Story 019: Confirm a Batch-Release Season

**As a** notification system, **I want** full-season releases treated as completed after release date **so that** streaming seasons are covered.

## Acceptance Criteria

- Complete batch is represented as completion evidence.
- Release date uses same original-zone date-ending rule.
- Partial episode drops do not qualify.
- Result emits at most one completion event.

## Dependencies

- Story 018.

## Small Safe Steps

### Phase 1: Define Complete Batch Evidence

#### Step 1: Classify release schedule examples - 1 hour

**Type:** Learning
**Outcome:** Fixtures classify full-season releases, partial drops, split batches, and ambiguous schedules.
**Verify:** Each fixture has reviewed expected evidence: complete batch, incomplete, or uncertain.
**Rollback:** Remove fixtures without changing completion behavior.

### Phase 2: Add Batch Policy

#### Step 2: Represent complete-batch evidence - 1 hour

**Type:** Earning
**Outcome:** Domain can carry complete-batch release date and original zone separately from episode-finale evidence.
**Verify:** Unit tests reject partial-drop inputs from complete-batch construction.
**Rollback:** Revert new evidence variant before runtime dispatch uses it.

#### Step 3: Reuse original-zone date-ending rule - 2 hours

**Type:** Earning
**Outcome:** Complete batches become eligible only after release date ends in original zone.
**Verify:** Fixed-clock tests prove listed-day withholding and next-day eligibility across offsets.
**Rollback:** Remove batch policy branch; batch releases remain unconfirmed.

### Phase 3: Integrate Conservatively

#### Step 4: Route confirmed batches to completion - 1 hour

**Type:** Earning
**Outcome:** Only classified complete batches reach existing atomic completion transition.
**Verify:** Integration tests show complete batch emits once while partial and ambiguous schedules emit none.
**Rollback:** Disable batch dispatch without affecting timestamped or date-only finales.

#### Step 5: Verify repeated batch evaluation - 1 hour

**Type:** Earning
**Outcome:** Reprocessing same complete batch leaves one completion event.
**Verify:** Integration test evaluates release repeatedly and counts one event and one completed state.
**Rollback:** Disable batch dispatch while retaining stored completion for already processed seasons.

## Stop Conditions

- Stop rollout if partial or ambiguous releases classify as complete batches.
- Stop if original-zone date-ending behavior diverges from Story 018.

## Completion Checklist

- [ ] Partial drops never qualify.
- [ ] Release-date boundary tests pass.
- [ ] Batch completion emits at most once.
