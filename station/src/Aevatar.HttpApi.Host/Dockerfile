# ABOUTME: Dockerfile for Aevatar HttpApi.Host service
# ABOUTME: Multi-stage build for API host with Orleans client and Kubernetes support

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY NuGet.Config ./
COPY station/common.props ./
COPY ["station/src/Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj", "station/src/Aevatar.HttpApi.Host/"]
COPY ["station/src/Aevatar.HttpApi/Aevatar.HttpApi.csproj", "station/src/Aevatar.HttpApi/"]
COPY ["station/src/Aevatar.Application/Aevatar.Application.csproj", "station/src/Aevatar.Application/"]
COPY ["station/src/Aevatar.Domain.Shared/Aevatar.Domain.Shared.csproj", "station/src/Aevatar.Domain.Shared/"]
COPY ["station/src/Aevatar.MongoDB/Aevatar.MongoDB.csproj", "station/src/Aevatar.MongoDB/"]
COPY ["station/src/Aevatar.Application.Contracts/Aevatar.Application.Contracts.csproj", "station/src/Aevatar.Application.Contracts/"]
COPY ["station/src/Aevatar.Kubernetes/Aevatar.Kubernetes.csproj", "station/src/Aevatar.Kubernetes/"]

# Restore packages
RUN dotnet restore "station/src/Aevatar.HttpApi.Host/Aevatar.HttpApi.Host.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/station/src/Aevatar.HttpApi.Host"
RUN dotnet build "Aevatar.HttpApi.Host.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Aevatar.HttpApi.Host.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aevatar.HttpApi.Host.dll"]