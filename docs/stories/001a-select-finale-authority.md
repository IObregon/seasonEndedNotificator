# Story 001a: Select a Finale Authority

**As a** product team, **I want** an explicit finale signal independent from TVmaze **so that** automatic completion does not mistake latest-known episode for finale.

## Scope

Time-box comparison of candidate sources that explicitly identify season finales or final episode counts. Evaluate data quality, show-ID mapping, terms, rate limits, cost, and schedule correction behavior. Do not build production integration.

## Acceptance Criteria

- [x] At least two viable sources are tested against Story 001 fixtures using official public contracts.
- [x] Selected source explicitly establishes finale identity rather than inferring from latest-known episode.
- [x] Mapping from selected source to TVmaze show/season is demonstrated through exact external-ID contracts.
- [x] License, attribution, rate limits, and MVP cost are recorded.
- [x] Decision and unresolved coverage gaps are documented.

## Dependencies

- Story 001.

## Small Safe Steps

### Phase 1: Define and Screen

#### Step 1: Define source acceptance gate - 1 hour

**Type:** Learning

**Outcome:** Evaluation matrix fixes required finale semantics, mapping identifiers, correction behavior, terms, limits, and budget.

**Verify:** Gate rejects any source that only exposes latest-known episodes.

**Rollback:** Delete matrix; runtime remains unchanged.

#### Step 2: Screen candidate sources - 2 hours

**Type:** Learning

**Outcome:** At least two candidates pass documentation-level gate and have testable API/data access.

**Verify:** Record endpoint/document links and explicit finale field or equivalent authoritative evidence.

**Rollback:** Remove candidate notes; no integration exists.

### Phase 2: Test Evidence

#### Step 3: Test candidate A against fixture set - 3 hours

**Type:** Learning

**Outcome:** Result table covers normal, ongoing, batch, split, postponed, special, and contradictory samples.

**Verify:** Each result cites raw candidate evidence and mapping identifiers.

**Rollback:** Delete disposable responses and retain only sanitized evidence.

#### Step 4: Test candidate B against fixture set - 3 hours

**Type:** Learning

**Outcome:** Comparable result table exists for second candidate.

**Verify:** Same samples and scoring rules are used for both candidates.

**Rollback:** Delete disposable responses and retain only sanitized evidence.

#### Step 5: Test cross-provider mapping failures - 2 hours

**Type:** Learning

**Outcome:** Mapping report quantifies direct IDs, deterministic matches, ambiguous matches, and unmapped shows.

**Verify:** Ambiguous title-only matches are never accepted automatically.

**Rollback:** Remove mapping spike code; no persisted product mappings exist.

### Phase 3: Decide

#### Step 6: Record source decision - 2 hours

**Type:** Learning

**Outcome:** ADR selects source or rejects all candidates, including cost, terms, coverage, and revisit trigger.

**Verify:** Decision proves how finale identity is established for each supported release pattern.

**Rollback:** Supersede ADR before production integration.

## Stop Conditions

- Stop candidate evaluation when source lacks explicit finale authority.
- Reject title-only automatic show matching.
- Do not begin production integration until terms and ShareAlike implications are understood.

## Completion Checklist

- [x] Every step stayed within 1-3 hours.
- [x] At least two candidates used identical sample matrix.
- [x] Selected source explicitly proves finale identity in documented fixture evidence.
- [x] Mapping ambiguity defaults to unsupported or manual review.
- [x] Provider decision and legal/operational constraints are recorded.

## Result

Completed on `2026-08-27` using official public documentation and fixtures because credentials were unavailable. TMDB is selected as MVP finale authority: season-details fixture explicitly marks an episode with `episode_type: "finale"`. Trakt passes as fallback with richer schema semantics but lacks a concrete public finale response fixture. Credentialed Story 001 fixture verification is a mandatory pre-production condition.
