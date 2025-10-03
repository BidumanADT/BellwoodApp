# Bellwood — Phase 2 Developer Guide (Dev README)

This README documents the working dev state used for the Phase‑2 passenger app demo. It describes how to run the **AuthServer**, **RidesAPI**, and the **.NET MAUI** mobile app; how to run the Postman smoke tests; and how to reproduce the quote module behavior (including round‑trip JSON).

> For a machine‑readable snapshot of this state, see the context lockpoint JSON: `Bellwood_Phase2_Context_Lock_20251003_141307.json` in the repo or artifact storage.

---

## 1) Local environment

### Ports & URLs
- **AuthServer**: `https://localhost:5001`  (`http://localhost:5000`)
- **RidesAPI**:   `https://localhost:5005`  (`http://localhost:5004`)
- **Android emulator** loopback: `10.0.2.2` (so the MAUI app uses `https://10.0.2.2:5001` and `https://10.0.2.2:5005`)
- **Dev SSL**: clients accept dev certs via `DangerousAcceptAnyServerCertificateValidator` (dev only).

### Seed users
- `alice / password`
- `bob / password`

### JWT (dev)
- Signing key: `Jwt:Key` (defaults to `"super-long-jwt-signing-secret-1234"` in dev)
- Issuer/Audience not validated; `ClockSkew = 0`.

---

## 2) Running the servers

From the solution root (or each project folder):

```bash
# Auth server (https://localhost:5001)
dotnet run --project AuthServer

# Rides API (https://localhost:5005)
dotnet run --project RidesApi
```

Health checks:
```bash
curl -k https://localhost:5001/health
curl -k https://localhost:5005/health
```

### Auth endpoints
- `POST /connect/token` — Password grant (`grant_type=password`, `client_id=bellwood-maui-dev`, `scope=api.rides offline_access`)
- `POST /connect/token` — Refresh (`grant_type=refresh_token`)
- `POST /api/auth/login` (alias: `/login`) — JSON login returns **all common shapes**: `accessToken`, `refreshToken`, `access_token`, `refresh_token`, `token`
- `GET /api/auth/me` (optional) — Returns claim summary when authorized
- `GET /health`, `GET /healthz`

### Rides endpoints
- `GET /api/rides` — list (auth required) → `RideListItem[]`
- `POST /api/rides` — create (auth required) → `RideListItem`
- `GET /api/rides/{id}` — by id (auth required)
- `GET /health`, `GET /healthz`

**RideListItem**
```json
{
  "id": "string",
  "pickupTime": "2025-10-02T12:00:00",
  "pickupAddress": "string",
  "dropoffAddress": "string",
  "status": "Completed|Created|...",
  "price": 86.50
}
```

---

## 3) Postman smoke tests

Files (exported):
- `Bellwood_Phase2_Smoke_Refresh.postman_collection.json`
- `Bellwood_Dev_Refresh.postman_environment.json`

Environment variables (typical):
```
scheme:    https
authHost:  localhost:5001
apiHost:   localhost:5005
client_id: bellwood-maui-dev
username:  alice
password:  password
token:     (populated by login)
token_expired: EXPIRED_OR_TAMPERED_JWT (fixture)
```

Run order:
1. **Auth Server Health** → 200 OK
2. **Rides API Health** → 200 OK
3. **Login — JSON** (or OAuth2 Password) → sets `token` and `refresh_token`
4. **Me (optional)** → shows JWT claims (requires token)
5. **Rides — List/Create/Get** → all require token
6. **Refresh Token** → rotates tokens (and updates env)

> Ensure “SSL certificate verification” is disabled in Postman for localhost dev if needed.

---

## 4) .NET MAUI mobile app

### DI & HTTP clients
- Pages: `LoginPage (Singleton)`, `MainPage (Transient)`, `RideHistoryPage (Transient)`, `QuotePage (Transient)`
- Services: `IAuthService -> AuthService`, `IRideService -> RideService`, `IQuoteService -> QuoteService`, `IProfileService -> ProfileService`
- `AuthHttpHandler` attaches `Authorization: Bearer {token}` from SecureStorage
- Clients:
  - `"auth"`: `https://10.0.2.2:5001` (Android), `https://localhost:5001` (desktop)
  - `"rides"`: `https://10.0.2.2:5005` (Android), `https://localhost:5005` (desktop)

### Routing
- **Shell visible**: `MainPage`
- **Registered routes**: `QuotePage`, `RideHistoryPage`
- After login: `GoToAsync("//MainPage")`
- Buttons: `GoToAsync(nameof(QuotePage))`, `GoToAsync(nameof(RideHistoryPage))`

### Quote module (demo)
- Booker (read-only) from profile stub
- Passenger picker: Booker/saved/new (+ optional phone/email default to booker)
- Additional passengers list (names)
- Vehicle: Sedan, SUV, Sprinter, S‑Class
- Pickup: date & time + location (saved/new)
- Dropoff: As Directed | saved | new
  - **As Directed** → Hours stepper (1–12), **Round Trip hidden**
  - **Fixed dropoff** → Round Trip toggle shows **Return (second ride)** with **Return date & time**
- Additional requests: Child Seats | Accessible Vehicle | Other (+ text)
- Flight info (optional): None | Commercial Flight (FlightNumber) | Private Tail Number (TailNumber)
  - Saved location includes **Signature FBO (ORD), 825 Patton Drive, Chicago, IL 60666**
- **Build Quote JSON**: Pretty JSON with `requestCount` and `requests[]`
  - For **round trip (point‑to‑point)** → **two objects**: outbound and return (pickup/dropoff swapped)
  - `ReturnPickupTime` is set on the **outbound** object when round trip
  - Copy and Save buttons available

**QuoteDraft (shape)**
```json
{
  "booker": { "firstName": "", "lastName": "", "phoneNumber": "", "emailAddress": "" },
  "passenger": { "firstName": "", "lastName": "", "phoneNumber": "", "emailAddress": "" },
  "additionalPassengers": ["Chris Park","Sam Irving"],
  "vehicleClass": "SUV",
  "pickupDateTime": "2025-10-03T10:00:00",
  "pickupLocation": "Signature FBO (ORD) - 825 Patton Drive, Chicago, IL 60666",
  "asDirected": false,
  "hours": null,
  "dropoffLocation": "Langham - 330 N Wabash Ave, Chicago, IL",
  "roundTrip": true,
  "returnPickupTime": "2025-10-05T09:30:00",
  "additionalRequest": "Other",
  "additionalRequestOtherText": "Bottle Water",
  "flightType": "Private",
  "flightNumber": null,
  "tailNumber": "N123AB"
}
```

---

## 5) Troubleshooting

- **404s on health or login**: confirm ports; ensure both projects are running; check for double host concatenation in clients.
- **Android sees old token**: Clear app storage (Settings → Apps → App → Storage → Clear storage) or log out from the menu if present.
- **401s on Rides API**: confirm the MAUI app successfully parsed a non‑empty token; AuthServer JSON login returns multiple casings.
- **XAML invalid char errors**: avoid non‑ASCII bullets/em‑dashes in XAML; replace with ASCII characters.
- **HTTPS cert warnings**: ok in dev; production will need cert pinning/validation.

---

## 6) Git tips (used during demo prep)

- Update feature branch from main:
  ```bash
  git fetch origin
  git switch feature/quote-module
  git merge --no-ff origin/main
  ```
- Start fresh from main (reset approach):
  ```bash
  git fetch origin
  git switch feature/quote-module || git checkout -b feature/quote-module
  git reset --hard origin/main
  git push --force-with-lease
  ```
- Recover specific files from an old commit:
  ```bash
  git restore --source=<commit> --worktree --staged path/to/File.cs
  git commit -m "Restore file from <commit>"
  ```
- Stash & reflog can rescue local, unpushed work:
  ```bash
  git stash list
  git reflog
  ```

---

## 7) Demo script (quick)

1. Start **AuthServer** and **RidesAPI** (`/health` both 200).
2. Launch the MAUI app; login as `alice/password` → lands on **MainPage**.
3. Tap **Get a Quote**:
   - Passenger: **Booker (you)** or select **Jordan Chen**.
   - Pickup: set date/time; choose **Signature FBO (ORD)**.
   - Dropoff: choose **Langham**.
   - Toggle **Round Trip** → set Return date/time (Sun at 09:30).
   - Flight Info: **Private Tail Number** → `N123AB`.
   - Additional Request: **Other** → “Bottle Water”.
   - **Build Quote JSON** → show 2 requests (outbound + return). Copy/Save if desired.
4. Back → **Ride History** to show demo data cards.
5. Optional: hit Postman **Refresh Token** and **Rides List** to show end‑to‑end auth.

---

## 8) Files of interest

- Context lockpoint: `Bellwood_Phase2_Context_Lock_20251003_141307.json`
- Postman: `Bellwood_Phase2_Smoke_Refresh.postman_collection.json`, `Bellwood_Dev_Refresh.postman_environment.json`
- Mobile:
  - `AppShell.xaml` (+ `.cs`) — routes
  - `MauiProgram.cs` — DI + HttpClients
  - `Services/AuthHttpHandler.cs` — attaches Bearer
  - `Services/ProfileService.cs` — demo data (includes Signature FBO)
  - `Pages/QuotePage.xaml` (+ `.cs`) — quote flow
  - `Pages/RideHistoryPage.xaml` (+ `.cs`) — history cards

---

## 9) Next steps (post‑demo)

- Persist passengers/locations server‑side; hydrate pickers from API
- Address autocomplete (LA/Maps) + validation
- Quote pricing endpoint: `/api/quotes/estimate` (fare + ETA)
- Token auto‑refresh in `AuthHttpHandler` on 401
- Wire JSON submission to **Admin Portal** + **reservations@**

Happy shipping! 🚕
