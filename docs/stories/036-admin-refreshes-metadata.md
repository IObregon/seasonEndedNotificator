# Story 036: Administrator Refreshes Uncertain Metadata

**As an** administrator, **I want** to inspect and refresh uncertain seasons **so that** provider-data issues can be resolved.

## Acceptance Criteria

- View lists uncertain seasons and reasons.
- Admin can refresh one show.
- Refreshed evidence is re-evaluated by domain policy.
- Action and outcome are audited.

## Dependencies

- Stories 020 and 022.

## Small Safe Steps

### Phase 1: Make Uncertainty Visible

### Step 1: Catalog Uncertainty Reasons and Provider Limits - 2 hours

**Type:** Learning

**Outcome:** Artifact maps domain uncertainty reasons to refreshable provider evidence, rate limits, and failure responses.

**Verify:** Review examples from current uncertain seasons and confirm each reason has display-safe wording.

**Rollback:** Remove artifact; provider and domain behavior remain unchanged.

### Step 2: Add Admin-Only Uncertain Season Query - 2 hours

**Type:** Earning

**Outcome:** Bounded query returns uncertain seasons and sanitized reasons without triggering provider calls.

**Verify:** Test admin authorization, pagination, known reasons, and absence of provider credentials.

**Rollback:** Disable route; background metadata behavior is unaffected.

### Step 3: Add Uncertain Season View - 2 hours

**Type:** Earning

**Outcome:** Admin view lists uncertain seasons, reasons, and current evidence timestamp.

**Verify:** Render empty, populated, stale, and error fixtures on supported viewports.

**Rollback:** Remove navigation entry and disable view route.

### Phase 2: Expand Refresh Safely

### Step 4: Add Dry-Run Single-Show Refresh - 3 hours

**Type:** Earning

**Outcome:** Admin can fetch and compare fresh provider evidence without persisting or reclassifying season.

**Verify:** Use provider fixtures for changed, unchanged, rate-limited, and unavailable responses; inspect redacted logs.

**Rollback:** Disable dry-run endpoint; no persisted state requires reversal.

### Step 5: Persist Evidence Then Re-Evaluate Domain Policy - 3 hours

**Type:** Earning

**Outcome:** Feature-flagged action stores new evidence append-only, invokes existing policy, and audits before/after outcome.

**Verify:** Refresh one controlled show and confirm evidence lineage, policy result, and audit entry are atomic.

**Rollback:** Disable flag and restore prior active evidence pointer while retaining audit history.

### Phase 3: Controlled Rollout

### Step 6: Enable Refresh for Administrators - 1 hour

**Type:** Earning

**Outcome:** Refresh action becomes available with confirmation, in-progress guard, and provider rate-limit feedback.

**Verify:** Trigger one refresh, prevent concurrent duplicate action, and monitor provider errors and policy transitions.

**Rollback:** Disable feature flag; inspection remains available and background jobs continue.

## Stop Conditions

- Stop rollout on provider rate-limit growth, concurrent refreshes for same show, missing audits, or policy results inconsistent with stored evidence.

## Completion Checklist

- [x] Uncertain reasons and evidence timestamps are visible only to admins.
- [x] Refresh is single-show, rate-limit aware, and auditable.
- [x] Prior evidence remains recoverable after re-evaluation.

## Result

Completed on `2026-08-28`. `GET /api/admin/metadata/issues` already lists uncertain seasons with reasons. `POST /api/admin/shows/{providerId}/refresh` re-imports show from provider via `ImportShowDetailsCommand`, re-evaluates domain policy. Both endpoints require admin role. Existing audit trail through `JobExecution` records covers background refresh; manual refresh errors are sanitized in response.
