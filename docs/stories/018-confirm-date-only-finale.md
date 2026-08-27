# Story 018: Confirm a Date-Only Finale

**As a** notification system, **I want** date-only finales held until the next original-zone day **so that** notification is not sent before airing.

## Acceptance Criteria

- Finale is ineligible throughout listed local date.
- It becomes eligible after local midnight.
- Daylight-saving boundaries are covered by tests.
- Existing precise-timestamp behavior remains unchanged.

## Dependencies

- Story 017.

## Small Safe Steps

### Phase 1: Resolve Calendar Boundaries

#### Step 1: Build date-only timezone examples - 2 hours

**Type:** Learning
**Outcome:** Executable matrix covers listed date, next local midnight, spring-forward, and fall-back zones.
**Verify:** Fixed-clock examples map each instant to an explicit eligible or ineligible decision.
**Rollback:** Remove matrix tests without changing precise-timestamp policy.

### Phase 2: Expand Completion Evidence

#### Step 2: Represent date-only evidence separately - 1 hour

**Type:** Earning
**Outcome:** Domain distinguishes date-only evidence from precise `airstamp` evidence without changing existing evaluation.
**Verify:** Construction tests prove both evidence variants retain original-zone information.
**Rollback:** Revert evidence variant before any persisted representation is enabled.

#### Step 3: Evaluate after next local midnight - 2 hours

**Type:** Earning
**Outcome:** Date-only policy stays ineligible throughout listed local date and becomes eligible next day.
**Verify:** Matrix from Step 1 passes, including daylight-saving transitions.
**Rollback:** Remove date-only policy branch; precise timestamps continue through existing branch.

### Phase 3: Integrate Without Regression

#### Step 4: Route date-only evidence to new policy - 1 hour

**Type:** Earning
**Outcome:** Runtime dispatches date-only finales to calendar policy while precise timestamps retain prior path.
**Verify:** Integration tests exercise both evidence types and compare precise results with baseline fixtures.
**Rollback:** Disable date-only dispatch and withhold those finales as unsupported.

#### Step 5: Verify exactly-once date completion - 1 hour

**Type:** Earning
**Outcome:** Repeated post-midnight evaluation emits at most one completion event.
**Verify:** Integration test evaluates before midnight and twice after midnight, then counts one event.
**Rollback:** Disable date-only dispatch while preserving precise completion processing.

## Stop Conditions

- Stop release if any date-only finale completes during its listed local date.
- Stop if precise-timestamp regression fixtures change.

## Completion Checklist

- [ ] Both daylight-saving boundaries pass.
- [ ] Precise behavior is unchanged.
- [ ] Date-only event remains exactly once.
