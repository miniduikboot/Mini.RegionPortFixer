#!/usr/bin/env bash
set -euo pipefail

# The normal build
dotnet build -c Release
# Artifact can be found in Mini.RegionPortFixer/bin/Release/Mini.RegionPortFixer.dll

# building with reactor support
dotnet build -c Release \
    -p:DefineConstants=REACTOR \
    -p:OutputPath=bin-reactor/

cp ./Mini.RegionPortFixer/bin-reactor/Mini.RegionPortFixer.dll ./Mini.RegionPortFixer/bin-reactor/Mini.RegionPortFixer_Reactor.dll
# Artifact can be found in Mini.RegionPortFixer/bin-reactor/Mini.RegionPortFixer_Reactor.dll
