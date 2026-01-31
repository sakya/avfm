#!/bin/bash
dotnet clean AVFM.sln -c Release
dotnet publish AVFM.sln -c Release --runtime linux-x64 -p:PublishReadyToRun=true --self-contained --output ./dist/linux-x64

