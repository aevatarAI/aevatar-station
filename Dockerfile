# Use the official .NET 9.0 ASP.NET runtime base image.
# This image contains only what is needed to run ASP.NET Core applications,
# making the final container smaller and more secure compared to the full SDK image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install Node.js and npm
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Verify installation
RUN node --version && npm --version && npx --version

# Declare build argument to retrieve servicename
ARG servicename
ARG ENABLE_EPHEMERAL_CONFIG

ENV ENABLE_EPHEMERAL_CONFIG=${ENABLE_EPHEMERAL_CONFIG}

# Create group and user with GID and UID 1000
RUN addgroup --gid 1000 appgroup && \
    adduser --uid 1000 --ingroup appgroup --disabled-password --gecos "" appuser

# Set the working directory inside the container to /app
WORKDIR /app

# Copy the published application files from the CI build stages
COPY out/$servicename .

# Change ownership of all files in /app to the  appuser and appgroup
RUN chown -R appuser:appgroup /app

# Switch the container user to 'appuser' for better security
# Running as non-root helps reduce risks if the container is compromised
USER appuser