#!/bin/bash
export UseEnvironmentVariables=true
export AevatarOrleans__AdvertisedIP=127.0.0.1
export AevatarOrleans__SiloPort=10003
export AevatarOrleans__GatewayPort=20003
export AevatarOrleans__SILO_NAME_PATTERN=Projector
export HealthCheck__Port=18083
export AevatarOrleans__DashboardIp=127.0.0.1
export AevatarOrleans__DashboardPort=19082
export OrleansEventSourcing__Provider=mongodb
export Orleans__MongoDBESClient=mongodb://localhost:27017
nohup dotnet run -c Release --project station/src/Aevatar.Silo/Aevatar.Silo.csproj > silo-projector-restart.log 2>&1 &
echo $! > silo-projector.pid
echo "Projector silo started with PID: $(cat silo-projector.pid)"