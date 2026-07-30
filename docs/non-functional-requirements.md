# ClinicFlow — Non-Functional Requirements (NFRs)

| Category | Target | Reasoning |
|---|---|---|
| **Availability** | 95% uptime (demo-grade, not production SLA) | This is a resume/demo project on a free-tier host; a 99.9% SLA would be unrealistic and not worth defending in an interview. 95% is honest and still a stated, deliberate target. |
| **Performance** | API p95 response time < 500ms for reads, < 1s for writes | Reasonable for a free-tier App Service instance with cold starts; gives you a concrete number to discuss without over-promising. |
| **Scalability** | ~50 concurrent users, ~10 tenants, ~5k appointments/tenant/year | Sized for a convincing demo/interview walkthrough, not real production load. Talking point: architecture (shared-DB multi-tenancy) scales further without a rewrite. |
| **Security** | JWT auth with refresh tokens; role-based authorization (Admin/Doctor/Receptionist/Patient); tenant isolation enforced at the query layer (global query filter on TenantId) | Matches patterns from your CRN project (JWT + refresh) and Framsikt (multi-tenant isolation). |
| **Cost** | ~$0/month running cost — Azure App Service Free (F1) tier + Azure SQL free tier or LocalDB for local demo | You're between roles right now; this needs to cost nothing to keep running during interview season. |
| **Maintainability** | Modular Monolith with Vertical Slice Architecture; one `CLAUDE.md` per repo (backend/frontend) documenting conventions | Keeps a single codebase simple to reason about and easy to extend module-by-module — also a strong interview talking point on architectural judgment. |
| **Reliability** | Global exception handling middleware returning a consistent error shape (`ProblemDetails` or custom `Error` type); no unhandled 500s in demo flows | Directly addresses the "vibe-coded" failure mode — consistent error handling is one of the clearest signals of production-quality thinking. |
| **Testability** | Unit tests for business rules (booking conflicts, tenant isolation, authorization) + integration tests for key API endpoints using xUnit | Matches your existing xUnit experience from the CRN assessment; gives you real, demonstrable test coverage to show. |

## Notes
- These targets are intentionally modest where real production rigor isn't
  the point (availability, scale) and intentionally strict where they
  demonstrate engineering judgment (security, reliability, testability).
- If asked in an interview "why 95% uptime and not 99.9%?" — the honest
  answer is the right answer: this is a portfolio project on a free tier,
  and the architecture doesn't change based on the SLA number.
