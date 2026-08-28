# Story 033: User Receives a Web Push Digest

**As a** subscribed user, **I want** one push digest **so that** ended seasons reach my device.

## Acceptance Criteria

- Existing digest eligibility and language rules are reused.
- Push opens relevant internal page.
- Delivery result is recorded idempotently.
- `404` or `410` revokes expired endpoint.

## Dependencies

- Stories 025 and 032.

## Small Safe Steps

### Phase 1: Learn and Expand

### Step 1: Characterize Digest Eligibility and Push Responses - 2 hours

**Type:** Learning

**Outcome:** Test matrix maps existing eligibility and language rules to success, transient failure, `404`, and `410` push responses.

**Verify:** Run existing digest examples through matrix and review expected endpoint state transitions.

**Rollback:** Remove artifact; delivery behavior remains unchanged.

### Step 2: Add Push Delivery Records Alongside Existing Channels - 3 hours

**Type:** Earning

**Outcome:** Additive channel records support one idempotency key per digest and endpoint without routing live traffic.

**Verify:** Apply migration on populated test data and prove repeated inserts resolve to one delivery record.

**Rollback:** Disable push record creation and remove additive schema only after confirming no callers.

### Step 3: Add Push Provider Adapter in Dry-Run Mode - 3 hours

**Type:** Earning

**Outcome:** Provider adapter builds localized payload and records intended send without contacting provider.

**Verify:** Compare dry-run recipients and language against existing digest output; inspect sanitized logs.

**Rollback:** Disable adapter configuration; existing delivery channels continue unchanged.

### Phase 2: Migrate Gradually

### Step 4: Send to One Controlled Endpoint - 2 hours

**Type:** Earning

**Outcome:** Feature-flagged canary sends one real digest with internal deep link while existing channels remain active.

**Verify:** Confirm one device notification, correct language and route, and one successful delivery record.

**Rollback:** Disable canary flag and retain existing channels as fallback.

### Step 5: Handle Provider Outcomes Idempotently - 3 hours

**Type:** Earning

**Outcome:** Success and retryable failures update delivery attempts; `404` and `410` deactivate only affected endpoint.

**Verify:** Replay fixture responses and confirm duplicate callbacks do not duplicate attempts or revoke other devices.

**Rollback:** Route provider outcomes to observation-only mode and restore endpoint state from audit record.

### Step 6: Expand Push Cohort - 1 hour

**Type:** Earning

**Outcome:** Push delivery expands through configurable cohort while established channels and retry path remain available.

**Verify:** Compare eligible, attempted, successful, and revoked counts before each cohort increase.

**Rollback:** Set cohort to zero; no provider or schema removal occurs during rollback.

### Phase 3: Confirm Stable Operation

### Step 7: Audit Delivery Parity - 2 hours

**Type:** Learning

**Outcome:** Production evidence shows push recipients, language, deep links, and idempotency match digest policy.

**Verify:** Reconcile sampled digests against eligibility and delivery records with message bodies excluded.

**Rollback:** Retain old channel policy and reduce push cohort if any mismatch appears.

## Stop Conditions

- Stop cohort growth on recipient mismatch, duplicate sends, secret leakage, unexpected revocation, or elevated transient failures.
- Keep existing channels available until push parity is demonstrated over an agreed observation window.

## Completion Checklist

- [x] Push reuses digest eligibility and language policy.
- [x] Provider outcomes and expired endpoints are handled idempotently.
- [x] Canary rollback to zero traffic is proven.

## Result

Completed on `2026-08-28`. `PushDigestMessages.Create` builds localized JSON payload with title, body, and URL. `SendDigestCommand` handles Push channel: sends via `IPushSender`, auto-revokes subscriptions on 404/410, records `LastSuccessAt`. `PrepareDigestCommand` includes Push channel for users with active subscriptions. Unique (UserId, Channel, DigestDate) prevents duplicate deliveries.
