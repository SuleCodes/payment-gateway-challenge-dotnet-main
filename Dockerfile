# ---- Build stage: full SDK, never shipped ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore from the project files only, so this layer (all NuGet packages) is cached
# and only re-runs when a .csproj or the NuGet config changes, not on every code change.
COPY nuget.config Directory.Build.props ./
COPY src/PaymentGateway.Api/PaymentGateway.Api.csproj                         src/PaymentGateway.Api/
COPY src/PaymentGateway.Application/PaymentGateway.Application.csproj         src/PaymentGateway.Application/
COPY src/PaymentGateway.Common/PaymentGateway.Common.csproj                   src/PaymentGateway.Common/
COPY src/PaymentGateway.Domain/PaymentGateway.Domain.csproj                   src/PaymentGateway.Domain/
COPY src/PaymentGateway.Infrastructure/PaymentGateway.Infrastructure.csproj   src/PaymentGateway.Infrastructure/
RUN dotnet restore src/PaymentGateway.Api/PaymentGateway.Api.csproj

COPY src/ src/
RUN dotnet publish src/PaymentGateway.Api/PaymentGateway.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime stage: ASP.NET runtime only, no SDK or source code ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Non-root: .NET 8 images ship an unprivileged "app" user. The published files stay owned
# by root, so the process can read but not modify its own binaries.
USER $APP_UID

# .NET 8 images listen on 8080 by default (ASPNETCORE_HTTP_PORTS), a port non-root users can bind.
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]
