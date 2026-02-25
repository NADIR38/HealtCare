# Use .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["HealthcareSystem.API/Healthcare.csproj", "Healthcare/"]
COPY ["HealthcareSystem.Application/HealthcareSystem.Application.csproj", "HealthcareSystem.Application/"]
COPY ["HealthcareSystem.Domain/HealthcareSystem.Domain.csproj", "HealthcareSystem.Domain/"]
COPY ["HealthcareSystem.Infrastructure/HealthcareSystem.Infrastructure.csproj", "HealthcareSystem.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Healthcare/Healthcare.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/Healthcare"
RUN dotnet build "Healthcare.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Healthcare.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use .NET 9 runtime for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Render provides PORT as environment variable
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Healthcare.dll"]
