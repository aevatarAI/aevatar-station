FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install Node.js and npm
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Verify installation
RUN node --version && npm --version && npx --version

ARG servicename
ARG ENABLE_EPHEMERAL_CONFIG
WORKDIR /app
COPY out/$servicename .
ENV ENABLE_EPHEMERAL_CONFIG=${ENABLE_EPHEMERAL_CONFIG}