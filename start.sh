#!/bin/bash

###########################################################################
# A script to download SSL certificates, then build and run the API locally
###########################################################################

cd "$(dirname "${BASH_SOURCE[0]}")"

#
# Download SSL certificates from a central repo if needed
#
if [ ! -d 'certs' ]; then
    git clone https://github.com/gary-archer/oauth.developmentcertificates ./certs
fi
if [ $? -ne 0 ]; then
    echo 'Problem encountered downloading development certificates'
    exit
fi

#
# Build the app
#
dotnet build
if [ $? -ne 0 ]; then
    echo 'Problem encountered building the API'
    exit
fi

#
# Then start listening
#
dotnet watch run
if [ $? -ne 0 ]; then
    echo 'Problem encountered running the API'
    exit
fi