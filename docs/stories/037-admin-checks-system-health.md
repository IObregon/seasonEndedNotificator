# Story 037: Administrator Checks System Health

**As an** administrator, **I want** job and adapter status in one view **so that** silent notification failures are visible.

## Acceptance Criteria

- View shows last refresh, confirmation, digest, and retry status.
- Database and external adapter checks show sanitized state.
- Outbox backlog and oldest retry are visible.
- Detailed view requires admin role.

## Dependencies

- Stories 026, 029, and 033.

## Small Safe Steps

### Phase 1: Define Health Signals

### Step 1: Specify Health Semantics and Thresholds - 2 hours

**Type:** Learning

**Outcome:** Decision table defines healthy, stale, degraded, and unavailable states for jobs, database, adapters, outbox, and retries.

**Verify:** Evaluate table against recent operational examples and identify owner for each threshold.

**Rollback:** Remove decision artifact; runtime remains unchanged.

### Step 2: Add Sanitized Health Snapshot Service - 3 hours

**Type:** Earning

**Outcome:** Read-only service composes last job runs, bounded dependency checks, backlog size, and oldest retry without secrets.

**Verify:** Test healthy, stale, timeout, and unavailable fixtures; assert checks cannot mutate dependencies.

**Rollback:** Disable service route; jobs and adapters continue unchanged.

### Phase 2: Expose Operational Status

### Step 3: Add Admin-Only Health Summary - 2 hours

**Type:** Earning

**Outcome:** Admin endpoint returns coarse statuses and timestamps with strict timeout and role enforcement.

**Verify:** Confirm unauthorized denial, timeout behavior, bounded response time, and sanitized payload.

**Rollback:** Disable endpoint without affecting public readiness checks.

### Step 4: Build Health Dashboard Summary - 3 hours

**Type:** Earning

**Outcome:** Single view shows refresh, confirmation, digest, retry, database, adapter, and outbox states.

**Verify:** Render every state from fixtures and confirm stale data displays timestamp instead of false health.

**Rollback:** Remove admin navigation entry and dashboard route.

### Phase 3: Improve Diagnosis

### Step 5: Add Backlog and Oldest-Retry Detail - 2 hours

**Type:** Earning

**Outcome:** Dashboard exposes bounded backlog counts and oldest retry age without message bodies.

**Verify:** Compare values with seeded outbox records and scan response for payload content.

**Rollback:** Hide detail panel while retaining summary states.

### Step 6: Validate Failure Visibility - 2 hours

**Type:** Learning

**Outcome:** Runbook evidence demonstrates each silent-failure scenario becomes degraded or unavailable within defined interval.

**Verify:** Simulate stale job, adapter timeout, database failure, and retry backlog in non-production environment.

**Rollback:** Remove fault injections and discard artifact; no production configuration changes.

## Stop Conditions

- Stop release if health checks mutate dependencies, expose secrets, or make dashboard latency depend on unbounded provider waits.

## Completion Checklist

- [ ] Health semantics and thresholds are documented.
- [ ] Dashboard is admin-only, bounded, and sanitized.
- [ ] Job, adapter, database, outbox, and retry failures are visibly distinguishable.
