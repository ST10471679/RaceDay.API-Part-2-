# syntax=docker/dockerfile:1

# ---------- Stage 1: build & publish ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the restore layer is cached
# until the dependencies actually change.
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish
COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Listen on a non-privileged port; run in Production mode
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish ./

# Run as the non-root user built into the .NET 8 images
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
