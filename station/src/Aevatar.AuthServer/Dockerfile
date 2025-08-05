# ABOUTME: Dockerfile for Aevatar AuthServer service
# ABOUTME: Multi-stage build for .NET 9 application with MongoDB support

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY NuGet.Config ./
COPY station/common.props ./
COPY ["station/src/Aevatar.AuthServer/Aevatar.AuthServer.csproj", "station/src/Aevatar.AuthServer/"]
COPY ["station/src/Aevatar.Application.Contracts/Aevatar.Application.Contracts.csproj", "station/src/Aevatar.Application.Contracts/"]
COPY ["station/src/Aevatar.Domain.Shared/Aevatar.Domain.Shared.csproj", "station/src/Aevatar.Domain.Shared/"]
COPY ["station/src/Aevatar.MongoDB/Aevatar.MongoDB.csproj", "station/src/Aevatar.MongoDB/"]
COPY ["station/src/Aevatar.AuthServer.Grants/Aevatar.AuthServer.Grants.csproj", "station/src/Aevatar.AuthServer.Grants/"]

# Restore packages
RUN dotnet restore "station/src/Aevatar.AuthServer/Aevatar.AuthServer.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/station/src/Aevatar.AuthServer"
RUN dotnet build "Aevatar.AuthServer.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Aevatar.AuthServer.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aevatar.AuthServer.dll"]