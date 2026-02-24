# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY FPT-EXE-201.sln .
COPY src/FPT.EXE201.Api/FPT.EXE201.Api.csproj src/FPT.EXE201.Api/
COPY src/FPT.EXE201.Application/FPT.EXE201.Application.csproj src/FPT.EXE201.Application/
COPY src/FPT.EXE201.Domain/FPT.EXE201.Domain.csproj src/FPT.EXE201.Domain/
COPY src/FPT.EXE201.Infrastructure/FPT.EXE201.Infrastructure.csproj src/FPT.EXE201.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish in Release mode
WORKDIR /src/src/FPT.EXE201.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Vietnamese locale support (for UTF-8)
RUN apt-get update && apt-get install -y --no-install-recommends \
    locales \
    && sed -i '/vi_VN.UTF-8/s/^# //g' /etc/locale.gen \
    && locale-gen \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

ENV LANG=vi_VN.UTF-8
ENV LC_ALL=vi_VN.UTF-8

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser -d /app -s /sbin/nologin appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port (Kestrel default)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "FPT.EXE201.Api.dll"]
