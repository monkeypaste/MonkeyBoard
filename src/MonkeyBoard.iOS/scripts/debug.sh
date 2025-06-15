#!/bin/bash
CONFIG=Debug
FRAMEWORK="net8.0-ios"
RUNTIME="ios-arm64"
PLATFORM="AnyCPU"
DEVICE_ID="00008020-001945DA3669402E"
DEVICE_ARG="-p:_DeviceName="
EXE_NAME="MonkeyPaste.Keyboard.iOS"

clear

cd ..

if [ "$1" = "dual" ] || [ "$2" = "dual" ]; then
	echo "Skipping clean..."
else
# NOTE these auto-gen files break build saying TargetRuntime doesn't match PlatformTarget
	rm -fr obj/
	rm -fr bin/
	rm -fr *.csproj.user

	rm -fr ../MonkeyPaste.Keyboard.iOS.KeyboardExt/bin
	rm -fr ../MonkeyPaste.Keyboard.iOS.KeyboardExt/obj
	rm -fr ../MonkeyPaste.Keyboard.iOS.KeyboardExt/*.csproj.user

	rm -fr ../MonkeyPaste.Keyboard/obj
	rm -fr ../MonkeyPaste.Keyboard/bin
	rm -fr ../MonkeyPaste.Keyboard/*.csproj.user

	rm -fr ../MonkeyPaste.Keyboard.Common/obj
	rm -fr ../MonkeyPaste.Keyboard.Common/bin
	rm -fr ../MonkeyPaste.Keyboard.Common/*.csproj.user
fi

if [ "$1" = "sim" ] || [ "$2" = "sim" ]; then
	RUNTIME="iossimulator-x64"
	# ipad 18.0 4th gen
	DEVICE_ID="3F003BD7-6850-4229-A905-E774A786AEDA"
	# ipad 18.0 pro 11" M4
	#DEVICE_ID="24B0221D-A0D6-41CF-BC1F-262CFC7365C1"
	# ipad 18.0 mini 6th gen
	#DEVICE_ID="B6636BA5-F87B-4E46-90DB-FA9EC529D53C"
	DEVICE_ARG="-p:_DeviceName=:v2:udid="
else
	CONFIG="Release"
fi

if [ "$1" = "manual" ] || [ "$2" = "manual" ]; then
	dotnet publish -c ${CONFIG} -f ${FRAMEWORK} -p:RuntimeIdentifier=${RUNTIME} ${DEVICE_ARG}${DEVICE_ID}
	
	cd bin/${CONFIG}/${FRAMEWORK}/${RUNTIME}/publish

	# unzip the IPA file to tmp foldercd D
	mkdir ./tmp
	unzip ${EXE_NAME}.ipa -d ./tmp

	# run ios-deploy to install the app into iOS device
	ios-deploy -r -b ./tmp/Payload/*.app -O "/Users/tkefauver/Desktop/output.log" -E "/Users/tkefauver/Desktop/error.log"
	rm -r ./tmp
else
	dotnet build -t:Run -c ${CONFIG} -f ${FRAMEWORK} -p:Platform=${PLATFORM} ${DEVICE_ARG}${DEVICE_ID} -p:RuntimeIdentifier=${RUNTIME}
fi