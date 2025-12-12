# SmartWaste Platform

SmartWaste is a role-aware waste management solution that now runs on a **.NET 9 backend** exposed through `App.WebAPI` and a **React (Vite + TypeScript) frontend** in `smartwaste-react`. The domain libraries (`App.Core`, `App.BLL.EF`, `App.BLL.SP`, `App.Factory`) remain unchanged and are reused by both the Web API and any legacy frontends.

## Architecture at a Glance

- **Database**: SQL Server schema, functions, triggers, and procedures defined in `sql` and the comprehensive seed script `../group28_p2.sql`.
- **Domain Layer**: `App.Core` (models and contracts) plus `App.BLL.EF` and `App.BLL.SP` (Entity Framework vs. stored procedure services). `App.Factory` selects the strategy at runtime.
- **Backend API**: `App.WebAPI` (ASP.NET Core) exposes controllers for authentication and citizen workflows, with CORS configured for the React client.
- **Frontend**: `smartwaste-react` (Vite + React + TypeScript) delivers role dashboards in the browser. Operator and admin experiences will be implemented here.
- **Legacy Client**: `App.UI` (Avalonia) remains for reference but is no longer the primary interface.

## Project Structure

```
SmartWaste/
├── App.Core/              # Shared models, DTOs, contracts
├── App.BLL.EF/           # Entity Framework services
├── App.BLL.SP/           # Stored procedure services
├── App.Factory/          # ServiceFactory switches EF/SP
├── App.WebAPI/           # ASP.NET Core Web API (primary backend)
│   ├── Controllers/      # Auth + Citizen endpoints (Operator coming soon)
│   └── Program.cs        # Hosting, DI, CORS
├── smartwaste-react/     # React frontend (Vite, TypeScript)
│   ├── src/pages/        # Login, Citizen, Operator, Admin views
│   ├── src/components/   # Shared layout/components
│   └── src/styles/       # Global theme and layout CSS
├── App.UI/               # Legacy Avalonia client (reference only)
├── sql/                  # Baseline SQL scripts
└── README_REACT.md       # Migration notes used during the switch
```

## Database Setup

1. Provision SQL Server locally (native install or Docker) or connect to an existing instance.
2. Execute the comprehensive schema and seed script `../group28_p2.sql` to deploy roles, waste listings, stored procedures, views, and sample data. This script supersedes older fragments in `sql/`.
3. Update the connection string in `App.WebAPI/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "SmartWasteDB": "Server=localhost;Database=SmartWasteDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
     }
   }
   ```
4. Choose execution mode (Entity Framework vs. stored procedures) via `AppSettings.UseEntityFramework` in the same file.

## Running the Stack

Start the API in one terminal:
```bash
cd App.WebAPI
dotnet restore
dotnet run
```

Start the React client in another terminal:
```bash
cd smartwaste-react
npm install
npm run dev
```

The API listens on `http://localhost:5000`; the React dev server runs on `http://localhost:5173`. Keep both shells open during development.

### Test Credentials

- Citizen: `35201-0000001-0` / `password1`
- Operator: `42000-0300001-0` / `oppass1`

## Backend API Surface

Current controllers in `App.WebAPI`:

- **AuthController** (`api/auth/login`): Authenticates by CNIC/password and returns role metadata. The optional `X-Use-EF` header overrides EF/SP selection.
- **CitizenController** (`api/citizen/*`): Profile, listings, transactions, categories, pricing, and registration endpoints that use the shared services through `ServiceFactory`.

Planned additions:

- **OperatorController**: exposes pickup queues, collection completion, route assignments, performance stats, and complaint handling aligned with the SQL assets listed below.

## Frontend Modules (`smartwaste-react/src`)

- `App.tsx`: Handles routing between login and role dashboards.
- `pages/LoginPage.tsx`: CNIC/password login with role selection cards.
- `pages/CitizenPortalPage.tsx`: Sample citizen dashboard (awaits real API wiring).
- `pages/OperatorPortalPage.tsx`: Layout scaffold for operator workflow; functionality to be implemented.
- `components/AppLayout.tsx`: Shared chrome with logout button.
- `styles/global.css`: Cream/forest-green theme, responsive cards, and error states.

## Operator Portal Backlog

`../group28_p2.sql` documents the backend expectations for operator capabilities. Key database objects to integrate when implementing `OperatorController` (API) and wiring up `OperatorPortalPage.tsx` (UI):

- **Views**
  - `WasteManagement.vw_OperatorCollectionPoints`: pending listings, citizen contact details, and verification codes per operator route.
  - `WasteManagement.vw_OperatorPerformance`: totals for pickups, collected weight, and amounts per operator.
  - `WasteManagement.vw_ActiveComplaints`: open complaints, route metadata, and assignment status.
- **Stored Procedures**
  - `WasteManagement.sp_OperatorPerformance`: KPI output (collections, complaints, qualitative rating).
  - `WasteManagement.sp_AssignOperatorToRoute`: enforces one-operator-per-route and persists warehouse assignments.
  - `WasteManagement.sp_CreateOperator` and `WasteManagement.sp_DeactivateOperator`: manage operator lifecycle and availability.
- **Tables and Triggers**
  - `WasteManagement.Collection` with trigger `trg_Collection_UpdateStatus`: inserting a collection automatically marks the related listing as collected.
  - `WasteManagement.WarehouseStock` with trigger `trg_WarehouseStock_UpdateInventory`: keeps warehouse utilisation in sync after drop-offs.
  - `WasteManagement.TransactionRecord`: links collections to payouts and verification codes.

Recommended feature slices for the operator portal:

1. **Dashboard summary**: fetch KPI cards via `sp_OperatorPerformance` or `vw_OperatorPerformance`.
2. **Pickup queue**: list route-specific pending listings from `vw_OperatorCollectionPoints` with filtering and search.
3. **Confirm collection**: POST to a new API endpoint that inserts into `Collection` (and optionally updates `WarehouseStock`), letting triggers update listing status.
4. **Route and warehouse info**: display assignments from the `Operator`, `Route`, and `Warehouse` tables, updating via `sp_AssignOperatorToRoute` when needed.
5. **Complaint follow-up**: surface unresolved complaints filtered by operator from `vw_ActiveComplaints` with status update actions.

## Development Tips

- **Switch EF vs. Stored Procedures**: toggle `AppSettings.UseEntityFramework` and, if needed, pass `X-Use-EF` from the React client for per-request overrides.
- **CORS**: `App.WebAPI` allows `http://localhost:3000` and `http://localhost:5173`; add production origins before deployment.
- **Building for production**:
  ```bash
  cd smartwaste-react
  npm run build
  ```
  Vite outputs to `smartwaste-react/dist`. Serve it via any static host or configure ASP.NET to serve the bundle.
- **Testing**: run `dotnet test` once operator services are added; use Jest/React Testing Library for frontend logic as features grow.

## Legacy Avalonia Client

`App.UI` and its view models remain for reference but are no longer the default UI. Avoid modifying these files unless maintaining the desktop client is explicitly required.

---

For historical migration notes, consult `README_REACT.md`. For detailed database behaviour, refer to `../group28_p2.sql` and the scripts under `sql/`.
