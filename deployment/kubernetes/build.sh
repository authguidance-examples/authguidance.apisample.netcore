#!/bin/bash

##########################################
# Build the API's code into a Docker image
##########################################

#
# Ensure that we are in the root folder
#
cd "$(dirname "${BASH_SOURCE[0]}")"

#
# Use a timestamp based tag and support both KIND and DockerHub repositories
#
TAG=$(date +%Y%m%d%H%M%S)
echo $TAG > ./dockertag.txt
if [ "$DOCKER_REPOSITORY" == "" ]; then
  DOCKER_IMAGE="finalnetcoreapi:$TAG"
else
  DOCKER_IMAGE="$DOCKER_REPOSITORY/finalnetcoreapi:$TAG"
fi

#
# Build the .NET API
#
cd ../..
dotnet publish sampleapi.csproj -c Release -r linux-x64 --no-self-contained
if [ $? -ne 0 ]; then
  echo '*** .NET API build problem encountered'
  exit 1
fi

#
# Build the Docker container
#
docker build --no-cache -f deployment/shared/Dockerfile -t "$DOCKER_IMAGE" .
if [ $? -ne 0 ]; then
  echo '*** API docker build problem encountered'
  exit 1
fi

#
# Push the API docker image
#
if [ "$DOCKER_REPOSITORY" == "" ]; then
  kind load docker-image "$DOCKER_IMAGE" --name $CLUSTER_NAME
else
  docker image push "$DOCKER_IMAGE"
fi
if [ $? -ne 0 ]; then
  echo '*** API docker push problem encountered'
  exit 1
fi
