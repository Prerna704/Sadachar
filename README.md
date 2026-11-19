# AccessEd — Education for Every Learner

> **Inclusive Education Ecosystem for Specially-Abled Students**  
> Modern Web Programming project implementing accessibility tools, RPwD Act 2016 / WCAG 2.2 Level AA compliance, 72-Hour grievance SLAs, and DPDP Act 2023 privacy governance.

---

## 🚀 Quick Start

```bash
# 1. Build the solution
dotnet build Sadachar.slnx

# 2. Run the application
dotnet run --project src/Sadachar.Web/Sadachar.Web.csproj
```

- **Web Application**: Open [http://localhost:5024](http://localhost:5024) in your browser.
- **Swagger / OpenAPI Documentation**: Open [http://localhost:5024/swagger](http://localhost:5024/swagger).

---

## 👥 Seed Accounts

All accounts use the password: **`Access@123`**

| Role | Email | Persona | Key Features |
|---|---|---|---|
| **Student** | `student@accessed.local` | Aarav Sharma | Enrolled courses, TTS reader, accessibility settings, barrier reporting |
| **Teacher** | `teacher@accessed.local` | Dr. Meera Iyer | Course accessibility monitoring, multi-format upload checklist, student support queue |
| **Administrator** | `admin@accessed.local` | Rohan Kapoor | Campus-wide overview, compliance audits, issue tracking Kanban board, CSV reports |

*(Public registration permits Student and Teacher sign-ups; Administrator accounts are restricted).*

---

## 🖥️ 14 Implemented Screens (Razor Views)

1. **Landing Page** (`/`): Value proposition, stat counters, live feature preview with audio snippet.
2. **Sign In & Register** (`/Account/Login`): Role-based registration with optional accessibility needs and live preview toggles.
3. **Student Dashboard** (`/Dashboard`): Continue learning progress bar, reading streak, schedule notes, and profile toggles.
4. **Accessible Resources** (`/Resources`): Format filter pills (Audio, Large print, Braille-ready, Captioned, Easy-read) and search.
5. **Material Details** (`/Resources/Details/{id}`): Multi-format switcher, inline audio playback, and WCAG verification checklist.
6. **Accessibility Settings** (`/Accessibility`): Font size slider (100–160%), Dyslexia-friendly/Mono typefaces, line spacing (1.4/1.8/2.2), themes (Paper, Light, High-contrast), saved to database and synchronized site-wide.
7. **TTS Reader Mode** (`/Reader`): Web Speech API synthesizer with sentence-level highlighting, voice selection, speed/pitch controls, and keyboard shortcuts (`Space`: play/pause, `←`/`→`: sentence, `+`/`-`: speed).
8. **Support & Complaints Portal** (`/Support`): Barrier logging with confidential submission option under DPDP Act 2023.
9. **Teacher Dashboard** (`/Teacher`): Per-course accessibility scores, pre-flight upload checklist (alt-text, captions, headings, contrast), student accommodation queue.
10. **Admin Dashboard** (`/Admin`): Registered users, active courses, compliance trend line, and category breakdown.
11. **Compliance Monitoring** (`/Admin/Compliance`): Standard score cards (WCAG 2.2 AA, Institutional policy, Data protection) and department audit rankings.
12. **Issue Tracking Kanban** (`/Admin/Issues`): Three-column interactive board (`Open`, `In review`, `Resolved`) with 72-hour statutory SLA tracking.
13. **Reports & Exports** (`/Admin/Reports`): Audit trend analytics and one-click RFC 4180 CSV export (`/Admin/ExportCsv`).
14. **Profile & Settings** (`/Profile`): User details, accessibility needs tag, notification toggles, and consent settings.

---

## 🎨 Design System & Accessibility (WCAG 2.2 AA)

- **Palette**: Paper (`#F6EEDF`), Ink (`#2A2320`), Terracotta (`#C4452B`), Mustard (`#F0B429`), Forest (`#1F6F5C`), Plum (`#6B2D5C`), Sage (`#A9C7AE`), Blush (`#F4C7B3`).
- **Typography**: Lora (headings) + Poppins (body) + Lexend (dyslexia-friendly).
- **Neo-Brutalist Craft**: 2px ink borders, 4px hard offset shadows, pill chips.
- **Layouts**: `_Layout.cshtml` (public) and `_AppLayout.cshtml` (fixed 240px sidebar on desktop, bottom tab bar on mobile `< 640px`).
- **Accessibility**: Semantic HTML landmarks (`<header>`, `<nav>`, `<main>`, `<footer>`), visible 3px focus rings, accessible skip links, ARIA live announcer, high-contrast gold/black theme.

---

## 📡 RESTful Web API

- `GET /api/resources` & `GET /api/resources/{id}`: Resource catalog and format alternatives.
- `GET /api/profile` & `POST /api/profile/accessibility`: User profile and synchronized settings.
- `GET /api/tickets`, `POST /api/tickets`, `PATCH /api/tickets/{id}/status`: Issue tracking.
- `GET /api/compliance`: WCAG and departmental compliance scores.
- `GET /api/reports/summary` & `GET /api/reports/export`: Analytics metrics and CSV export.
