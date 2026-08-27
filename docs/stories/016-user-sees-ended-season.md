# Story 016: User Sees an Already-Ended Season

**As a** user, **I want** existing completion status shown when following **so that** I understand show state without receiving stale notifications.

## Acceptance Criteria

- Detail page shows known completion date.
- Following after completion creates no notification candidate for that season.
- Future seasons remain eligible.
- Behavior has automated test around follow timestamp.

## Dependencies

- Stories 014 and 017.

## Small Safe Steps

### Phase 1: Define the Time Boundary

#### Step 1: Capture follow-time eligibility examples - 1 hour

**Type:** Learning
**Outcome:** Clock-based examples define completed-before-follow, completed-at-follow, and completed-after-follow outcomes.
**Verify:** Examples explicitly identify stale and future notification candidates.
**Rollback:** Remove examples without changing runtime behavior.

### Phase 2: Show Existing Completion

#### Step 2: Expose known completion date in details - 1 hour

**Type:** Earning
**Outcome:** Detail query includes normalized completion date for an already-ended regular season.
**Verify:** Query test covers known and unknown completion dates.
**Rollback:** Remove response field; stored completion data remains unchanged.

#### Step 3: Render completed-season status - 1 hour

**Type:** Earning
**Outcome:** Detail page clearly shows existing season completion date before user follows.
**Verify:** UI test asserts completed label and date for seeded detail data.
**Rollback:** Remove status presentation without affecting follow behavior.

### Phase 3: Prevent Stale Candidates

#### Step 4: Filter completions by follow timestamp - 2 hours

**Type:** Earning
**Outcome:** Candidate policy excludes seasons completed at or before follow time while retaining later seasons.
**Verify:** Clock-controlled domain tests cover all boundary examples from Step 1.
**Rollback:** Revert policy change before enabling candidate generation for this path.

#### Step 5: Verify follow-after-completion journey - 2 hours

**Type:** Learning
**Outcome:** Integration artifact proves follow succeeds, ended state remains visible, and no stale candidate is stored.
**Verify:** Test follows an ended show, runs candidate generation, and observes zero stale candidates.
**Rollback:** Remove integration test; production behavior remains unchanged.

## Stop Conditions

- Stop release if any completion at or before follow timestamp creates a candidate.
- Stop if filtering suppresses a season completed after follow timestamp.

## Completion Checklist

- [ ] Completion date is visible before following.
- [ ] Timestamp boundary tests pass.
- [ ] Future-season eligibility remains intact.
