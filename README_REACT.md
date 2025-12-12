# SmartWaste - React + ASP.NET Core Web API

Your SmartWaste application has been converted to use **React frontend** with **ASP.NET Core Web API** backend!

## ✅ Why This Is Better

### Cross-Platform
- **Works on Windows, Mac, and Linux** 
- Backend API runs identically on all platforms
- Frontend is browser-based (same UI everywhere)

### Easier to Debug
- Browser DevTools for frontend debugging
- Network tab to see all API calls
- React DevTools for component inspection
- Much easier than Avalonia UI debugging

### Your Database Layer is Unchanged
- ✅ All LINQ queries still work
- ✅ All Stored Procedures still work
- ✅ Database functions, triggers, partitioning all work
- ✅ Entity Framework and Stored Procedure switching still works

## 🚀 How to Run

### Prerequisites
1. .NET 9 SDK
2. Node.js (v18 or higher)
3. SQL Server with your SmartWaste database

### Step 1: Start the Web API (Backend)

```bash
# Open Terminal/Command Prompt
cd E:\db-project\SmartWaste\App.WebAPI

# Run the API
dotnet run
```

The API will start at: **http://localhost:5000**

You'll see:
```
Now listening on: http://localhost:5000
```

**Keep this terminal open!**

### Step 2: Start React App (Frontend)

```bash
# Open a NEW Terminal/Command Prompt
cd E:\db-project\SmartWaste\smartwaste-react

# Start React dev server
npm run dev
```

The React app will start at: **http://localhost:5173**

You'll see:
```
  VITE v6.0.0  ready in 500 ms

  ➜  Local:   http://localhost:5173/
  ➜  press h + enter to show help
```

### Step 3: Open in Browser

Open your browser and go to: **http://localhost:5173**

## 🔑 Test Credentials

**CNIC:** 35201-0000001-0
**Password:** password1
**Role:** Citizen

## 📁 Project Structure

```
SmartWaste/
├── App.Core/              ← Your models, interfaces (unchanged)
├── App.BLL.EF/            ← Entity Framework LINQ (unchanged)
├── App.BLL.SP/            ← Stored Procedures (unchanged)
├── App.Factory/           ← Service factory (unchanged)
├── App.WebAPI/            ← NEW - ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── AuthController.cs       ← Login endpoint
│   │   └── CitizenController.cs    ← Citizen endpoints
│   ├── Program.cs         ← API configuration
│   └── appsettings.json   ← Connection string
└── smartwaste-react/      ← NEW - React Frontend
    ├── src/
    │   ├── pages/
    │   │   ├── Login.jsx           ← Login page
    │   │   ├── CitizenDashboard.jsx ← Dashboard
    │   ├── services/
    │   │   └── api.js              ← API calls
    │   └── App.jsx        ← Main app
    └── package.json
```

## 🌐 API Endpoints

All endpoints are at `http://localhost:5000/api/`

### Authentication
- POST `/auth/login` - Login with CNIC and password

### Citizen
- GET `/citizen/profile/{citizenID}` - Get citizen profile
- GET `/citizen/listings/{citizenID}` - Get all listings
- POST `/citizen/listings` - Create new listing
- PUT `/citizen/listings/{id}/cancel` - Cancel a listing
- GET `/citizen/categories` - Get waste categories
- GET `/citizen/areas` - Get areas
- POST `/citizen/price-estimate` - Calculate price

## 🔧 Configuration

### Change Database Connection

Edit `App.WebAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SmartWasteDB": "Server=localhost;Database=SmartWasteDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

### Switch Between EF and Stored Procedures

Edit `App.WebAPI/appsettings.json`:

```json
{
  "AppSettings": {
    "UseEntityFramework": true  // false to use Stored Procedures
  }
}
```

## 🐛 Debugging

### Backend (API)
- Check console where `dotnet run` is running
- API logs appear there
- Add breakpoints in Visual Studio/VS Code

### Frontend (React)
- Press F12 in browser to open DevTools
- Check Console tab for errors
- Check Network tab to see API calls
- Install React DevTools extension



If SQL Server connection fails on Mac, they can:
1. Use Docker: `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourPassword' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`
2. Connect to your Windows SQL Server remotely
3. Use Azure SQL Database

## 🎯 Features Implemented

✅ Login with role-based access control
✅ Citizen Dashboard
✅ Create waste listings
✅ View all listings (with real-time data from database!)
✅ Cancel listings
✅ Price calculation
✅ Category selection
✅ Modern, responsive UI
✅ Error handling
✅ Loading states

## 🚧 Not Implemented Yet

- Operator Portal
- Government Portal

These can be added later in the same way!

## 💡 Tips

1. **Always start the API first**, then the React app
2. **Keep both terminals open** while developing
3. Changes to React code **auto-reload** in browser (Hot Module Replacement)
4. Changes to C# code require **restarting the API**
5. Use browser DevTools Network tab to see all API requests/responses

## 🆘 Troubleshooting

### "Failed to fetch" error
- Make sure Web API is running at http://localhost:5000
- Check CORS is configured (already done in Program.cs)

### Port already in use
**API (5000):**
```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <process_id> /F

# Mac/Linux
lsof -ti:5000 | xargs kill
```

**React (5173):**
```bash
# Windows
netstat -ano | findstr :5173
taskkill /PID <process_id> /F

# Mac/Linux
lsof -ti:5173 | xargs kill
```

### Database connection error
- Check SQL Server is running
- Verify connection string in `appsettings.json`
- Test with SSMS or Azure Data Studio



Happy coding! 🚀
