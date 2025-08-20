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

# Install additional tools for benchmarks (if servicename is BenchmarkRunner)
ARG servicename
RUN if [ "$servicename" = "BenchmarkRunner" ]; then \
        apt-get update && apt-get install -y jq bc && apt-get clean && rm -rf /var/lib/apt/lists/*; \
    fi

ARG ENABLE_EPHEMERAL_CONFIG
WORKDIR /app
COPY out/$servicename .

# Special entrypoint for BenchmarkRunner
RUN if [ "$servicename" = "BenchmarkRunner" ]; then \
        chmod +x unified-benchmark-runner.sh; \
    fi

ENV ENABLE_EPHEMERAL_CONFIG=${ENABLE_EPHEMERAL_CONFIG}

# Use unified-benchmark-runner.sh as entrypoint for BenchmarkRunner
RUN if [ "$servicename" = "BenchmarkRunner" ]; then \
        echo '#!/bin/bash' > /entrypoint.sh && \
        echo './unified-benchmark-runner.sh "$@"' >> /entrypoint.sh && \
        chmod +x /entrypoint.sh; \
    else \
        echo '#!/bin/bash' > /entrypoint.sh && \
        echo 'exec dotnet *.dll "$@"' >> /entrypoint.sh && \
        chmod +x /entrypoint.sh; \
    fi

ENTRYPOINT ["/entrypoint.sh"]