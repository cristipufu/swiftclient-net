# OpenStack Swift dev/test all-in-one container

Docker file forked from [docker-swift-onlyone](https://github.com/MorrisJobke/docker-swift-onlyone)

## Windows Prerequisites

Install [Docker for Windows](https://docs.docker.com/docker-for-windows/), works on Windows 10 64bit only.

## Run OpenStack Swift docker image

You'll need to copy all files inside `vtfuture/SwiftClient/tools/docker-swift` and run `up.ps1`, this will build and start a swift container that exposes port 8080 on your windows.


Optionally you could follow these steps:

***Download project using git***

```bash
git clone https://github.com/vtfuture/SwiftClient
```

***Build docker image***

```bash
# navigate to docker-swift folder and build local
docker build -t swift-aio .
```

***Build a storage container***

```bash
docker run -v /srv --name SWIFT_DATA busybox
```

***Start swift container in background***

```bash
docker run --name SWIFT_AIO -d -p 8080:8080 --volumes-from SWIFT_DATA -t swift-aio
```

***Test connectivity***

```bash
Invoke-RestMethod -Method Get -Headers @{'X-Auth-User'= 'test:tester';'X-Auth-Key'='testing'} -Uri http://localhost:8080/auth/v1.0/
```

## Tear down

Run `down.ps1` or the following commands to stop and remove swift containers and image:

```bash
docker stop SWIFT_AIO
docker rm SWIFT_AIO
docker rm SWIFT_DATA
docker rmi swift-aio
``` 

## Linux Prerequisites 

OpenStack Swift docker is compatible with Ubuntu 14.04.3 LTS or newer.

### Install docker engine

```bash
#!/bin/bash

if hash docker 2>/dev/null; then
echo ">>> Docker detected, skyping prerequisites and install"
else
  # docker prerequisites
  apt-key adv --keyserver hkp://pgp.mit.edu:80 --recv-keys 58118E89F3A912897C070ADBF76221572C52609D
  AptDockerFile = "/etc/apt/sources.list.d/docker.list"
  echo "deb https://apt.dockerproject.org/repo ubuntu-trusty main" > $AptDockerFile
  apt-get update
  
  # install docker
  apt-get -y install docker-engine 
  echo ">>> Docker engine installed"
fi
```

Optionally you could add your user to the `docker` group in order to run docker commands without `sudo`.

```
sudo usermod -aG docker your-username
```

## Run OpenStack Swift docker image

You'll need to copy all files inside `vtfuture/SwiftClient/tools/docker-swift` on your Ubuntu server and run `up.sh`, this will build and start a swift container that exposes port 8080 on your server.

Optionally you could follow these steps:

***Download project using git***

```bash
cd ~/
git clone https://github.com/vtfuture/SwiftClient
```

***Build docker image***

```bash
# navigate to docker-swift folder
cd ~/SwiftClient/docker-swift

# Build local
docker build -t swift-aio .
```

***Build a storage container***

```bash
docker run -v /srv --name SWIFT_DATA busybox
```

***Start swift container in background***

```bash
docker run --name SWIFT_AIO -d -p 8080:8080 --volumes-from SWIFT_DATA -t swift-aio
```

***Test connectivity***

```bash
curl -i -H "X-Auth-User:test:tester" -H "X-Auth-Key:testing" http://localhost:8080/auth/v1.0/
```

## Tear down

Run `down.sh` or the following commands to stop and remove swift containers and image from your server:

```bash
docker stop SWIFT_AIO
docker rm SWIFT_AIO
docker rm SWIFT_DATA
docker rmi swift-aio
``` 
