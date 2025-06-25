# Use the official .NET 9.0 ASP.NET runtime base image.
# This image contains only what is needed to run ASP.NET Core applications,
# making the final container smaller and more secure compared to the full SDK image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Create group and user with GID and UID 1000
RUN addgroup --gid 1000 appgroup && \
    adduser --uid 1000 --ingroup appgroup --disabled-password --gecos "" appuser

# Set the working directory inside the container to /app
WORKDIR /app

# Copy the published application files from the CI build stages
COPY out/$servicename .

# Change ownership of all files in /app to the newly created user and group
# This ensures 'appuser' has permission to access and run the app files
RUN chown -R appuser:appgroup /app

# Switch the container user to 'appuser' for better security
# Running as non-root helps reduce risks if the container is compromised
USER appuser

