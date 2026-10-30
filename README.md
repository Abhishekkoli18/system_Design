# Ecommerce App — Setup & Run Guide

Folder structure:
```
EcommerceApp/
├── backend/EcommerceApi/   ← ASP.NET Core Web API
└── frontend/               ← React (Vite)
```

> **Before you start:** after unzipping, make sure VS Code opens the folder that **directly contains** `backend`, `frontend`, and this `README.md` — not a parent folder wrapping it. If your file explorer shows something like `EcommerceApp (1)/EcommerceApp/backend`, open the *inner* `EcommerceApp` folder specifically (File → Open Folder → navigate into it → Open). Otherwise every `cd backend/...` command below will fail with a "path not found" error since your terminal starts one level too high.

---

## 0. One-time software installs (only needed once on your machine)

You said you only have VS Code — that's fine, you don't need Visual Studio (the big IDE), just these:

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
   Check it installed: open a terminal and run `dotnet --version` → should print something like `8.0.xxx`

2. **Node.js (LTS)** — https://nodejs.org
   Check it installed: `node --version` and `npm --version`

3. **SQL Server** — this project is configured for **SQL Server Express** with a named instance called `SQLEXPRESS` (this is what you get from the standalone "SQL Server Express" installer from Microsoft — different from LocalDB, which is a lighter-weight option bundled with Visual Studio).
   Check it's installed and running: open **Services** (Windows search → "Services") and confirm **SQL Server (SQLEXPRESS)** is listed and running. Or open SQL Server Management Studio (SSMS) and connect to `localhost\SQLEXPRESS` using Windows Authentication.

   *(If you don't want to install SQL Server at all, tell me and I'll swap the project to SQLite instead — zero install, single file database. Good enough for an evaluation.)*

4. **EF Core CLI tools** (lets you run `dotnet ef` commands):
   ```
   dotnet tool install --global dotnet-ef
   ```

5. **VS Code extensions** (open VS Code → Extensions panel, search and install):
   - "C# Dev Kit" (Microsoft) — for the backend
   - "ES7+ React/Redux snippets" (optional, for the frontend)

---

## 1. Running the BACKEND (ASP.NET Core API)

Open a terminal **inside** `backend/EcommerceApi`:

```bash
cd backend/EcommerceApi
```

**Step 1 — Restore NuGet packages** (downloads the libraries listed in the .csproj file):
```bash
dotnet restore
```

**Step 2 — Create the database using EF Core migrations:**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
- `migrations add` looks at your `Models/User.cs` and generates the SQL needed to create matching tables.
- `database update` actually runs that SQL against your local SQL Server (`SQLEXPRESS` instance) and creates the `EcommerceDb` database.
- You should see a new `Migrations/` folder appear with generated files — that's normal and expected.

**Step 3 — Run the API:**
```bash
dotnet run
```
Watch the terminal output — it prints something like:
```
Now listening on: https://localhost:7000
Now listening on: http://localhost:5000
```
**Copy the https port number** — you'll need it to match in the frontend (see below).

**Step 4 — Test it works (optional but recommended):**
Open your browser to `https://localhost:7000/swagger` — you'll see a UI listing `/api/auth/register`, `/api/auth/login`, etc. You can test the API directly here before even touching React. Click "Try it out" on `/api/auth/register` and submit some test data.

---

## 2. Running the FRONTEND (React)

Open a **second, separate terminal** (leave the backend running in the first one) inside `frontend`:

```bash
cd frontend
```

**Step 1 — Install packages:**
```bash
npm install
```

**Step 2 — Match the backend URL:**
Open `frontend/src/api/axiosInstance.js` and check this line:
```js
const BASE_URL = 'https://localhost:7000/api'
```
If your backend printed a different port in Step 3 above (e.g. `7123` instead of `7000`), change this line to match.

**Step 3 — Run the React dev server:**
```bash
npm run dev
```
Terminal will show:
```
Local: http://localhost:5173/
```
Open that URL in your browser.

---

## 3. How the frontend connects to the backend (the full flow)

1. **CORS** — In `Program.cs`, the backend explicitly allows requests from `http://localhost:5173` (the React dev server address). Without this, the browser blocks the request even if the API works fine — this is a browser security rule, not a bug. If you ever change the React port, update `Cors:AllowedOrigin` in `appsettings.json` too.

2. **Register/Login request** — When you submit the form in `Register.jsx`, it calls `axiosInstance.post('/auth/register', {...})`, which becomes a real HTTP request to `https://localhost:7000/api/auth/register`.

3. **Backend processes it** — `AuthController` receives it → calls `AuthService.RegisterAsync()` → which hashes the password, saves the user via `AppDbContext` (EF Core) into SQL Server, and generates a JWT via `JwtHelper`.

4. **Response comes back** — `{ token, fullName, role }` is sent back as JSON.

5. **Frontend stores it** — `AuthContext.login()` saves the token to `localStorage` and updates React state, so the whole app now knows someone is logged in.

6. **Every future request is authenticated automatically** — the axios interceptor in `axiosInstance.js` reads the token from `localStorage` and attaches `Authorization: Bearer <token>` to every request without you writing that code again.

7. **Role-based redirect** — after login, `Login.jsx`/`Register.jsx` checks `response.data.role` and navigates to `/admin-home` or `/user-home`. If a User tries to manually type `/admin-home` in the browser, `ProtectedRoute.jsx` checks their role and bounces them back to `/login`. The backend also independently enforces this on `HomeController` with `[Authorize(Roles = "Admin")]` — so even if someone bypasses the frontend check, the API itself refuses the request. **This is an important point to mention in your evaluation: never rely on frontend checks alone for security, always enforce on the backend too.**

---

## 4. Common errors and fixes

| Error | Fix |
|---|---|
| `Set-Location : Cannot find path ... backend/EcommerceApi` | You opened the outer/wrapper folder in VS Code, not the actual project folder. See the note at the top of this README — open the inner `EcommerceApp` folder that directly contains `backend` and `frontend`. |
| `Unable to connect` in Swagger/React | Backend isn't running — check the first terminal |
| CORS error in browser console | Frontend port doesn't match `Cors:AllowedOrigin` in `appsettings.json`, or you forgot `app.UseCors(...)` runs before `UseAuthentication()` in Program.cs |
| `Login failed / 401` immediately | Check the email/password actually match what you registered — passwords are case-sensitive |
| `dotnet ef` command not found | Run `dotnet tool install --global dotnet-ef`, then restart your terminal |
| SQL Server connection error / login failed | Confirm **SQL Server (SQLEXPRESS)** service is running (Windows search → Services). Confirm the connection string in `appsettings.json` says `Server=localhost\SQLEXPRESS`. Try connecting to `localhost\SQLEXPRESS` in SSMS first to confirm the instance name is exactly right. |
| Port already in use | Another app is using 5173 or 7000 — close it, or change the port in `vite.config.js` / `launchSettings.json` |

---

## 5. Admin login (hardcoded — no public sign-up for Admin)

There is no way to register as Admin through the UI anymore. One fixed Admin account is
automatically created in the database the first time the backend starts (see
`Data/DbSeeder.cs`):

```
Email:    admin@ecommerce.com
Password: Admin@123
```

Just log in with these on the normal `/login` page — the backend already knows this
account's role is "Admin" and redirects accordingly. To use different credentials,
edit the constants at the top of `Data/DbSeeder.cs` **before** the first `dotnet run`
(it only seeds once — if you change it after the account already exists, delete the row
from the `Users` table in SSMS first, or drop and recreate the database).

## 6. Product management (Admin only)

- **Manual add** — Admin Dashboard has a form: Name, Price, Quantity → adds one product.
- **CSV bulk upload** — Admin Dashboard has a file upload. CSV must have a header row:
  ```
  Name,Price,Quantity
  Keyboard,999.00,50
  Mouse,499.00,120
  ```
  Any row with bad data (missing name, non-numeric price, etc.) is skipped and reported
  back with a specific error message and line number — valid rows still get added.
- **Viewing products** — both Admin and User dashboards show a live product table,
  fetched from `GET /api/product` (no role restriction on viewing, only on adding).

## 7. Quick test flow once both servers are running

1. Go to `http://localhost:5173/register`
2. Register a normal account → you should land on the (blue) User Dashboard, and the
   product table will be empty at first
3. Logout → log in with the hardcoded Admin credentials above → yellow Admin Dashboard
4. Add one product manually, confirm it appears in the table below
5. Try uploading a small CSV file with 2-3 product rows, confirm they appear too
6. Logout → log back in as the regular User → confirm the same products now show up
   there too, in a read-only table
7. Try manually typing `/admin-home` in the URL bar while logged in as a User → should
   redirect you away
8. Try registering with an email like `abc@abc` (no proper domain) → should be rejected
   client-side immediately, before it even reaches the server
