# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY TelematicsDataConsole.Core/TelematicsDataConsole.Core.csproj TelematicsDataConsole.Core/
COPY TelematicsDataConsole.Infrastructure/TelematicsDataConsole.Infrastructure.csproj TelematicsDataConsole.Infrastructure/
COPY TelematicsDataConsole.API/TelematicsDataConsole.API.csproj TelematicsDataConsole.API/
RUN dotnet restore TelematicsDataConsole.API/TelematicsDataConsole.API.csproj

# Copy everything and build
COPY . .
RUN dotnet build TelematicsDataConsole.API/TelematicsDataConsole.API.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish TelematicsDataConsole.API/TelematicsDataConsole.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelematicsDataConsole.API.dll"]

