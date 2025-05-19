FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Directory.Packages.props ./
COPY ["src/Aevatar.SignalR/Aevatar.SignalR.csproj", "src/Aevatar.SignalR/"]
COPY ["samples/SignalRSample.Host/SignalRSample.Host.csproj", "samples/SignalRSample.Host/"]
COPY ["samples/SignalRSample.GAgents/SignalRSample.GAgents.csproj", "samples/SignalRSample.GAgents/"]
RUN dotnet restore "samples/SignalRSample.Host/SignalRSample.Host.csproj"
COPY . .
WORKDIR "/src/samples/SignalRSample.Host"
RUN dotnet build "SignalRSample.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SignalRSample.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM publish AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SignalRSample.Host.dll"]
