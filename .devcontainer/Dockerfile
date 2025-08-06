# Use the official Microsoft image for .NET SDK.
# This provides a consistent environment for building and running the backend.
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

# The dev container user, 'vscode', is created by the base image.
# No need to create it again.

# Set the working directory
WORKDIR /workspace

# You can add other dependencies for the backend here if needed.
# For example:
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#    && apt-get -y install --no-install-recommends <your-package>

# Install Node.js 20.x and npm (official setup)
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install -y curl ca-certificates gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*
