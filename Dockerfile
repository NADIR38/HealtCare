# Use .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY ["HealthCare/HealthCare.csproj", "HealthCare/"]
COPY ["HealthcareSystem.Application/HealthcareSystem.Application.csproj", "HealthcareSystem.Application/"]
COPY ["HealthcareSystem.Domain/HealthcareSystem.Domain.csproj", "HealthcareSystem.Domain/"]
COPY ["HealthcareSystem.Infrastructure/HealthcareSystem.Infrastructure.csproj", "HealthcareSystem.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "HealthCare/HealthCare.csproj"

# Copy everything
COPY . .

# Build
WORKDIR "/src/HealthCare"
RUN dotnet build "HealthCare.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "HealthCare.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Don't set ASPNETCORE_URLS here - let Program.cs handle it
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .

# Use shell form to allow environment variable expansion
ENTRYPOINT sh -c "dotnet HealthCare.dll --urls http://0.0.0.0:${PORT:-8080}"