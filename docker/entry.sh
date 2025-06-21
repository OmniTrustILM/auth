#!/bin/sh

czertainlyHome="/opt/czertainly"
source ${czertainlyHome}/static-functions

#if [ -f ${czertainlyHome}/trusted-certificates.pem ]
#then
#  log "INFO" "Adding additional trusted certificates to cacerts"
#  ./update-cacerts.sh ${czertainlyHome}/trusted-certificates.pem
#
#  log "INFO" "Launching the Auth service"
#  dotnet Czertainly.Auth.dll
#else
#  log "ERROR" "No trusted certificates were provided!"
#fi

log "INFO" "Launching the Auth service"
dotnet Czertainly.Auth.dll