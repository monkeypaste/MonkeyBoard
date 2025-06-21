#!/bin/bash
# Workaround for:
# Xamarin.Android.EmbeddedResource.targets(39,5): Error XA1004 : There was an error opening <projectName>.aar. 
# The file is probably corrupt. Try deleting it and building again.
rm -f ../MonkeyBoard.Android.aar
cd "$1"
rm -f MonkeyBoard.Android.aar
# echo blah > blah.txt
# zip -r MonkeyBoard.Android.aar blah.txt
# rm blah.txt
cp /home/tkefauver/Desktop/MonkeyBoard.Android.aar .
