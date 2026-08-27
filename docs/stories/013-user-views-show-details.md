# Story 013: User Views Show Details

**As a** user, **I want** to view one show's known seasons **so that** I can decide whether to follow it.

## Acceptance Criteria

- Selecting search result imports and displays normalized show metadata.
- Regular numbered seasons and known dates are shown.
- Specials are visually excluded from tracked seasons.
- Provider failure does not corrupt previously stored data.

## Dependencies

- Story 012.

## Small Safe Steps

### Phase 1: Define Normalization

#### Step 1: Record show-detail provider variants - 1 hour

**Type:** Learning
**Outcome:** Fixtures document numbered seasons, specials, missing dates, and provider failure responses.
**Verify:** Fixture tests load every variant and identify expected normalized fields.
**Rollback:** Remove fixtures and fixture-only tests.

### Phase 2: Import Without Corruption

#### Step 2: Normalize show and season metadata - 2 hours

**Type:** Earning
**Outcome:** Adapter converts provider details into domain-safe show and regular-season records while excluding specials.
**Verify:** Unit tests prove season zero and specials are absent and known dates survive mapping.
**Rollback:** Revert normalizer; search remains available without detail import.

#### Step 3: Persist detail import atomically - 2 hours

**Type:** Earning
**Outcome:** Successful imports replace known metadata in one transaction; failed imports preserve existing records.
**Verify:** Integration test seeds metadata, simulates provider failure, and confirms data is unchanged.
**Rollback:** Remove import command and migration if unused; retain existing metadata.

### Phase 3: Display Details

#### Step 4: Expose normalized show details - 1 hour

**Type:** Earning
**Outcome:** Read-only detail endpoint returns show metadata and numbered seasons with known dates.
**Verify:** Endpoint test asserts stored normalized output and not-found behavior.
**Rollback:** Remove route and handler; imported records remain harmless.

#### Step 5: Render show details from search selection - 2 hours

**Type:** Earning
**Outcome:** Selecting a result displays normalized show metadata and visually omits specials.
**Verify:** UI test selects a result and asserts numbered seasons, dates, and failure state.
**Rollback:** Remove detail navigation and view; search behavior remains intact.

## Stop Conditions

- Stop rollout if a failed provider request changes previously stored metadata.
- Stop if season zero or a special appears among tracked seasons.

## Completion Checklist

- [ ] Import and display paths use normalized data.
- [ ] Failure-preservation test passes.
- [ ] Specials remain excluded.
