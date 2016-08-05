# Getting Started with ASP.NET Core and OpenStack Swift

With ASP.NET Core being released .NET developers will switch to containers as the default deployment model, breaking away from IIS and monolithic apps is about to happen. 
Running ASP.NET web apps in containers being docker or windows containers will simplify and speed up not only deployment but also the build and test operations. 

Lets assume you have an ASP.NET Core MVC app that manages documents, photos and video files uploaded by users. 
Your application scales horizontally by running in multiple containers behind a reverse proxy. 
All your application instances must access the storage where user files are stored in order to function, but having the same storage shared between containers can a be a challenge.
If you have a HA setup then your containers are running on multiple machines, probability not event in the same data center, so instead of storing the files on disk your application could use a distributed storage system like OpenStack Swift.

OpenStack Swift is an eventually consistent storage system designed to scale horizontally without any single point of failure. All objects are stored with multiple copies and are replicated across zones and regions making Swift withstand failures in storage and network hardware. Swift can be used as a stand-alone distributed storage system on top of Linux without the need of expensive hardware solutions like NAS or SAN.
Data is stored and served directly over HTTP making Swift the ideal solution when dealing with applications running in containers.

Lets assume you've installed an OpenStack Swift cluster with a minimum of two swift servers, each containing a proxy and a storage node. Ideally these servers or VMs should be hosted in different regions/datacenters.
In order to access the Swift cluster from your ASP.NET Core application you can use SwiftClient.AspNetCore package. Adding both swift proxy endpoints to SwfitClient config will ensure that any app instance will share the same storage and if a Swift node becomes unreachable due to a restart or network failure all app instances will silently fail-over to the 2nd node.

