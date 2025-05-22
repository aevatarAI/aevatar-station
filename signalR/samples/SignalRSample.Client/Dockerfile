# NOTE: Build context must be the project root (aevatar-station)
# Example: docker build -f signalR/samples/SignalRSample.Client/Dockerfile .
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ../Directory.Packages.props ./
COPY ["../signalR/src/Aevatar.SignalR/Aevatar.SignalR.csproj", "signalR/src/Aevatar.SignalR/"]
COPY ["../signalR/samples/SignalRSample.Client/SignalRSample.Client.csproj", "signalR/samples/SignalRSample.Client/"]
COPY ["../signalR/samples/SignalRSample.GAgents/SignalRSample.GAgents.csproj", "signalR/samples/SignalRSample.GAgents/"]
RUN dotnet restore "signalR/samples/SignalRSample.Client/SignalRSample.Client.csproj"
COPY .. .
WORKDIR "/src/signalR/samples/SignalRSample.Client"
RUN dotnet build "SignalRSample.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SignalRSample.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM publish AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SignalRSample.Client.dll"]
