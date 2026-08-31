# Build context is the backend/ folder (see docker-compose.yml), so this Dockerfile
# can see all five layered projects, not just the Api.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first and restore, so dependency restore is cached
# independently of source-code changes.
COPY Dma.OrderIntake.Domain/Dma.OrderIntake.Domain.csproj Dma.OrderIntake.Domain/
COPY Dma.OrderIntake.Contracts/Dma.OrderIntake.Contracts.csproj Dma.OrderIntake.Contracts/
COPY Dma.OrderIntake.Application/Dma.OrderIntake.Application.csproj Dma.OrderIntake.Application/
COPY Dma.OrderIntake.Infrastructure/Dma.OrderIntake.Infrastructure.csproj Dma.OrderIntake.Infrastructure/
COPY Dma.OrderIntake.Api/Dma.OrderIntake.Api.csproj Dma.OrderIntake.Api/
RUN dotnet restore Dma.OrderIntake.Api/Dma.OrderIntake.Api.csproj

COPY . .
RUN dotnet publish Dma.OrderIntake.Api/Dma.OrderIntake.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The base image runs as a non-root user by default. /data is where the
# docker-compose "api-data" volume gets mounted for the SQLite file, so it
# needs to be owned by that user before the volume is attached.
USER root
RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /data
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Dma.OrderIntake.Api.dll"]
