#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
ARG BUILDPLATFORM

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /
COPY ["src/Auth/Auth.csproj", "Auth/"]
RUN dotnet restore "Auth/Auth.csproj"
COPY . .
WORKDIR "/src/Auth"
RUN dotnet build "Auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth.csproj" -c Release -o /app/publish

FROM base AS final

LABEL org.opencontainers.image.authors="ILM <ilm@omnitrust.com>"

RUN addgroup --system --gid 10001 ilm && adduser --system --home /opt/auth --uid 10001 --ingroup ilm ilm

COPY --from=publish /app/publish /opt/auth
COPY ./docker /opt/auth

WORKDIR /opt/auth

ENV COMPlus_EnableDiagnostics=0

ENV AUTH_DB_CONNECTION_STRING=
ENV AUTH_CREATE_UNKNOWN_USERS=false
ENV AUTH_CREATE_UNKNOWN_ROLES=false

USER 10001

ENTRYPOINT ["/opt/auth/entry.sh"]
