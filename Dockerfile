FROM mcr.microsoft.com/dotnet/sdk:9.0

# Install Node.js, npm, Python, and uv
RUN apt-get update && \
    apt-get install -y curl python3 python3-pip python3-venv && \
    curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - && \
    apt-get install -y nodejs && \
    # Install uv (Python package manager with uvx command)
    curl -LsSf https://astral.sh/uv/install.sh | sh && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Add uv to PATH
ENV PATH="/root/.cargo/bin:$PATH"

# Verify installation
RUN node --version && npm --version && npx --version && \
    python3 --version && uv --version && uvx --version

ARG servicename
ARG ENABLE_EPHEMERAL_CONFIG
WORKDIR /app
COPY out/$servicename .
ENV ENABLE_EPHEMERAL_CONFIG=${ENABLE_EPHEMERAL_CONFIG}