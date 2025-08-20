#!/bin/bash
export UseEnvironmentVariables=true
export AevatarOrleans__AdvertisedIP=127.0.0.1
export AevatarOrleans__SiloPort=10001
export AevatarOrleans__GatewayPort=20001
export AevatarOrleans__SILO_NAME_PATTERN=Scheduler
export HealthCheck__Port=18081
export AevatarOrleans__DashboardIp=127.0.0.1
export AevatarOrleans__DashboardPort=19080
export OrleansEventSourcing__Provider=mongodb
export Orleans__MongoDBESClient=mongodb://localhost:27017
nohup dotnet run -c Release --project station/src/Aevatar.Silo/Aevatar.Silo.csproj > silo-scheduler-restart.log 2>&1 &
echo $! > silo-scheduler.pid
echo "Scheduler silo started with PID: $(cat silo-scheduler.pid)"