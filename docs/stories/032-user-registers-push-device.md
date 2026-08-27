# Story 032: User Registers One Push Device

**As a** user, **I want** to enable push on my current browser **so that** it can receive season digests.

## Acceptance Criteria

- Permission is requested only after explicit action.
- VAPID subscription is stored for authenticated user.
- Re-registering same endpoint is idempotent.
- User can revoke current device.

## Dependencies

- Stories 007 and 031.

## Small Safe Steps

### Phase 1: Define Safe Subscription Contract

### Step 1: Record Browser and VAPID Constraints - 1 hour

**Type:** Learning

**Outcome:** Decision note defines permission states, supported browsers, endpoint identity, and secret-handling boundaries.

**Verify:** Review note against service-worker and push-provider documentation using one test subscription.

**Rollback:** Remove decision note and test subscription; application remains unchanged.

### Step 2: Add Push Subscription Storage - 3 hours

**Type:** Earning

**Outcome:** Additive persistence stores endpoint, keys, user, and revocation state without changing existing notification paths.

**Verify:** Apply migration to empty and populated test databases, then insert and read a subscription.

**Rollback:** Stop writes, remove additive table in rollback migration, and leave existing schemas untouched.

### Phase 2: Expand Registration

### Step 3: Add Idempotent Authenticated Registration API - 3 hours

**Type:** Earning

**Outcome:** Authenticated endpoint upserts same browser endpoint for current user without duplicate rows.

**Verify:** Submit identical subscription twice and confirm one active record with no exposed key material in logs.

**Rollback:** Disable route and retain unused additive table for later removal.

### Step 4: Add User-Initiated Permission Flow - 2 hours

**Type:** Earning

**Outcome:** Push opt-in control requests permission only after explicit user action and registers successful subscription.

**Verify:** Test default, granted, and denied states; confirm page load never opens permission prompt.

**Rollback:** Hide opt-in control with configuration and disable registration route.

### Phase 3: Revoke and Roll Out

### Step 5: Add Current-Device Revocation - 2 hours

**Type:** Earning

**Outcome:** User can deactivate server record and unsubscribe current browser independently.

**Verify:** Revoke test device, confirm inactive record, and confirm repeated revoke succeeds safely.

**Rollback:** Hide revoke control; registration remains valid and records are not deleted.

### Step 6: Enable Registration for a Small Cohort - 1 hour

**Type:** Earning

**Outcome:** Feature flag exposes registration to controlled users while existing channels remain primary.

**Verify:** Monitor registration errors, duplicate endpoints, and permission prompts for cohort.

**Rollback:** Disable feature flag; stored subscriptions remain dormant and removable later.

## Stop Conditions

- Stop rollout if subscription secrets appear in logs, duplicate endpoints are created, or permission is requested without user action.
- Do not remove additive storage until registration route has been disabled and records are confirmed unused.

## Completion Checklist

- [ ] Registration and revocation are authenticated and idempotent.
- [ ] Permission prompt follows explicit user action.
- [ ] Feature flag rollback has been exercised.
