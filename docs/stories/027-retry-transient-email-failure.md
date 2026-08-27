# Story 027: Retry a Transient Email Failure

**As a** user, **I want** temporary email-provider failures retried **so that** brief outages do not lose my digest.

## Acceptance Criteria

- Timeout, rate limit, and server errors schedule bounded exponential retry.
- Permanent rejection is not automatically retried.
- Attempts and sanitized errors are recorded.
- Successful retry cannot send duplicate digest afterward.

## Dependencies

- Story 025.

## Small Safe Steps

### Phase 1: Learn and Expand Attempt State

### Step 1: Classify provider outcomes and retry limits - 2 hours

**Type:** Learning

**Outcome:** Decision table maps timeout, rate limit, server error, permanent rejection, and success to bounded retry behavior.

**Verify:** Review provider documentation and encode representative outcomes as executable tests.

**Rollback:** Remove decision artifact; current one-attempt behavior remains.

### Step 2: Add attempt and next-attempt persistence - 3 hours

**Type:** Earning

**Outcome:** Additive fields or records store attempt number, sanitized error, next-attempt time, and terminal state.

**Verify:** Migration and repository tests cover success, retryable failure, permanent failure, and existing delivery rows.

**Rollback:** Stop writing new attempt state; retain additive data for audit.

### Phase 2: Build Retry Path Disabled

### Step 3: Implement bounded exponential retry planning - 2 hours

**Type:** Earning

**Outcome:** Pure policy computes capped delay and terminal outcome from classification and attempt count.

**Verify:** Deterministic tests cover each error class, maximum attempts, delay cap, and optional provider retry-after value.

**Rollback:** Remove policy; no worker uses it yet.

### Step 4: Record provider outcome atomically - 3 hours

**Type:** Earning

**Outcome:** Send completion atomically records success or schedules one next attempt while preserving digest identity.

**Verify:** Integration tests simulate crash/re-entry and prove one due attempt plus no retry after success or permanent rejection.

**Rollback:** Keep retry worker disabled and revert outcome recording to existing failure state.

### Step 5: Add retry worker disabled by default - 2 hours

**Type:** Earning

**Outcome:** Feature-flagged worker claims due retries and reuses idempotent digest sending.

**Verify:** Clock and concurrency tests prove flag-off inactivity, one claimant, bounded attempts, and no duplicate after success.

**Rollback:** Disable worker flag; queued attempts remain available for inspection or manual handling.

### Phase 3: Migrate and Observe

### Step 6: Enable retries for controlled staging failures - 1 hour

**Type:** Learning

**Outcome:** Staging evidence shows timeout/server error retry, permanent rejection termination, sanitized diagnostics, and eventual success.

**Verify:** Inject each provider response and inspect attempt timing, count, final status, and received message count.

**Rollback:** Disable worker and clear only isolated staging retry fixtures.

### Step 7: Enable production retries with bounded kill switch - 1 hour

**Type:** Earning

**Outcome:** Production transient failures retry automatically within configured attempt and delay limits.

**Verify:** Monitor first retry cohort for attempt counts, terminal states, provider call rate, and duplicate digest count.

**Rollback:** Disable worker; existing scheduled attempts stop without losing audit history.

## Stop Conditions

- Stop if permanent failures retry, provider call rate exceeds limits, errors expose sensitive data, or one digest sends twice.
- Do not enable production until crash/re-entry and concurrent-claim tests pass.

## Completion Checklist

- [ ] Error classification and retry bounds are executable and documented.
- [ ] Attempts and sanitized errors are persisted atomically.
- [ ] Successful or permanent outcomes cannot be retried.
