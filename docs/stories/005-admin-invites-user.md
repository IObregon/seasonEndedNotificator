# Story 005: Administrator Invites a User

**As an** administrator, **I want** to invite one email address **so that** a person can join the private group.

## Acceptance Criteria

- Admin submits email with default `User` role.
- Single-use expiring invitation is persisted as a hash.
- Local invitation email is visible in Mailpit.
- Duplicate active invitation returns clear result.

## Dependencies

- Stories 003 and 004.

## Small Safe Steps

### Phase 1: Persist Invitations Safely

#### Define Invitation Security Scenarios - 2 hours

**Type:** Learning

**Outcome:** Executable scenarios fix token lifetime, hashing, duplicate-active behavior, email normalization, and default `User` role.

**Verify:** Each acceptance criterion maps to a scenario, including proof that plaintext tokens are never persisted.

**Rollback:** Remove the scenarios; no runtime or data changes occur.

#### Add Invitation Persistence - 3 hours

**Type:** Earning

**Outcome:** An additive migration stores normalized email, token hash, expiry, status, inviter, and default role.

**Verify:** Apply and roll back the migration on a disposable database; repository tests persist hashes but never raw tokens.

**Rollback:** Roll back the migration before invitations are issued.

### Phase 2: Issue One Invitation

#### Add Authorized Invitation Command - 3 hours

**Type:** Earning

**Outcome:** An admin can create one invitation, while non-admins are rejected and duplicate active invitations return a stable result.

**Verify:** Test admin, non-admin, new email, and normalized duplicate cases through the application boundary.

**Rollback:** Disable or revert the command endpoint; persisted unused invitations remain harmless and expiring.

#### Send Local Invitation Email - 2 hours

**Type:** Earning

**Outcome:** Successful creation sends a link containing the one-time raw token through `IEmailSender` to Mailpit.

**Verify:** Invite one address, inspect the Mailpit recipient and link, and confirm database/log searches contain only the hash.

**Rollback:** Revert email dispatch while retaining invitation creation for controlled testing.

## Stop Conditions

- Stop if raw invitation tokens appear in database records or logs.
- Stop if duplicate submissions create multiple active invitations.
- Stop if authorization cannot be proven at the application boundary.

## Completion Checklist

- [ ] Admin creates invitation with default `User` role.
- [ ] Only token hash is persisted.
- [ ] Duplicate-active result is clear and stable.
- [ ] Mailpit captures the invitation email.
