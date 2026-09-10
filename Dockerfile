# syntax=docker/dockerfile:1

# The API image.
#
# Multi-stage: the SDK is needed to build and is ~800MB; the runtime image that
# actually ships is a fraction of that and contains no compiler, no source and no
# NuGet cache for an attacker to read.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The manifests first, and restore before the source is copied.
#
# This is the layer that matters for build times: restore is slow and its inputs
# change rarely, so Docker caches it and a source-only change skips it entirely.
COPY Directory.Packages.props Directory.Build.props global.json ./
COPY Workvivo.sln ./
COPY Workvivo.Domain/*.csproj Workvivo.Domain/
COPY Workvivo.Application/*.csproj Workvivo.Application/
COPY Workvivo.Infrastructure/*.csproj Workvivo.Infrastructure/
COPY Workvivo.API/*.csproj Workvivo.API/
COPY Workvivo.Resources/*.csproj Workvivo.Resources/
COPY tests/Directory.Build.props tests/
COPY tests/Workvivo.Domain.Tests/*.csproj tests/Workvivo.Domain.Tests/
COPY tests/Workvivo.Application.Tests/*.csproj tests/Workvivo.Application.Tests/
COPY tests/Workvivo.API.Tests/*.csproj tests/Workvivo.API.Tests/
COPY tests/Workvivo.IntegrationTests/*.csproj tests/Workvivo.IntegrationTests/

RUN dotnet restore Workvivo.API/Workvivo.API.csproj

COPY . .

RUN dotnet publish Workvivo.API/Workvivo.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# A non-root user. The base image ships one; using it means a container escape
# starts from an unprivileged account rather than root inside the namespace.
USER $APP_UID

COPY --from=build /app/publish .

# Kestrel listens on 8080 because a non-root process cannot bind 80.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# TLS is terminated at the ingress, not here. A certificate baked into an image is
# a certificate in every registry that image is pushed to.
ENV ASPNETCORE_ENVIRONMENT=Production

# Deliberately no HEALTHCHECK here.
#
# The runtime image has no curl or wget, and `dotnet --info` - the usual
# workaround - passes whether or not the application is serving anything. A probe
# that always succeeds is worse than none: it makes an orchestrator confident
# about a container that is failing every request.
#
# The application exposes the two probes an orchestrator should use, and they
# answer different questions, so do not point both at the same one:
#
#   /health/live   is the process alive   -> restart it
#   /health/ready  can it serve traffic   -> route around it

ENTRYPOINT ["dotnet", "Workvivo.API.dll"]
