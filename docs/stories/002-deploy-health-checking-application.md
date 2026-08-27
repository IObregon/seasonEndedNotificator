# Story 002: Deploy a Health-Checking Application

**As an** operator, **I want** a minimal application running through Docker Compose **so that** deployment assumptions are tested early.

## Acceptance Criteria

- Vue shell and ASP.NET API build in CI.
- PostgreSQL starts and API can connect.
- Caddy serves application over HTTPS on configured domain.
- `/health/live` and `/health/ready` report expected state.

## Dependencies

None.

## Small Safe Steps

### Phase 1: Prove Local Components

#### Record Build and Runtime Assumptions - 1 hour

**Type:** Learning

**Outcome:** A checked-in note identifies required SDKs, ports, domain settings, health semantics, and Compose services.

**Verify:** Another developer can follow the note and confirm every prerequisite without guessing.

**Rollback:** Remove the note; no runtime behavior changes.

#### Add Minimal Vue and API Health Surfaces - 3 hours

**Type:** Earning

**Outcome:** Vue shell loads and ASP.NET exposes `/health/live` without external dependencies.

**Verify:** Build both projects and assert the live endpoint returns success locally.

**Rollback:** Revert the shell and endpoint commit; no persisted state is involved.

#### Add PostgreSQL Readiness Check - 2 hours

**Type:** Earning

**Outcome:** `/health/ready` reports success only when API can query PostgreSQL.

**Verify:** Start PostgreSQL for a success response, then stop it and confirm readiness fails while liveness remains healthy.

**Rollback:** Remove the database health registration and restore the prior readiness response.

### Phase 2: Package and Expose

#### Package Services with Docker Compose - 3 hours

**Type:** Earning

**Outcome:** Compose starts Vue, API, and PostgreSQL with explicit configuration and health checks.

**Verify:** Build from a clean checkout, start Compose, and confirm all containers become healthy.

**Rollback:** Stop Compose and revert its files; locally built applications remain usable.

#### Add Caddy HTTPS Routing - 2 hours

**Type:** Earning

**Outcome:** Caddy routes the configured HTTPS domain to Vue and API while preserving direct internal health checks.

**Verify:** Resolve the test domain, validate its certificate, load the shell, and call both health endpoints through Caddy.

**Rollback:** Remove Caddy from Compose and restore direct development ports.

### Phase 3: Gate Deployment

#### Add CI Build and Health Smoke Test - 3 hours

**Type:** Earning

**Outcome:** CI builds both applications and fails when the Compose stack cannot reach expected health states.

**Verify:** Run the pipeline successfully, then temporarily use a bad readiness path and confirm the smoke job fails.

**Rollback:** Revert the CI job while retaining locally verified images and Compose configuration.

## Stop Conditions

- Stop deployment if readiness can pass without a working PostgreSQL query.
- Stop HTTPS rollout if certificate issuance requires undocumented manual state.
- Stop and split work if a clean Compose build exceeds the three-hour packaging step.

## Completion Checklist

- [ ] Vue and API build locally and in CI.
- [ ] PostgreSQL affects readiness but not liveness.
- [ ] Configured domain serves valid HTTPS.
- [ ] Clean Compose startup passes smoke checks.
