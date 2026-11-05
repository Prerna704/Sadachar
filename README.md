# Sadachar (सदाचार) — Education Without Barriers.

<p align="center">
  <img src="src/SakshamGovernance.Web/wwwroot/images/logo.jpg" alt="Sadachar: Inclusive Education Ecosystem - Education Without Barriers" width="460" />
</p>

> **Inclusive Education Ecosystem for Specially-Abled Students**  
> *Education Without Barriers. Automating Compliance (RPwD Act 2016, WCAG 2.2), Enforcing Governance (72-Hour Grievance SLAs), and Monitoring Safe Digital Conduct (DPDP Act 2023).*

---

## 🏛️ Project Overview (Smart India Hackathon)

Educational institutions frequently struggle with three systemic governance gaps regarding Persons with Disabilities (PwD / Divyangjan):
1. **Compliance Blindspots**: Learning materials and institutional portals are uploaded without validating against **WCAG 2.2 Level AA** or India's **RPwD Act 2016** (Sections 40 & 42).
2. **Administrative Inertia**: Grievances filed by specially-abled students regarding physical or digital barriers often languish without accountability.
3. **Privacy & Safe Conduct Risks**: Inappropriate ableist conduct or harassment goes unmonitored, or monitoring systems violate privacy norms under India's **Digital Personal Data Protection (DPDP) Act 2023**.

**Sadachar** solves all three with an integrated .NET Clean Architecture ecosystem, turning the mission of **"Education Without Barriers."** into an enforceable, transparent reality.

---

## 🏗️ Architecture & Tech Stack

```text
SakshamGovernance/
├── SakshamGovernance.slnx
└── src/
    ├── SakshamGovernance.Core/            # Domain Entities, Enums, Interfaces
    │   ├── Entities/                      # ApplicationUser, ComplianceLog, GrievanceTicket, ConductLog
    │   ├── Enums/                         # UserRole, DisabilityType, TicketStatus, IncidentType
    │   └── Interfaces/                    # IComplianceRepository, IGrievanceRepository, ISlaEscalationService, ...
    ├── SakshamGovernance.Infrastructure/  # EF Core, SQL Server, Repositories, Background Daemon
    │   ├── Data/                          # ApplicationDbContext
    │   ├── Repositories/                  # ComplianceRepository, GrievanceRepository, ConductRepository
    │   └── Services/                      # SlaEscalationService, SlaEscalationBackgroundService
    └── SakshamGovernance.Web/             # ASP.NET Core Web API + Accessible Frontend
        ├── Controllers/                   # ComplianceController, GovernanceController, ConductController
        ├── DTOs/                          # Strictly-typed API Request & Response Contracts
        └── wwwroot/                       # Accessible Web Portal (High Contrast, Font Resizing, WCAG 2.2 AAA)
```

- **Backend**: C# 14, .NET 10 / ASP.NET Core MVC and Web API, Entity Framework Core 10.
- **Database**: Microsoft SQL Server / LocalDB.
- **Security & Identity**: ASP.NET Core Identity with Role-Based Access Control (RBAC).
- **Background Automation**: Autonomous `BackgroundService` with `PeriodicTimer` for statutory 72-Hour SLA audits.
- **Accessible UI**: Razor views for MVC pages and a static portal built with semantic HTML5, Bootstrap 5.3, WCAG 2.2 keyboard navigation, high-contrast mode, and dynamic text scaling.

---

## 📜 Regulatory Alignment

### 1. RPwD Act 2016 & WCAG 2.2 Level AA
- Automatically computes weighted scores across:
  - Color Contrast (25%)
  - Alt-Text / Captions (25%)
  - Keyboard Navigability (25%)
  - ARIA Screen Reader Compatibility (25%)
- Scores $\ge 80.00\%$ receive institutional certification; scores below trigger remediation flags.

### 2. Statutory 72-Hour Grievance SLA
- Every grievance logged initializes `SLAExpiryDateTime = CreatedAt.AddHours(72)`.
- The autonomous background daemon (`SlaEscalationBackgroundService`) runs every 15 minutes.
- If $DateTime.UtcNow > SLAExpiryDateTime$ and status is `Open` or `InProgress`, the ticket transitions immediately to `Escalated` for Ombudsman intervention.

### 3. DPDP Act 2023 (Digital Personal Data Protection)
- **Pseudonymization (Section 4)**: Actor IDs are hashed using SHA-256 with an institutional salt before persistence.
- **Data Minimization (Section 8)**: Text snippets are sanitized to scrub PII (phone numbers, email addresses).
- **Storage Limitation**: Records carry a strict retention timestamp (`DataRetentionExpiryUtc`), purgeable via automated job.

---

## 🚀 Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (.NET 10)
- SQL Server (or LocalDB installed with Visual Studio / VS Code)

### Run the Application

```bash
# 1. Restore & Build Solution
dotnet build SakshamGovernance.slnx

# 2. Run the Web Application
dotnet run --project src/SakshamGovernance.Web/SakshamGovernance.Web.csproj
```

Once running:
- **Interactive Accessible Portal**: Open `https://localhost:5001` or `http://localhost:5000` in your browser.
- **Swagger OpenAPI Documentation**: Open `https://localhost:5001/swagger`.
- **ASP.NET Core MVC / Razor example**: Open `https://localhost:5001/Home/Index` or `http://localhost:5024/Home/Index`.

The existing API controllers remain available under `/api`. The configured default is an in-memory database for easy demos; set `UseInMemoryDatabase` to `false` and configure `ConnectionStrings:DefaultConnection` to use SQL Server.

---

## 📡 Core API Endpoints

### Compliance (`/api/compliance`)
- `POST /api/compliance/evaluate`: Submit accessibility scores for educational materials.
- `GET /api/compliance/material/{materialId}`: Audit history for specific course material.
- `GET /api/compliance/all`: Institutional compliance logs.

### Governance & SLA (`/api/governance`)
- `POST /api/governance/grievance`: File a grievance and activate 72-hour SLA clock.
- `GET /api/governance/grievance/{ticketId}`: Retrieve grievance status with real-time breach check.
- `GET /api/governance/grievances`: View all student grievances.
- `POST /api/governance/grievance/trigger-sla-check`: Manual trigger for the SLA escalation engine.

### Conduct & Privacy (`/api/conduct`)
- `POST /api/conduct/log-incident`: Anonymized incident logging with DPDP compliance.
- `GET /api/conduct/incidents`: Audit flagged harassment/ableist language incidents.
- `POST /api/conduct/purge-expired`: Enforce storage limitation and delete expired records.
