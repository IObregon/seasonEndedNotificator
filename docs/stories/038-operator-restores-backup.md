# Story 038: Operator Restores a Database Backup

**As an** operator, **I want** a tested restore procedure **so that** VPS loss does not destroy service data.

## Acceptance Criteria

- Nightly encrypted backup is copied off VPS.
- Procedure restores into clean PostgreSQL instance.
- Restored application passes readiness and record-count checks.
- Retention and latest successful backup are documented and monitored.

## Dependencies

- Story 002.

## Small Safe Steps

### Phase 1: Establish Backup Contract

### Step 1: Inventory Data, Recovery Targets, and Secrets - 2 hours

**Type:** Learning

**Outcome:** Recovery artifact defines included PostgreSQL data, exclusions, RPO, RTO, encryption owner, and clean-instance prerequisites.

**Verify:** Review every persistent volume and database against deployment configuration.

**Rollback:** Remove artifact; production remains unchanged.

### Step 2: Create Encrypted Local Backup Command - 3 hours

**Type:** Earning

**Outcome:** Least-privilege command creates timestamped encrypted logical backup without interrupting database writes.

**Verify:** Run against disposable populated database and validate checksum plus encrypted format without exposing plaintext.

**Rollback:** Remove command and securely delete test artifact and temporary credentials.

### Step 3: Copy Backup Off VPS - 2 hours

**Type:** Earning

**Outcome:** Backup is uploaded to isolated storage with retention metadata while local database remains source of truth.

**Verify:** Download by immutable identifier, compare checksum, and confirm VPS compromise credentials cannot delete retained copy.

**Rollback:** Disable upload credential and delete test object under storage retention policy.

### Phase 2: Prove Restore Without Cutover

### Step 4: Restore Into Clean Isolated PostgreSQL - 3 hours

**Type:** Earning

**Outcome:** Documented command decrypts and restores one backup into a disposable, network-isolated database.

**Verify:** Restore from empty instance and confirm schema version, ownership, and expected record counts.

**Rollback:** Destroy disposable database; production database is never targeted.

### Step 5: Validate Application Against Restored Database - 2 hours

**Type:** Learning

**Outcome:** Restore report records readiness, migrations, critical queries, and record-count comparisons using temporary application instance.

**Verify:** Start app with restored database, keep outbound delivery disabled, and run readiness plus sampled domain checks.

**Rollback:** Stop temporary app and destroy isolated restore environment.

### Phase 3: Automate and Rehearse Cutover

### Step 6: Schedule Nightly Backup and Monitoring - 3 hours

**Type:** Earning

**Outcome:** Scheduler creates encrypted offsite backup, applies retention, and publishes latest-success age and failure alert.

**Verify:** Trigger one scheduled run, inspect immutable object, retention tags, metric, and alert test.

**Rollback:** Disable schedule and revoke backup credential; retain last known-good backups.

### Step 7: Rehearse Reversible Database Cutover - 3 hours

**Type:** Learning

**Outcome:** Runbook proves maintenance-mode cutover to restored instance and rollback to original instance before any new writes.

**Verify:** Execute rehearsal in staging, pass readiness and counts, then switch back and record timings.

**Rollback:** Restore original connection target, exit maintenance mode, and destroy rehearsal instance.

## Stop Conditions

- Never target production with restore command unless maintenance mode is active, backup identity is verified, and explicit operator approval exists.
- Stop cutover on checksum mismatch, schema mismatch, failed readiness, unexpected record counts, outbound delivery activity, or writes to either database.

## Completion Checklist

- [ ] Nightly encrypted backup exists off VPS with tested retention and alerting.
- [ ] Clean-instance restore and application validation are reproducible.
- [ ] Cutover and rollback timings satisfy documented recovery targets.
