#!/usr/bin/env bash

root=$(cd "$(dirname "$0")"; pwd -P)
artifacts=$root/artifacts
configuration=Release

skipTests=0

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            echo "./build.sh [--skip-tests]"
            exit 1
            ;;

        --skip-tests)
            skipTests=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

export CLI_VERSION=`cat ./global.json | grep -E '[0-9]\.[0-9]\.[a-zA-Z0-9\-]*' -o`
export DOTNET_INSTALL_DIR="$root/.dotnetcli"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

dotnet_version=$(dotnet --version)

if [ "$dotnet_version" != "$CLI_VERSION" ]; then
    curl -sSL https://raw.githubusercontent.com/dotnet/cli/v$CLI_VERSION/scripts/obtain/dotnet-install.sh | bash /dev/stdin --version "$CLI_VERSION" --install-dir "$DOTNET_INSTALL_DIR"
fi

dotnet build src/JustEat.StatsD/JustEat.StatsD.csproj --output $artifacts --configuration $configuration --framework "netstandard2.0" || exit 1

if [ $skipTests == 0 ]; then
    dotnet test src/JustEat.StatsD.Tests/JustEat.StatsD.Tests.csproj --output $artifacts --configuration $configuration --framework "netcoreapp2.1" || exit 1
fi
