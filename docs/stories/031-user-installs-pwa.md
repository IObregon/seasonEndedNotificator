# Story 031: User Installs the PWA

**As a** user, **I want** to install the website **so that** it behaves like an app on my device.

## Acceptance Criteria

- Valid manifest, icons, and service worker pass installability checks.
- Installed app opens correct start route.
- Offline launch shows application shell and network status.
- Update-available flow avoids stale UI indefinitely.

## Dependencies

- Story 002.

## Small Safe Steps

### Phase 1: Establish Installability

### Step 1: Capture PWA Baseline - 1 hour

**Type:** Learning

**Outcome:** Saved installability report covering current manifest, icons, start route, and service-worker state.

**Verify:** Run browser installability tooling on desktop and mobile profiles and record each finding.

**Rollback:** Remove report artifact; runtime remains unchanged.

### Step 2: Add Manifest and Icons - 2 hours

**Type:** Earning

**Outcome:** Deployable manifest and required icons make app installable without caching behavior changes.

**Verify:** Validate manifest, icon sizes, scope, and start URL in browser tooling.

**Rollback:** Revert manifest link and static assets.

### Phase 2: Expand Offline Support

### Step 3: Register Pass-Through Service Worker - 2 hours

**Type:** Earning

**Outcome:** Versioned service worker registers but leaves all requests on network, creating safe rollout point.

**Verify:** Confirm registration, activation, normal navigation, and zero cached responses in production-like build.

**Rollback:** Unregister worker and deploy prior bundle; no cache migration is required.

### Step 4: Cache Versioned Application Shell - 3 hours

**Type:** Earning

**Outcome:** Service worker precaches only immutable shell assets under new cache version while network remains preferred for data.

**Verify:** Load once, disconnect network, and confirm shell plus offline status render without stale API data.

**Rollback:** Deploy pass-through worker that deletes new cache version during activation.

### Phase 3: Roll Out Updates Safely

### Step 5: Add Explicit Update Prompt - 2 hours

**Type:** Earning

**Outcome:** Waiting worker triggers update-available UI and activates only after user confirmation.

**Verify:** Serve two worker versions and confirm prompt, controlled reload, and retained navigation route.

**Rollback:** Remove prompt and restore previous worker activation policy.

### Step 6: Validate Installed Launches - 2 hours

**Type:** Learning

**Outcome:** Recorded desktop and mobile-profile evidence for install, online launch, offline launch, and update flow.

**Verify:** Execute installability checklist against production build with a clean browser profile.

**Rollback:** Discard validation artifact; deployed behavior remains unchanged.

## Stop Conditions

- Stop rollout if worker controls pages outside manifest scope, caches authenticated API responses, or cannot be removed by pass-through worker.
- Stop activation if installed app loses current route or remains on old assets after confirmed update.

## Completion Checklist

- [ ] Installability checks pass with production assets.
- [ ] Offline launch exposes shell and network status without stale user data.
- [ ] New worker can roll forward and back from one prior version.
