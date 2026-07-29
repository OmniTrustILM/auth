#!/bin/sh

appHome="/opt/auth"
source ${appHome}/static-functions

log "INFO" "Launching the Auth service"
dotnet Auth.dll
