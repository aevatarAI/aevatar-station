# ABOUTME: Dockerfile for Aevatar Silo service
# ABOUTME: Multi-stage build for Orleans silo with MongoDB clustering

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY NuGet.Config ./
COPY station/common.props ./
COPY ["station/src/Aevatar.Silo/Aevatar.Silo.csproj", "station/src/Aevatar.Silo/"]
COPY ["station/src/Aevatar.Domain.Shared/Aevatar.Domain.Shared.csproj", "station/src/Aevatar.Domain.Shared/"]
COPY ["station/src/Aevatar.MongoDB/Aevatar.MongoDB.csproj", "station/src/Aevatar.MongoDB/"]
COPY ["station/src/Aevatar.Application.Grains/Aevatar.Application.Grains.csproj", "station/src/Aevatar.Application.Grains/"]
COPY ["station/src/Aevatar.Domain.Grains/Aevatar.Domain.Grains.csproj", "station/src/Aevatar.Domain.Grains/"]
COPY ["signalR/src/Aevatar.SignalR/Aevatar.SignalR.csproj", "signalR/src/Aevatar.SignalR/"]
COPY ["framework/src/Aevatar/Aevatar.csproj", "framework/src/Aevatar/"]
COPY ["framework/src/Aevatar.PermissionManagement/Aevatar.PermissionManagement.csproj", "framework/src/Aevatar.PermissionManagement/"]
COPY ["framework/src/Aevatar.Plugins/Aevatar.Plugins.csproj", "framework/src/Aevatar.Plugins/"]
COPY ["framework/src/Aevatar.EventSourcing.Core/Aevatar.EventSourcing.Core.csproj", "framework/src/Aevatar.EventSourcing.Core/"]
COPY ["framework/src/Aevatar.EventSourcing.MongoDB/Aevatar.EventSourcing.MongoDB.csproj", "framework/src/Aevatar.EventSourcing.MongoDB/"]
COPY ["framework/src/Aevatar.Core/Aevatar.Core.csproj", "framework/src/Aevatar.Core/"]
COPY ["framework/src/Aevatar.Core.Abstractions/Aevatar.Core.Abstractions.csproj", "framework/src/Aevatar.Core.Abstractions/"]
COPY ["station/src/Aevatar.Application.Contracts/Aevatar.Application.Contracts.csproj", "station/src/Aevatar.Application.Contracts/"]
COPY ["station/src/Aevatar.CQRS/Aevatar.CQRS.csproj", "station/src/Aevatar.CQRS/"]
COPY ["station/src/Aevatar.Domain/Aevatar.Domain.csproj", "station/src/Aevatar.Domain/"]
COPY ["station/src/Aevatar.Neo4JStore/Aevatar.Neo4JStore.csproj", "station/src/Aevatar.Neo4JStore/"]
COPY ["station/src/Aevatar.Station.Feature/Aevatar.Station.Feature.csproj", "station/src/Aevatar.Station.Feature/"]
COPY ["station/samples/E2E.Grains/E2E.Grains.csproj", "station/samples/E2E.Grains/"]

# Restore packages
RUN dotnet restore "station/src/Aevatar.Silo/Aevatar.Silo.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/station/src/Aevatar.Silo"
RUN dotnet build "Aevatar.Silo.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Aevatar.Silo.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 11111
EXPOSE 30000

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aevatar.Silo.dll"]