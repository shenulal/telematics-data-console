# Telematics Data Console

A GPS Tracking Data Verification Application for managing and verifying telematics device data.

## Tech Stack

### Backend
- **.NET 8 Web API** - RESTful API with Clean Architecture
- **Entity Framework Core** - ORM with SQL Server
- **JWT Authentication** - Role-based access control with permission claims
- **Serilog** - Structured logging

### Frontend
- **Next.js 14** - React framework with App Router
- **TypeScript** - Type-safe development
- **Tailwind CSS** - Utility-first CSS framework
- **Zustand** - State management
- **React Hook Form + Zod** - Form handling and validation
- **Lucide React** - Icon library
- **html5-qrcode** - Barcode/QR code scanning

## Features

- 🔐 **Authentication & Authorization** - JWT-based auth with role-based permissions
- 📱 **IMEI Verification** - Verify GPS device data with barcode/QR scanning support
- 📊 **Dashboard** - Real-time statistics and verification metrics
- 👥 **User Management** - Manage technicians, resellers, and administrators
- 📋 **Verification Logs** - Complete audit trail of all verifications
- 🗺️ **Location Tracking** - View device location with Google Maps integration
- 📱 **PWA Support** - Progressive Web App for mobile access

## Project Structure

```
telematics-data-console/
├── backend/
│   ├── TelematicsDataConsole.API/        # Web API layer
│   ├── TelematicsDataConsole.Core/       # Domain entities & interfaces
│   └── TelematicsDataConsole.Infrastructure/  # Data access & services
├── frontend/
│   ├── src/
│   │   ├── app/          # Next.js App Router pages
│   │   ├── components/   # React components
│   │   └── lib/          # Utilities, API client, store
│   └── public/           # Static assets
└── docker-compose.yml    # Docker configuration
```

## Getting Started

### Prerequisites

- Node.js 18+
- .NET 8 SDK
- SQL Server (or SQL Server Express)

### Backend Setup

1. Navigate to the backend API project:
   ```bash
   cd backend/TelematicsDataConsole.API
   ```

2. Update the connection string in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=TelematicsDataConsole;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

3. Run the API:
   ```bash
   dotnet run
   ```

The API will start at `https://localhost:7001` (or `http://localhost:5000`).

### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Create a `.env.local` file:
   ```env
   NEXT_PUBLIC_API_URL=https://localhost:7001/api
   ```

4. Run the development server:
   ```bash
   npm run dev
   ```

The frontend will start at `http://localhost:3000`.

## Environment Variables

### Backend (`appsettings.json`)

| Variable | Description |
|----------|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Key` | JWT signing key (min 32 characters) |
| `Jwt:Issuer` | JWT issuer |
| `Jwt:Audience` | JWT audience |
| `Jwt:ExpiryInMinutes` | Token expiry time |
| `VzoneApi:BaseUrl` | External GPS API URL |
| `Cors:AllowedOrigins` | Allowed CORS origins |

### Frontend (`.env.local`)

| Variable | Description |
|----------|-------------|
| `NEXT_PUBLIC_API_URL` | Backend API URL |

## Default Users

The application seeds the following default users:

| Username | Password | Role |
|----------|----------|------|
| admin | Admin@123 | Administrator |
| technician | Tech@123 | Technician |

## API Documentation

The API follows RESTful conventions. Key endpoints:

- `POST /api/auth/login` - User authentication
- `GET /api/verification/device-data/{imei}` - Get device data
- `POST /api/verification/log` - Submit verification log
- `GET /api/dashboard/stats` - Dashboard statistics
- `GET /api/technicians` - List technicians
- `GET /api/resellers` - List resellers

## Deployment

### Frontend (Netlify)

1. Push your code to GitHub
2. Connect your repository to Netlify
3. Configure build settings:
   - **Build command**: `npm run build`
   - **Publish directory**: `.next`
   - **Base directory**: `frontend`
4. Add environment variables in Netlify Dashboard:
   - `NEXT_PUBLIC_API_URL`: Your backend API URL (e.g., `https://your-api.azurewebsites.net/api`)

### Backend (Azure App Service, Railway, or Render)

Since Netlify doesn't support .NET, deploy the backend separately:

#### Option 1: Azure App Service
1. Create an Azure App Service (Windows or Linux)
2. Deploy using Visual Studio, VS Code, or Azure CLI:
   ```bash
   cd backend/TelematicsDataConsole.API
   dotnet publish -c Release
   az webapp deploy --resource-group <group> --name <app-name> --src-path bin/Release/net8.0/publish
   ```
3. Configure connection strings in Azure Portal > Configuration

#### Option 2: Railway
1. Connect your GitHub repository
2. Set the root directory to `backend/TelematicsDataConsole.API`
3. Railway auto-detects .NET projects
4. Add environment variables for connection strings

#### Option 3: Render
1. Create a new Web Service
2. Connect your GitHub repository
3. Set build command: `dotnet publish -c Release -o out`
4. Set start command: `dotnet out/TelematicsDataConsole.API.dll`

### CORS Configuration

After deploying, update `appsettings.json` or environment variables to include your Netlify domain:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-app.netlify.app",
      "http://localhost:3000"
    ]
  }
}
```

## License

MIT License

