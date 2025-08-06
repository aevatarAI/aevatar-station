# ABOUTME: Dockerfile for Aevatar DbMigrator service
# ABOUTME: Builds the database migration and seeding tool for deployment

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY NuGet.Config ./
COPY station/common.props ./

# Copy project file and restore dependencies
COPY station/src/Aevatar.DbMigrator/Aevatar.DbMigrator.csproj station/src/Aevatar.DbMigrator/
COPY station/src/Aevatar.Application.Contracts/Aevatar.Application.Contracts.csproj station/src/Aevatar.Application.Contracts/
COPY station/src/Aevatar.Domain.Shared/Aevatar.Domain.Shared.csproj station/src/Aevatar.Domain.Shared/
COPY station/src/Aevatar.MongoDB/Aevatar.MongoDB.csproj station/src/Aevatar.MongoDB/

RUN dotnet restore "station/src/Aevatar.DbMigrator/Aevatar.DbMigrator.csproj"

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish "station/src/Aevatar.DbMigrator/Aevatar.DbMigrator.csproj" -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Aevatar.DbMigrator.dll"]