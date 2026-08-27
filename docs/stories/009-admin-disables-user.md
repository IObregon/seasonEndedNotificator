# Story 009: Administrator Disables a User

**As an** administrator, **I want** to disable one account **so that** access and future notifications stop.

## Acceptance Criteria

- [x] Admin can disable an active non-self account.
- [x] Existing sessions cease working.
- [x] Disabled user cannot request usable magic links.
- [x] Notification eligibility excludes disabled user.

## Dependencies

- Story 007.

## Small Safe Steps

### Phase 1: Map Every Access Path

#### Inventory Active-User Enforcement Points - 2 hours

**Type:** Learning

**Outcome:** A checked-in threat matrix covers admin mutation, current sessions, magic links, and notification eligibility.

**Verify:** Each acceptance criterion and every authentication entry point maps to an owner and executable test.

**Rollback:** Remove the matrix; runtime behavior remains unchanged.

#### Add Disable State and Authorized Command - 3 hours

**Type:** Earning

**Outcome:** An admin can disable one active non-self user; self-disable, unauthorized access, and repeated disable are safely rejected.

**Verify:** Test all four paths through the application boundary and confirm only the target state changes.

**Rollback:** Disable the command endpoint and reactivate affected test accounts with the inverse data change.

### Phase 2: Enforce Disabled State

#### Reject Disabled Users on Session Validation - 2 hours

**Type:** Earning

**Outcome:** Existing cookies stop authorizing requests immediately after their user is disabled.

**Verify:** Authenticate, disable the account, then retry an authorized endpoint with the old cookie and expect rejection.

**Rollback:** Revert the active-state authorization check and reactivate the account if emergency access is required.

#### Block Magic-Link Authentication - 2 hours

**Type:** Earning

**Outcome:** Disabled users receive no usable new link, and links issued before disable cannot establish a session.

**Verify:** Test requests and consumption before and after disable; confirm uniform request responses remain intact.

**Rollback:** Revert this enforcement independently while keeping session validation and disable data unchanged.

#### Exclude Disabled Users from Notification Eligibility - 2 hours

**Type:** Earning

**Outcome:** Shared notification eligibility filtering excludes disabled accounts by default.

**Verify:** Query a mixed active/disabled fixture and assert only active recipients remain.

**Rollback:** Revert the filter and reactivate users if notification restoration is explicitly required.

## Stop Conditions

- Stop if an administrator can disable their own current account.
- Stop if any authentication path bypasses active-state validation.
- Stop if notification exclusion requires destructive deletion of user preferences.

## Completion Checklist

- [x] Admin can disable an active non-self account.
- [x] Existing sessions fail immediately.
- [x] New and previously issued magic links cannot authenticate.
- [x] Disabled users are ineligible for notifications.

## Result

Completed on `2026-08-27`. Role-protected admin endpoint disables active non-self users with explicit conflict outcomes. Cookie principal validation checks current database status on every request, invalidating existing sessions immediately. Magic-link request and consumption require active status, and `ActiveUserPolicy` provides shared notification-recipient filtering.
