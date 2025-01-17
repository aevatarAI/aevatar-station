FROM mcr.microsoft.com/dotnet/sdk:9.0
ARG servicename
WORKDIR /app
COPY out/$servicename .