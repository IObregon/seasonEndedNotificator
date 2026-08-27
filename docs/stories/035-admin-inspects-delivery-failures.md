# Story 035: Administrator Inspects Delivery Failures

**As an** administrator, **I want** to inspect failed deliveries **so that** channel problems can be diagnosed.

## Acceptance Criteria

- Admin filters deliveries by date, channel, user, and status.
- View shows attempts and sanitized failure category.
- Secrets and message bodies are absent.
- Eligible permanent failure can be retried manually without duplication.

## Dependencies

- Story 027.

## Small Safe Steps

### Phase 1: Define Safe Diagnostics

### Step 1: Inventory Delivery Data and Redaction Risks - 2 hours

**Type:** Learning

**Outcome:** Field-level artifact identifies filterable data, failure categories, secrets, and message content that must remain hidden.

**Verify:** Review representative records from each channel and mark every returned field allowlisted or excluded.

**Rollback:** Delete artifact; application remains unchanged.

### Step 2: Add Sanitized Failure Query - 3 hours

**Type:** Earning

**Outcome:** Admin-only query filters by date, channel, user, and status using allowlisted fields and bounded pagination.

**Verify:** Test role denial, all filter combinations, pagination, and absence of secrets and bodies.

**Rollback:** Disable route without changing delivery processing.

### Phase 2: Expose Diagnosis

### Step 3: Add Filtered Failure List - 3 hours

**Type:** Earning

**Outcome:** Admin view presents sanitized deliveries and stable empty, loading, and error states.

**Verify:** Exercise each filter and confirm results match query fixtures on desktop and mobile widths.

**Rollback:** Remove navigation entry and disable view route.

### Step 4: Add Attempt Detail View - 2 hours

**Type:** Earning

**Outcome:** Admin can inspect attempt time, status, and normalized failure category without payload or credentials.

**Verify:** Snapshot success, transient, and permanent examples and scan rendered output for sensitive fixtures.

**Rollback:** Hide detail control; list remains usable.

### Phase 3: Add Guarded Retry

### Step 5: Implement Retry Eligibility Check - 2 hours

**Type:** Earning

**Outcome:** Read-only eligibility result explains whether one permanent failure can be retried without creating work.

**Verify:** Test already-successful, pending, duplicate, ineligible, and eligible delivery records.

**Rollback:** Disable eligibility endpoint and retry control.

### Step 6: Enqueue One Idempotent Manual Retry - 3 hours

**Type:** Earning

**Outcome:** Confirmed admin action enqueues one audited retry under existing delivery idempotency rules.

**Verify:** Submit action twice and confirm one retry job, one audit trail, and no immediate duplicate send.

**Rollback:** Disable retry feature flag and cancel unclaimed retry job.

## Stop Conditions

- Stop release if any secret, endpoint key, token, or message body reaches API, UI, logs, or audit metadata.
- Disable retry immediately if repeated action can enqueue duplicate delivery work.

## Completion Checklist

- [ ] Filters and attempt details are admin-only and sanitized.
- [ ] Retry eligibility is explicit and tested.
- [ ] Manual retry is audited and idempotent.
