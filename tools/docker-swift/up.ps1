$ErrorActionPreference = "Stop"

# Build local
docker build -t swift-aio .

# Build storage container
docker run -v /srv --name SWIFT_DATA busybox

# Start swift server
$ID = docker run --name SWIFT_AIO -d -p 8080:8080 --volumes-from SWIFT_DATA -t swift-aio

# Wait for supervisord to start
Start-Sleep -Seconds 15

# print swift server logs
docker logs $ID

# test auth
Invoke-RestMethod -Method Get -Headers @{'X-Auth-User'= 'test:tester';'X-Auth-Key'='testing'} -Uri http://localhost:8080/auth/v1.0/