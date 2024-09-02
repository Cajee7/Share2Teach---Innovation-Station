# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Setting working directory in container
WORKDIR /app

# Copying .csproj file and restoring dependencies
COPY Share2Teach.csproj ./
RUN dotnet restore

# Copying rest of the application
COPY . ./

# Building app
RUN dotnet publish -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install LibreOffice
RUN apt-get update && \
    apt-get install -y libreoffice && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Setting working directory
WORKDIR /app

# Copying the built app from the build stage
COPY --from=build-env /app/out .

# Exposing port 80
EXPOSE 80

# Finally running the application
ENTRYPOINT ["dotnet", "Share2Teach.dll"]
