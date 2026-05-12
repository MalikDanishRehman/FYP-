# HydroAI / AI-Driven Water Supply — Project Overview (Recruiter Brief)

**One-line summary:** A full-stack **marketplace web application** that connects **consumers** with **water suppliers** (bottles, RO plants, tankers), with **role-based experiences** for consumers, providers, and administrators, plus **AI-assisted** discovery, support, and trust-and-safety features backed by **Supabase (Postgres + Auth)**.

This document highlights **what the product does** and **what engineering skills the codebase demonstrates**, so reviewers can quickly map the work to hiring needs.

---

## Problem domain

Urban and residential users need reliable access to **bottled water, tanker delivery, and RO-style supply**. The app centralizes **supplier discovery**, **ordering**, **order lifecycle** (including status updates and billing hooks), **messaging**, **reviews**, and **operational tooling** (attendance for workers, admin oversight). The **HydroAI** assistant helps users find providers and escalate issues.

---

## Technical stack (what candidates built with)

| Layer | Technologies |
|--------|----------------|
| **Frontend / host** | **ASP.NET Core 8**, **Blazor** (Interactive **Server** rendering), Razor components, Bootstrap 5, Chart.js, Leaflet maps, PWA assets (`manifest.json`, service worker) |
| **Architecture** | **Clean-style layering**: `Presentation` → `Application` (interfaces + DTOs) → `Infrastructure` (Supabase, HTTP clients) → `Domain` (entities mapped to database tables) |
| **Backend-as-a-service** | **Supabase** (.NET client): authentication, Postgres data access (Postgrest models), realtime options enabled in client configuration |
| **AI / automation** | **Google Gemini**: (1) **FastAPI** Python service for conversational **function-calling** (tool use), (2) **.NET** service calling Gemini for **review moderation** and admin-alert style workflows |
| **Cross-cutting** | `HttpClientFactory` (named clients for external APIs, timeouts), `IConfiguration` + **DotNetEnv** (`.env`), structured logging where used, antiforgery, HTTPS |

---

## Engineering highlights (why this matters on a résumé)

- **Multi-project solution** with explicit **dependency direction** (UI depends on abstractions, not on raw Supabase calls everywhere).
- **Server-side Blazor** with interactive components: stateful UI without shipping a full SPA framework, while still integrating **JavaScript** (maps, fingerprint helper, cookie bridge).
- **Hybrid AI architecture**: a **dedicated Python microservice** for the chat agent (long timeout, tool orchestration) and **in-process .NET** moderation for user-generated content—shows judgment about **where** to place AI workloads.
- **Security-minded auth**: Supabase session bridged to **HttpOnly**, **SameSite Strict** cookies via a minimal **API controller** (`/api/auth/set-cookie`, `/api/auth/remove-cookie`), reducing token exposure to client-side scripts compared to localStorage-only patterns.
- **Role-based access**: admin capabilities gated by **profile role** checks against the database (not only “is logged in”).
- **Real product surface area**: search/sort/pagination patterns, dashboards, dispute and user management, audits, preferences—not a single CRUD demo.

---

## Major features by audience

### Consumers and public discovery

- **Landing and onboarding** (`/`, `/get-started`): role selection (buy vs sell), service-type framing for the water vertical.
- **Consumer hub** (`/Consumer`): marketing-style hero, **supplier search** with debouncing, filters, **sorting**, **pagination / load more**, tabs (e.g. all / active / top-rated), multi-select style flows where implemented, and **quick order** entry points by product type (bottle / plant / tanker).
- **Supplier listing by order type** (`/order/{type}`): route-driven flows aligned to **bottle**, **plant**, **tanker** paths.
- **Supplier profile** (`/supplier/{name}`): provider detail, **reviews**, quantity and pricing UX, **order submission** persisted to **`orders`** in Supabase, navigation to **order-scoped chat** after placement where applicable.
- **Bills** (`/bills`): consumer-facing billing experience (paired with domain `Bill` entity and provider-side bill views).
- **HydroAI helper** (`/Helper_Agent`): authenticated **conversational UI** (text + optional image), calls the **FastAPI** backend; UI parses structured assistant output into **clickable supplier cards** and deep links into the app.
- **PWA-oriented assets**: web app manifest and service worker registration for installability / offline-adjacent behavior (scope depends on service worker implementation).

### Providers (suppliers)

- **Provider dashboard** (`/ProviderDashboard`): operational view of the business in the app.
- **Profile management** (`/profilepro`): provider profile and presentation to buyers.
- **Supplier bills** (`/supplier-bills`): financial / billing side for the supplier role.
- **Reviews** (`/provider-reviews`): reputation management for providers.
- **Attendance** (`/attendance`): workforce / shift-style tracking using **`AttendanceLog`** / **`Worker`** domain concepts—shows operational depth beyond a pure marketplace listing.

### Orders, status, and collaboration

- **Order status workflow** (via `IOrderStatusService`): supplier-driven status updates; on key transitions (e.g. accepted), **bill creation** and **system messaging** to the consumer are described in the service contract—coordinates **orders**, **bills**, and **messages**.
- **Per-order chat** (`/chat/{OrderId}`): messaging tied to a specific order for coordination between parties.

### Administrators

- **Admin home / dashboard** (`/AdminPage`): **metrics** and **recent orders** with paging and filters (backed by `IAdminDashboardService` and DTOs such as `AdminDashboardMetricsDto`, `AdminOrderRowDto`).
- **User management** (`/UserManagement`): administrative control over user-related records and access patterns in the product.
- **Vendor management** (`/VendorManagement`): oversight of supplier-side accounts and related data.
- **Disputes** (`/AdminDispute`): dispute rows and resolution-oriented admin UI (`AdminDisputeRowDto`, dispute services).
- **Settings** (`/AdminSetting`): admin configuration including preferences (`IAdminPreferencesService`).
- **Audit trail** (`IAdminAuditService`): admin actions logged for accountability (`AdminAuditLog` entity).
- **Profile moderation** (`IAdminProfileModerationService`): admin workflows around profile content quality or policy.
- **Self-profile for admins** (`IAdminSelfProfileService`): admins maintain their own admin profile within the system.

### Trust, safety, and quality

- **AI-assisted review moderation** (`IReviewModerationService` / `ReviewModerationService`): before reviews are accepted, content is evaluated with **Gemini**; decisions include accept vs reject paths (e.g. abuse, **star-rating vs text sentiment mismatch**), with hooks to raise **`AdminAlert`** records for follow-up—demonstrates **responsible AI** thinking in UGC-heavy products.
- **SQL scripts** under `AI_Driven_Water_Supply.Presentation/Scripts/`: operational and policy-oriented database artifacts (e.g. admin panel support, **ratings anti-manipulation**, admin alerts)—shows collaboration with **DBA / security** style concerns, not only application code.

### Platform and developer experience

- **Environment-driven configuration**: Supabase URL/keys from `.env`; AI agent base URL from environment or `appsettings.json` (`AiAgent:BaseUrl`), defaulting to local FastAPI.
- **One-command local orchestration**: `Run_All.bat` starts the Python AI backend and the Blazor app (documented behavior in-repo) for demos and interviews.

---

## Data model (high level)

Domain entities map to Supabase/Postgres tables, including among others: **`profiles`** (roles, ratings, location, services, account status), **`orders`**, **`bills`**, **`messages`**, **`disputes`**, **`workers`**, **`attendance_logs`**, **`admin_preferences`**, **`admin_audit_logs`**, **`admin_alerts`**. This supports a **multi-sided marketplace** with **operational** and **governance** data, not only listings and carts.

---

## Skills map (recruiter checklist)

Use the following as a **keyword map** from this project to typical role requirements.

- **.NET / C#**: ASP.NET Core hosting, Blazor components, async/await, DI, options/configuration.
- **Web**: Razor, CSS component scopes, minimal APIs/controllers for cross-boundary concerns (cookies).
- **Cloud BaaS**: Supabase Auth + Postgres integration, Postgrest-annotated models.
- **Python**: FastAPI service, Gemini tool/function calling, SMTP integrations for escalations.
- **AI/ML product engineering**: prompt/tool orchestration (Python), moderation pipeline (.NET + Gemini), structured UI rendering of model output.
- **Security & privacy**: HttpOnly cookies, SameSite, HTTPS/HSTS in production template, antiforgery.
- **UX**: dashboards, maps, charts, responsive layouts, loading states, toast notifications (`IToastService`).

---

## Honest scope notes (professional credibility)

- Some interfaces or stubs may exist for future work (for example, a **water status** style service interface appears without being wired in the main host `Program.cs` at the time of this document). The **core** product value is in **Supabase-backed flows**, **administration**, **orders/chat/bills**, and **AI surfaces** described above.

---

## Repository layout (where to look in an interview)

| Path | Purpose |
|------|---------|
| `AI_Driven_Water_Supply.Presentation/` | Blazor UI, `Program.cs`, `Controllers/`, `wwwroot/`, SQL scripts |
| `AI_Driven_Water_Supply.Application/` | Contracts and DTOs shared by UI and infrastructure |
| `AI_Driven_Water_Supply.Infrastructure/` | Supabase-backed service implementations |
| `AI_Driven_Water_Supply.Domain/` | Table-mapped entities |
| `HydroAI.Backend/` | FastAPI + Gemini **HydroAI** API consumed by the Blazor helper page |

---

## Suggested talking points for candidates

1. **Why Blazor Server here:** simplified deployment and secure access to server resources, while still using JS for specialized widgets (maps, fingerprint, cookie helper).
2. **Why a Python sidecar for chat:** isolates long-running LLM + tool loops, keeps the .NET app responsive, and allows independent scaling or swapping of the model service.
3. **Why Gemini in .NET for reviews:** low-latency path on the same request as review submission, centralized logging and policy, and direct writes to Supabase for alerts—**different coupling** than the chat agent, intentionally.

---

*This file is maintained as a **recruiter- and reviewer-facing** summary of the repository’s intent and scope. For run instructions, see `Run_All.bat`, `Start_Backend.bat`, and environment variables referenced in `Program.cs` and `HydroAI.Backend/main.py`.*
