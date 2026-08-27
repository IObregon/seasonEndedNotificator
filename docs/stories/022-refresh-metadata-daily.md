# Story 022: Refresh Followed-Show Metadata Daily

**As a** user, **I want** followed shows refreshed daily **so that** schedule changes are discovered without admin work.

## Acceptance Criteria

- Hosted job refreshes followed airing shows at configured UTC time.
- Persisted lease prevents concurrent duplicate run.
- TVmaze rate limits and transient failures are respected.
- Job execution and last success are recorded.

## Dependencies

- Stories 014 and 021.

## Small Safe Steps

### Phase 1: Learn and Isolate

### Step 1: Specify refresh scheduling and lease behavior - 2 hours

**Type:** Learning

**Outcome:** Decision note defines UTC scheduling, eligible followed airing shows, lease ownership/expiry, TVmaze throttling, and restart behavior.

**Verify:** Review examples for overlapping runs, expired leases, rate limits, and partial provider failure against acceptance criteria.

**Rollback:** Delete the note; runtime behavior is unchanged.

### Step 2: Extract a manually invoked refresh operation - 3 hours

**Type:** Earning

**Outcome:** Existing metadata refresh logic is callable as one operation without changing current callers or adding automation.

**Verify:** Automated tests prove eligible shows refresh and existing metadata behavior remains unchanged.

**Rollback:** Revert the extraction and restore direct calls to existing logic.

### Phase 2: Expand Automation Safely

### Step 3: Add persisted lease and execution status records - 3 hours

**Type:** Earning

**Outcome:** Additive persistence supports atomic lease acquisition plus started, completed, and failed execution status.

**Verify:** Integration tests prove one concurrent owner, lease expiry recovery, and recorded last success.

**Rollback:** Stop writing new records; additive storage can remain unused.

### Step 4: Add hosted scheduler disabled by default - 2 hours

**Type:** Earning

**Outcome:** Configured UTC scheduler invokes the refresh operation behind a disabled-by-default feature flag.

**Verify:** Clock-controlled tests prove due-time invocation, no invocation while disabled, and no overlap without a lease.

**Rollback:** Disable the flag; manual refresh remains available.

### Step 5: Add bounded TVmaze pacing and failure reporting - 2 hours

**Type:** Earning

**Outcome:** Automated runs respect provider limits, preserve completed updates, and record sanitized transient failures.

**Verify:** Adapter tests simulate throttling and transient errors and assert bounded requests plus accurate final status.

**Rollback:** Keep scheduler disabled and revert pacing wrapper without changing manual behavior.

### Phase 3: Migrate and Observe

### Step 6: Enable one scheduled run in staging - 1 hour

**Type:** Learning

**Outcome:** Staging evidence records schedule timing, lease ownership, provider request rate, and refreshed show count.

**Verify:** Observe one due run and one forced overlap; only one execution refreshes metadata.

**Rollback:** Disable the scheduler flag and release or expire the staging lease.

### Step 7: Enable production schedule with kill switch - 1 hour

**Type:** Earning

**Outcome:** Daily production refresh runs at configured UTC time with status and last-success visibility.

**Verify:** Confirm first run status, refreshed records, provider error rate, and absence of duplicate execution.

**Rollback:** Disable the scheduler flag; retain manual refresh and execution records.

## Stop Conditions

- Stop rollout if lease ownership overlaps, provider limits are exceeded, or refresh failures can corrupt previously valid metadata.
- Do not enable production until staging completes one due run and overlap test successfully.

## Completion Checklist

- [ ] Scheduler remains configurable and has a tested kill switch.
- [ ] Lease, provider pacing, execution status, and last success are verified.
- [ ] First production run completes without duplicate refresh.
