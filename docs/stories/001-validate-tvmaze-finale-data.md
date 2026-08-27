# Story 001: Validate TVmaze Finale Data

**As a** product team, **I want** evidence that TVmaze can identify season finales **so that** false notifications are not built into the product.

## Scope

Time-box investigation to two days. Check representative ongoing, completed, postponed, batch-release, split, date-only, timezone, and season-zero examples. Save sanitized fixtures and document confidence rules.

## Acceptance Criteria

- [x] Each sample has expected completion result and rationale.
- [x] Gaps and contradictory fields produce `Uncertain`.
- [x] Decision records whether TVmaze alone is adequate.
- [x] No production integration is built.

## Dependencies

None.

## Small Safe Steps

All steps are **Learning** steps. Keep spike code outside production projects. Every step must end with a committed or reviewable artifact; if timebox expires, record unknowns instead of extending scope.

### Phase 1: Define Evidence

#### Step 1.1: Define Decision Questions - 1 hour

- Write exact questions TVmaze must answer: season identity, regular episodes, final episode, original timezone, precise airtime, date-only release, episode order, and schedule updates.
- Define outcomes `Completed`, `NotCompleted`, and `Uncertain` in provider-independent language.
- Record minimum acceptable evidence for each outcome.

**Artifact:** Decision-question and outcome table in this story or linked spike notes.

**Verify:** Every acceptance criterion maps to at least one question.

**Safe/reversible:** Documentation only; revise or delete without product impact.

#### Step 1.2: Select Representative Shows - 1 hour

- Select one known example for each category: ongoing, normally completed, postponed, batch release, split season, date-only, timezone edge, and season zero/specials.
- Record TVmaze show ID, expected result, and why example matters.
- Prefer recent data that can be checked against an independent public source.

**Artifact:** Sample matrix with eight or more provider IDs and expected outcomes.

**Verify:** Matrix covers every category in Scope exactly enough to answer decision questions.

**Safe/reversible:** Sample list can change without code or data migration.

### Phase 2: Capture Provider Evidence

#### Step 2.1: Capture Baseline Show and Season Responses - 2 hours

- Fetch TVmaze show and season responses for selected samples.
- Save raw responses only in temporary local storage.
- Create sanitized, minimal fixtures containing fields relevant to decision questions.
- Record endpoint URL and retrieval date beside each fixture.

**Artifact:** Reviewable show/season fixtures with no unrelated payload fields.

**Verify:** Fixtures parse as JSON and retain IDs, season numbers, dates, timezone-related data, episode order, and update evidence when available.

**Safe/reversible:** Fixtures are isolated spike artifacts and can be deleted.

#### Step 2.2: Capture Episode and Schedule Responses - 2 hours

- Fetch episode and schedule data for same samples.
- Sanitize into minimal fixtures preserving episode type, season, number, air date, airtime, and `airstamp`.
- Note absent fields instead of inventing defaults.

**Artifact:** Reviewable episode/schedule fixtures paired with sample matrix.

**Verify:** Each expected finale can be located, or its absence is explicitly recorded.

**Safe/reversible:** No production database or integration touched.

#### Step 2.3: Verify Samples Independently - 2 hours

- Compare expected finale and postponement facts with one independent source per sample.
- Record discrepancies without changing TVmaze payloads.
- Replace unsuitable samples when expected real-world result cannot be established.

**Artifact:** Evidence table containing expected fact, independent source, TVmaze result, and discrepancy.

**Verify:** Every sample has either corroboration or explicit `Unknown` status.

**Safe/reversible:** Research notes only.

### Phase 3: Test Candidate Rules

#### Step 3.1: Evaluate Straightforward Completed and Ongoing Seasons - 2 hours

- Apply proposed rules manually or with disposable spike code to normal completed and ongoing samples.
- Record which fields establish or prevent completion.
- Do not generalize from one successful example.

**Artifact:** Result table for normal completed and ongoing samples.

**Verify:** Actual result matches expected result, or discrepancy has reproducible evidence.

**Safe/reversible:** Disposable evaluator has no production dependency.

#### Step 3.2: Evaluate Date-Only and Timezone Cases - 2 hours

- Check whether TVmaze provides enough original-zone data to decide when listed day has ended.
- Evaluate timestamps immediately before and after local midnight.
- Record daylight-saving ambiguity or missing timezone data as `Uncertain`.

**Artifact:** Boundary examples with input time, zone, expected state, and actual available evidence.

**Verify:** No date-only sample becomes completed during its listed original-zone day.

**Safe/reversible:** Test calculations only.

#### Step 3.3: Evaluate Batch and Split Releases - 2 hours

- Check full-season batch sample for evidence that all regular episodes released together.
- Check split-season sample for false completion after first part.
- Mark missing release semantics `Uncertain` rather than infer from last known episode.

**Artifact:** Batch/split result table and identified provider gaps.

**Verify:** Partial release never qualifies solely because it is last currently known episode.

**Safe/reversible:** No domain model is committed from spike conclusions yet.

#### Step 3.4: Evaluate Postponements and Specials - 2 hours

- Compare postponed sample's old and current schedule evidence when available.
- Confirm season zero and special episode types cannot qualify.
- Identify whether TVmaze history supports detecting changes or only latest state.

**Artifact:** Postponement and exclusion result table.

**Verify:** Current future schedule yields `NotCompleted`; season zero and specials always remain ineligible.

**Safe/reversible:** Findings do not alter application behavior.

### Phase 4: Decide

#### Step 4.1: Draft Confidence Rules - 2 hours

- Convert observed evidence into smallest provider-independent rule set.
- For every rule, cite at least one supporting fixture and one failure/edge fixture where applicable.
- Route missing or contradictory evidence to `Uncertain`.
- Separate proven rules from assumptions requiring production observation.

**Artifact:** Versioned confidence-rule table with evidence links.

**Verify:** Running rules against sample matrix produces recorded expected outcome or documented unresolved mismatch.

**Safe/reversible:** Rules remain proposal until decision approval.

#### Step 4.2: Make Provider Decision - 1 hour

- Score TVmaze on catalog coverage, finale evidence, timezone precision, postponement handling, rate limits, and operational cost.
- Choose one outcome: `TVmaze sufficient`, `TVmaze sufficient with uncertain cases withheld`, or `second provider spike required`.
- State rejected alternatives and trigger for revisiting decision.

**Artifact:** Short architecture decision record linked from this story.

**Verify:** Decision directly answers whether TVmaze alone satisfies MVP acceptance criteria.

**Safe/reversible:** Decision can be superseded before production integration.

#### Step 4.3: Close Spike - 1 hour

- Confirm fixtures contain only useful provider data.
- Remove disposable secrets, raw downloads, and abandoned spike code.
- Link retained fixtures, rule table, discrepancies, and decision record.
- Create follow-up story only for unresolved risk that changes MVP feasibility.

**Artifact:** Complete evidence index and clean workspace.

**Verify:** Another developer can reproduce decision from retained artifacts without verbal context.

**Safe/reversible:** Cleanup removes non-production artifacts only.

## Stop Conditions

Stop early and decide `second provider spike required` when any condition holds:

- TVmaze cannot distinguish regular finale from last currently known episode for representative shows.
- Original broadcast timezone cannot be established reliably.
- Batch and split releases cannot be classified conservatively.
- Schedule updates cannot be rechecked before notification.
- Required use exceeds documented TVmaze access or rate constraints.

## Completion Checklist

- [x] Every step stayed within 1-3 hours.
- [x] Sample matrix covers all listed categories.
- [x] Retained fixtures are sanitized and reproducible.
- [x] Expected facts have independent evidence or explicit `Unknown` status.
- [x] Confidence rules default gaps and contradictions to `Uncertain`.
- [x] Provider decision is recorded with revisit trigger.
- [x] No production integration or irreversible change was introduced.

## Result

Completed on `2026-08-27`. TVmaze is suitable for catalog and schedule data but cannot independently prove finale identity. Story reached its documented stop condition; a second-source spike is required before implementing automatic completion.

Evidence: [`../spikes/tvmaze-finale-validation/README.md`](../spikes/tvmaze-finale-validation/README.md).
