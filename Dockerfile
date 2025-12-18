# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for better layer caching)
COPY WasteCollectionSystem.sln ./
COPY WasteCollectionSystem.csproj ./ 
COPY NuGet.Config ./

# Restore dependencies
RUN dotnet restore WasteCollectionSystem.csproj

# Copy remaining source code
COPY . ./

# Build and publish (exclude test projects)
RUN dotnet publish WasteCollectionSystem.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Set environment variables (can be overridden via docker-compose or docker run)
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/ || exit 1

# Start application
ENTRYPOINT ["dotnet", "WasteCollectionSystem.dll"]

