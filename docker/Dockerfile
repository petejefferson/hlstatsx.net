# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker layer caching for package restore
COPY src/HLStatsX.NET.Core/HLStatsX.NET.Core.csproj                     src/HLStatsX.NET.Core/
COPY src/HLStatsX.NET.Infrastructure/HLStatsX.NET.Infrastructure.csproj  src/HLStatsX.NET.Infrastructure/
COPY src/HLStatsX.NET.Web/HLStatsX.NET.Web.csproj                       src/HLStatsX.NET.Web/

RUN dotnet restore src/HLStatsX.NET.Web/HLStatsX.NET.Web.csproj -r linux-x64

# Copy full source and publish the web project
COPY . .
RUN dotnet publish src/HLStatsX.NET.Web/HLStatsX.NET.Web.csproj \
        -c Release -r linux-x64 --no-self-contained -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Listen on HTTP only — TLS termination is handled by a reverse proxy (e.g. Nginx)
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "HLStatsX.NET.Web.dll"]
