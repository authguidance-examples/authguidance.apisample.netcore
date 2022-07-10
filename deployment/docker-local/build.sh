#!/bin/bash

##############################################################
# A script to test Docker deployment on a development computer
##############################################################

cd "$(dirname "${BASH_SOURCE[0]}")"
cd ../..

#
# Get the platform
#
case "$(uname -s)" in

  Darwin)
    PLATFORM="MACOS"
 	;;

  MINGW64*)
    PLATFORM="WINDOWS"
	;;

  Linux)
    PLATFORM="LINUX"
	;;
esac

#
# Download certificates if required
#
./downloadcerts.sh
if [ $? -ne 0 ]; then
  exit
fi

#
# Build the code
#
dotnet publish sampleapi.csproj -c Release -r linux-x64 --no-self-contained
if [ $? -ne 0 ]; then
  echo '*** .NET API build problem encountered'
  exit 1
fi

#
# Prepare root CA certificates that the Docker container will trust
#
cp ./certs/authsamples-dev.ca.pem deployment/docker-local/trusted.ca.pem

#
# On Windows, fix problems with trailing newline characters in Docker scripts
#
if [ "$PLATFORM" == 'WINDOWS' ]; then
  sed -i 's/\r$//' docker/docker-init.sh
fi

#
# Build the docker image
#
docker build -f deployment/docker/Dockerfile --build-arg TRUSTED_CA_CERTS='deployment/docker-local/trusted.ca.pem' -t finalnetcoreapi:v1 .
if [ $? -ne 0 ]; then
  echo 'Problem encountered building the API docker image'
  exit
fi
