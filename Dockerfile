FROM mcr.microsoft.com/dotnet/sdk:9.0
RUN apt-get update && apt-get install -y \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*
ARG servicename
WORKDIR /app
COPY out/$servicename .