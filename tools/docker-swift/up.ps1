$ErrorActionPreference = "Stop"

Write-Output "Building storage container"

# Build storage container
docker run -v /srv --name SWIFT_DATA busybox

Write-Output "Starting Swift server"

# Start swift server
docker run --restart=unless-stopped --name SWIFT_AIO -d -p 8080:8080 --volumes-from SWIFT_DATA -t  morrisjobke/docker-swift-onlyone

# Wait for supervisord to start
Start-Sleep -Seconds 15

Write-Output "Testing authentication"

# test auth
Invoke-RestMethod -Method Get -Headers @{'X-Auth-User'= 'test:tester';'X-Auth-Key'='testing'} -Uri http://localhost:8080/auth/v1.0/

Write-Output "Done"