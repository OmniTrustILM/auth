#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
ARG BUILDPLATFORM

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /
COPY ["src/Czertainly.Auth/Czertainly.Auth.csproj", "Czertainly.Auth/"]
RUN dotnet restore "Czertainly.Auth/Czertainly.Auth.csproj"
COPY . .
WORKDIR "/src/Czertainly.Auth"
RUN dotnet build "Czertainly.Auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Czertainly.Auth.csproj" -c Release -o /app/publish

FROM base AS final

LABEL org.opencontainers.image.authors="CZERTAINLY <support@czertainly.com>"

RUN addgroup --system --gid 10001 czertainly && adduser --system --home /opt/czertainly --uid 10001 --ingroup czertainly czertainly
#RUN addgroup --group czertainly --gid 10001 && adduser --uid 10001 --gid 10001 "czertainly" 

COPY --from=publish /app/publish /opt/czertainly
COPY ./docker /opt/czertainly

WORKDIR /opt/czertainly

ENV COMPlus_EnableDiagnostics=0

ENV AUTH_DB_CONNECTION_STRING=
ENV AUTH_CREATE_UNKNOWN_USERS=false
ENV AUTH_CREATE_UNKNOWN_ROLES=false

USER 10001

ENTRYPOINT ["/opt/czertainly/entry.sh"]
