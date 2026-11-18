# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files first for Docker layer caching
COPY src/EBayClone.Domain/EBayClone.Domain.csproj src/EBayClone.Domain/
COPY src/EBayClone.Application/EBayClone.Application.csproj src/EBayClone.Application/
COPY src/EBayClone.Infrastructure/EBayClone.Infrastructure.csproj src/EBayClone.Infrastructure/
COPY src/EBayClone.API/EBayClone.API.csproj src/EBayClone.API/

# Restore only the API project.
# This avoids the missing EBayClone.Tests project
# referenced by EBayClone.sln.
RUN dotnet restore src/EBayClone.API/EBayClone.API.csproj

# Copy application source
COPY . .

# Publish API
RUN dotnet publish src/EBayClone.API/EBayClone.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EBayClone.API.dll"]
