# Story 039: CI Deploys a Verified Release

**As an** operator, **I want** CI to deploy a tested versioned image **so that** releases are repeatable and reversible.

## Acceptance Criteria

- CI builds backend and frontend and runs automated tests.
- Versioned image is scanned and pushed to registry.
- Restricted deployment account applies reviewed migrations and restarts Compose.
- Readiness is checked and previous image remains available for rollback.

## Dependencies

- Story 002.

## Small Safe Steps

### Phase 1: Verify Build Artifacts

### Step 1: Map Current Release and Rollback Path - 2 hours

**Type:** Learning

**Outcome:** Release artifact documents build inputs, migration order, deployment identity, readiness gate, and current rollback commands.

**Verify:** Walk through artifact against existing Compose deployment and one previous version.

**Rollback:** Remove artifact; deployment remains unchanged.

### Step 2: Run Backend and Frontend Checks in CI - 3 hours

**Type:** Earning

**Outcome:** Required CI job builds both applications and blocks release when automated tests fail.

**Verify:** Run passing pipeline and intentionally failing test branch; confirm no deploy job starts on failure.

**Rollback:** Remove new required check and return to previous manual verification process.

### Step 3: Build and Scan Immutable Versioned Image - 3 hours

**Type:** Earning

**Outcome:** CI produces commit-addressed image, scans it, and retains scan report without deploying.

**Verify:** Confirm image digest maps to commit and policy-blocking vulnerability prevents publish promotion.

**Rollback:** Delete candidate tag and disable image job; existing image remains active.

### Phase 2: Expand Deployment Automation

### Step 4: Push Candidate and Record Release Metadata - 2 hours

**Type:** Earning

**Outcome:** Passing image is pushed under immutable version and release metadata records digest plus previous digest.

**Verify:** Pull by digest, inspect labels, and confirm previous image remains available in registry.

**Rollback:** Remove candidate tag while preserving immutable prior release.

### Step 5: Add Restricted Staging Deployment - 3 hours

**Type:** Earning

**Outcome:** Least-privilege account deploys reviewed image and additive migrations to staging, then waits for readiness.

**Verify:** Audit account permissions, deploy candidate, and prove it cannot access unrelated host paths or registry writes.

**Rollback:** Redeploy previous digest; additive migration remains backward-compatible.

### Phase 3: Migrate Production Safely

### Step 6: Gate Production Deployment and Automatic Rollback - 3 hours

**Type:** Earning

**Outcome:** Approved production job applies backward-compatible migration, starts candidate, checks readiness, and restores previous digest on failure.

**Verify:** Rehearse successful deployment and forced readiness failure with automatic rollback in staging.

**Rollback:** Disable production job and use documented previous-digest Compose deployment.

### Step 7: Deploy One Production Release and Observe - 2 hours

**Type:** Earning

**Outcome:** One approved versioned image reaches production with release record, readiness evidence, and previous image retained.

**Verify:** Confirm image digest, migration status, readiness, critical smoke checks, and error rates during observation window.

**Rollback:** Redeploy previous digest; if migration is additive, leave schema expanded until later contract release.

### Phase 4: Contract Only After Stability

### Step 8: Document Contract Migration Gate - 1 hour

**Type:** Learning

**Outcome:** Checklist requires old application version zero usage and stable expanded schema before destructive migration may be separately released.

**Verify:** Review checklist against rollback window and prove current pipeline cannot bundle destructive migration with first code switch.

**Rollback:** Retain expanded schema and defer contract migration indefinitely.

## Stop Conditions

- Stop deployment on failed tests, scan policy failure, unreviewed migration, digest mismatch, failed readiness, or unavailable previous image.
- Do not run destructive contract migrations until previous version has zero usage beyond rollback window and backup restore is proven.

## Completion Checklist

- [ ] CI gates builds, tests, scan, publish, approval, deployment, and readiness in order.
- [ ] Deployment account is restricted and audited.
- [ ] Previous image rollback and additive-schema compatibility are rehearsed.
