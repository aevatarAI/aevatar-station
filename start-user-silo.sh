#!/bin/bash
export UseEnvironmentVariables=true
export AevatarOrleans__AdvertisedIP=127.0.0.1
export AevatarOrleans__SiloPort=10002
export AevatarOrleans__GatewayPort=20002
export AevatarOrleans__SILO_NAME_PATTERN=User
export HealthCheck__Port=18082
export AevatarOrleans__DashboardIp=127.0.0.1
export AevatarOrleans__DashboardPort=19081
export OrleansEventSourcing__Provider=mongodb
export Orleans__MongoDBESClient=mongodb://localhost:27017
nohup dotnet run -c Release --project station/src/Aevatar.Silo/Aevatar.Silo.csproj > silo-user-restart.log 2>&1 &
echo $! > silo-user.pid
echo "User silo started with PID: $(cat silo-user.pid)"