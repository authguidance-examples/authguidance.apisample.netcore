#!/bin/bash

#############################################################
# A script to build and run the API with a test configuration
#############################################################

cd "$(dirname "${BASH_SOURCE[0]}")"

#
# Download development SSL certificates if required
#
./downloadcerts.sh
if [ $? -ne 0 ]; then
    exit
fi

#
# Copy down the test configuration, to point the API to Wiremock rather than AWS Cognito
#
cp environments/test.config.json ./api.config.json

#
# Get the platform
#
case "$(uname -s)" in

  Darwin)
    PLATFORM='MACOS'
 	;;

  MINGW64*)
    PLATFORM='WINDOWS'
	;;
esac

#
# Run the API in a child window
#
echo 'Running API ...'
if [ "$PLATFORM" == 'MACOS' ]; then
    #open -a Terminal ./run_wiremock.sh
    open -a Terminal ./run_api.sh
else
    #"$GIT_BASH" -c ./run_wiremock.sh &
    "$GIT_BASH" -c ./run_api.sh &
fi

#
# Wait for endpoints to become available
#
#echo 'Waiting for Wiremock endpoints to come up ...'
#WIREMOCK_URL='https://login.authsamples-dev.com:446/__admin/mappings'
#while [ "$(curl -k -s -X GET -o /dev/null -w '%{http_code}' "$WIREMOCK_URL")" != '200' ]; do
#    sleep 2
#done

echo 'Waiting for API endpoints to come up ...'
API_URL='https://api.authsamples-dev.com:445/api/companies'
while [ "$(curl -k -s -X GET -o /dev/null -w '%{http_code}' "$API_URL")" != '401' ]; do
    sleep 2
done

#
# Restore the API configuration once the API is loaded
#
cp environments/api.config.json ./api.config.json

#
# Indicate success
#
echo "Start tests via 'dotnet test' ..."
