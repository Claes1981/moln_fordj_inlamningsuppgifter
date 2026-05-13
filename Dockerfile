# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY CloudSoft.slnx .
COPY src/ ./

# Restore dependencies
RUN dotnet restore CloudSoft.Web/CloudSoft.Web.csproj

# Build the application
RUN dotnet build CloudSoft.Web/CloudSoft.Web.csproj -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish CloudSoft.Web/CloudSoft.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Note: HEALTHCHECK removed - chiseled aspnet:10.0 image lacks curl.
# Health verification handled by docker-compose service dependencies instead.

ENTRYPOINT ["dotnet", "CloudSoft.Web.dll"]
