# Story 025: User Receives a Manual Email Digest

**As a** user, **I want** confirmed followed-season completions emailed together **so that** I know which seasons ended.

## Acceptance Criteria

- Admin-triggered run selects eligible events after follow timestamp.
- One email contains title, season, date, and internal link for each item.
- User language controls template.
- Persisted unique keys prevent duplicate digest and items.

## Dependencies

- Stories 008, 014, 017, and 023.

## Small Safe Steps

### Phase 1: Learn Eligibility and Expand Persistence

### Step 1: Specify digest eligibility and grouping examples - 2 hours

**Type:** Learning

**Outcome:** Executable examples cover follow timestamp, confirmed completion, email preference, grouping, locale, and repeated runs.

**Verify:** Review examples against acceptance criteria using boundary dates and multiple users.

**Rollback:** Remove examples; runtime behavior is unchanged.

### Step 2: Add digest and item uniqueness records - 3 hours

**Type:** Earning

**Outcome:** Additive schema records digest identity and item identity with database-enforced unique keys.

**Verify:** Migration tests apply/rollback schema and concurrent insert tests prove duplicate keys fail safely.

**Rollback:** Roll back additive migration before any production records exist, or stop writes and retain populated tables.

### Phase 2: Build Without Sending

### Step 3: Implement eligible-item selection - 3 hours

**Type:** Earning

**Outcome:** Query returns only confirmed followed-season completions after each user's follow timestamp and with email enabled.

**Verify:** Integration tests cover timestamp boundary, unrelated shows, disabled email, and already-recorded items.

**Rollback:** Remove the new query; no sending path depends on it yet.

### Step 4: Build localized grouped email content - 2 hours

**Type:** Earning

**Outcome:** Selected items produce one localized message containing title, season, date, and internal link per item.

**Verify:** Approved HTML/plain-text outputs cover English, Spanish, one item, and multiple items.

**Rollback:** Remove the builder and retain selection/persistence artifacts.

### Step 5: Persist digest atomically before provider send - 3 hours

**Type:** Earning

**Outcome:** Manual run reserves one digest and its items atomically, returning existing identity on duplicate processing.

**Verify:** Transaction and concurrency tests prove restart/repetition cannot reserve duplicate digest or items.

**Rollback:** Disable manual orchestration and stop new writes; additive records remain auditable.

### Phase 3: Migrate to Controlled Sending

### Step 6: Add admin manual-send action disabled by default - 2 hours

**Type:** Earning

**Outcome:** Authorized admin can invoke reserved digest sending behind a disabled-by-default feature flag.

**Verify:** Tests prove authorization, flag-off no-op, one provider call, localized content, and duplicate invocation safety.

**Rollback:** Disable the flag; selection and reserved records remain inspectable.

### Step 7: Enable and verify one staging manual digest - 1 hour

**Type:** Learning

**Outcome:** Staging evidence demonstrates correct recipient, grouped content, persistence, and idempotent repetition.

**Verify:** Invoke twice for fixed data and confirm one message, one digest record, and unique item records.

**Rollback:** Disable the flag and remove isolated staging fixtures.

## Stop Conditions

- Stop if repeated or concurrent invocation can call the provider twice for one digest identity.
- Do not enable manual sending until unique constraints and transaction behavior pass integration tests.

## Completion Checklist

- [ ] Eligibility and localization cover all required fields and boundaries.
- [ ] Unique keys prevent duplicate digest and items.
- [ ] Authorized manual run sends one verified staging digest.
