#!/usr/bin/env sh

PEM_FILE=$1
CERTS=$(grep 'END CERTIFICATE' $PEM_FILE| wc -l)

# For every cert in the PEM file, extract it and store it to CA certificates
# awk command: step 1, if line is in the desired cert, print the line
#              step 2, increment counter when last line of cert is found
for N in $(seq 0 $(($CERTS - 1))); do
  ALIAS="czertainly-trusted-$N"
  cat $PEM_FILE |
        awk "n==$N { print }; /END CERTIFICATE/ { n++ }" > /usr/local/share/ca-certificates/$ALIAS.crt
done

update-ca-certificates