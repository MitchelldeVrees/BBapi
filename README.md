# BBapi Web Application

This workspace contains:

- `backend`: ASP.NET Core Web API
- `frontend`: Angular application

## Prerequisites

- .NET SDK (project currently targets .NET 10)
- Node.js (current setup validated with Node 22.14.0)
- npm

## Run The Backend

From the repository root:

```powershell
cd backend
dotnet run
```

The API runs on `http://localhost:5088` in development.

## Run The Frontend

In another terminal from the repository root:

```powershell
cd frontend
npm start
```

Angular runs on `http://localhost:4200`.

## App Behavior

When both apps are running, the Angular UI fetches weather data from:

- `GET http://localhost:5088/weatherforecast`

If the API is not running, the frontend shows a clear error message.