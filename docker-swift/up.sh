#!/bin/sh

# Stop and remove swift containers and image if any
docker stop SWIFT_AIO
docker rm SWIFT_AIO
docker rm SWIFT_DATA
docker rmi SWIFT_AIO_IMG

# Build local
docker build -t SWIFT_AIO_IMG .

# Build storage container
docker run -v /srv --name SWIFT_DATA busybox

# Start swift server
ID=$(docker run --name SWIFT_AIO -d -p 8080:8080 --volumes-from SWIFT_DATA -t SWIFT_AIO_IMG)

# print swift server logs
docker logs $ID

# test auth
curl -i -H "X-Auth-User:test:tester" -H "X-Auth-Key:testing" http://localhost:8080/auth/v1.0/
