#!/bin/bash

rm -fr ../../../MonkeyBoard.Android/obj
rm -fr ../../../MonkeyBoard.Android/bin
rm -fr ../obj
rm -fr ../bin
dotnet publish ../*.csproj -c Release /p:PublishAd=true