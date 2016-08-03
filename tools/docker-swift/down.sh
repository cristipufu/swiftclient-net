#!/bin/sh

# Stop and remove swift containers and image if any
docker stop SWIFT_AIO
docker rm SWIFT_AIO
docker rm SWIFT_DATA
#docker rmi swift-aio
