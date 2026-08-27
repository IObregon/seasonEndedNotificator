# Story 020: Withhold an Uncertain Finale

**As a** user, **I want** uncertain provider data withheld **so that** I do not receive false finale notifications.

## Acceptance Criteria

- [x] Missing or contradictory evidence yields `Uncertain`.
- [x] Uncertain season emits no completion event.
- [x] Reason is persisted for admin inspection.
- [x] Later valid evidence can complete season.

## Dependencies

- Story 017.

## Small Safe Steps

### Phase 1: Enumerate Uncertainty

#### Step 1: Catalog missing and contradictory evidence - 1 hour

**Type:** Learning
**Outcome:** Decision table maps provider-data gaps and contradictions to stable uncertainty reason codes.
**Verify:** Domain review confirms every known unsafe input has a reason and no completion outcome.
**Rollback:** Remove decision table without changing runtime behavior.

### Phase 2: Withhold and Explain

#### Step 2: Return `Uncertain` from completion policy - 2 hours

**Type:** Earning
**Outcome:** Missing or contradictory evidence produces `Uncertain` with reason instead of completion.
**Verify:** Table-driven tests assert reason code and zero domain events for each unsafe input.
**Rollback:** Disable affected evaluations so uncertain inputs remain unprocessed.

#### Step 3: Persist uncertainty reason - 2 hours

**Type:** Earning
**Outcome:** Latest uncertainty reason is stored for admin inspection without marking season completed.
**Verify:** Integration test persists reason and confirms completed state and event store remain unchanged.
**Rollback:** Reverse additive schema migration after disabling reason writes.

### Phase 3: Recover on Better Evidence

#### Step 4: Replace uncertainty with valid evidence - 2 hours

**Type:** Earning
**Outcome:** Later valid evidence clears current uncertainty and uses normal completion policy.
**Verify:** Integration test moves uncertain season to one completed state and one event after valid evidence arrives.
**Rollback:** Disable recovery path; seasons remain safely uncertain for later retry.

#### Step 5: Preserve uncertainty audit coverage - 1 hour

**Type:** Learning
**Outcome:** End-to-end artifact demonstrates reason visibility, no early event, and later valid completion.
**Verify:** Scenario passes against persisted state and event count.
**Rollback:** Remove scenario test without changing production behavior.

## Stop Conditions

- Stop rollout if any `Uncertain` result emits or persists a completion event.
- Stop recovery if valid evidence can create more than one completion event.

## Completion Checklist

- [x] Every known uncertainty has a stable stored reason.
- [x] Uncertain seasons emit no completion.
- [x] Valid later evidence can complete once.

## Result

Completed on `2026-08-27`. Missing authority, schedule, timezone, mapping conflict, and episode-count conflict map to stable `UncertaintyReason` values. Reasons persist on incomplete seasons and are visible through the role-protected metadata-issues endpoint. Uncertain evidence creates no completion event; later valid evidence clears the reason and enters the existing exactly-once transition.
