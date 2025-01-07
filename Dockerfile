FROM mcr.microsoft.com/dotnet/sdk:8.0.302-1
ARG servicename
WORKDIR /app
COPY out/$servicename .