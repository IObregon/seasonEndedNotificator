# Story 026: User Receives the Scheduled Email Digest

**As a** user, **I want** email digest generated at `09:00 UTC` **so that** no administrator must trigger it.

## Acceptance Criteria

- Hosted job runs at configured `09:00 UTC` schedule.
- Job lease prevents duplicate concurrent execution.
- Restart and repeated execution do not duplicate delivery.
- Job status is recorded.

## Dependencies

- Stories 022 and 025.

## Small Safe Steps

### Phase 1: Learn and Isolate

### Step 1: Define scheduler restart and overlap scenarios - 1 hour

**Type:** Learning

**Outcome:** Executable scenarios define configured UTC timing, missed runs, restarts, overlap, and repeated execution.

**Verify:** Review scenarios against manual digest idempotency and lease behavior from dependencies.

**Rollback:** Remove scenarios; runtime behavior is unchanged.

### Step 2: Expose one idempotent scheduled-run operation - 2 hours

**Type:** Earning

**Outcome:** Scheduler-facing operation invokes existing manual digest workflow without changing manual trigger behavior.

**Verify:** Integration tests prove identical eligibility and duplicate prevention for manual and scheduled invocation.

**Rollback:** Remove operation and retain manual digest execution.

### Phase 2: Expand Disabled Automation

### Step 3: Add digest-job lease and status recording - 3 hours

**Type:** Earning

**Outcome:** Persisted lease prevents concurrent ownership and status records started, completed, failed, and last success.

**Verify:** Concurrent integration tests prove one owner and accurate status after success, failure, and expired lease recovery.

**Rollback:** Stop acquiring/writing job records; scheduled flag remains disabled.

### Step 4: Add configured UTC hosted scheduler disabled by default - 2 hours

**Type:** Earning

**Outcome:** Hosted job invokes scheduled-run operation at configured `09:00 UTC` only when feature flag is enabled.

**Verify:** Fake-clock tests cover due time, non-due time, disabled state, restart, and repeated ticks.

**Rollback:** Disable flag; manual digest remains operational.

### Phase 3: Migrate and Observe

### Step 5: Enable scheduler for one staging cycle - 1 hour

**Type:** Learning

**Outcome:** Staging artifact records due-time execution, lease, delivery identity, and job status across a restart test.

**Verify:** Restart near due time and confirm exactly one delivery plus completed status.

**Rollback:** Disable flag and expire staging lease without deleting audit records.

### Step 6: Enable production scheduler with kill switch - 1 hour

**Type:** Earning

**Outcome:** Production digest runs automatically at configured UTC schedule with observable status.

**Verify:** Confirm first due run, one lease owner, expected delivery count, and no duplicate identities.

**Rollback:** Disable scheduler flag and use existing manual trigger if needed.

## Stop Conditions

- Stop rollout on overlapping lease ownership, duplicate delivery identity, or unexplained missed-run behavior.
- Do not enable production before staging restart test passes.

## Completion Checklist

- [x] Configured UTC schedule and disabled state are tested.
- [x] Lease and status survive restart and repeated execution.
- [x] First production run creates no duplicate delivery.

## Result

Completed on `2026-08-28`. `DailyDigestJob` acquires a persisted lease, prepares digests, sends each delivery, and records job execution status. `DigestHostedService` runs at configured `DigestSchedule:HourUtc` (default 09:00 UTC) only when `DigestSchedule:Enabled` is true, checking last completed date to run at most once per day. `DigestSchedule.IsDue` covers due, not-due, disabled, and already-ran-today scenarios. Lease prevents concurrent owners and recovers after expiry.
