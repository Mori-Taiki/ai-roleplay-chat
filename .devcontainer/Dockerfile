# Use the official Microsoft image for .NET SDK.
# This provides a consistent environment for building and running the backend.
FROM mcr.microsoft.com/dotnet/sdk:8.0

# The dev container user, 'vscode', is created by the base image.
# No need to create it again.

# Set the working directory
WORKDIR /workspace

# You can add other dependencies for the backend here if needed.
# For example:
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#    && apt-get -y install --no-install-recommends <your-package>