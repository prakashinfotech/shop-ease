FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EBayClone.sln .
COPY src/EBayClone.Domain/EBayClone.Domain.csproj src/EBayClone.Domain/
COPY src/EBayClone.Application/EBayClone.Application.csproj src/EBayClone.Application/
COPY src/EBayClone.Infrastructure/EBayClone.Infrastructure.csproj src/EBayClone.Infrastructure/
COPY src/EBayClone.API/EBayClone.API.csproj src/EBayClone.API/

RUN dotnet restore

COPY . .
RUN dotnet publish src/EBayClone.API/EBayClone.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EBayClone.API.dll"]