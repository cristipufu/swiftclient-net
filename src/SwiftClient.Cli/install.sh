#!/usr/bin/env bash

if hash dnx 2>/dev/null; then
echo "DNX detected, skyping prerequisites"
else
	#install Mono
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
	echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
	sudo apt-get update

	# install DNVM
	sudo apt-get install unzip curl
	curl -sSL https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.sh | DNX_BRANCH=dev sh && source ~/.dnx/dnvm/dnvm.sh

	# install DNX for Mono
	dnvm upgrade -r mono
fi

# install Swift.Cli
CLI_USER_HOME=~/swift-client-cli
CLI_SOURCE="https://get.veritech.io/packages/SwiftClient.Cli/latest"

mkdir -p $CLI_USER_HOME
cd $CLI_USER_HOME

if [ -s "$CLI_USER_HOME/SwiftClient.Cli" ]; then
    echo "SwiftClient.Cli is already installed in $CLI_USER_HOME, trying to update"
else
    echo "Downloading SwiftClient.Cli package to '$CLI_USER_HOME'"
fi

curl -sS $CLI_SOURCE > package.zip || {
    echo >&2 "Failed to download '$DNVM_SOURCE'.."
    return 1
}

unzip package.zip
rm package.zip

echo "Type `$CLI_USER_HOME/SwiftClient.Cli -h <host> -u <user> -p <password>` to start SwiftClient.Cli"