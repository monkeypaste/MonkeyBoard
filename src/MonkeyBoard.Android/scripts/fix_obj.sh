#!/bin/bash
# Workaround for:
# Xamarin.Android.EmbeddedResource.targets(39,5): Error XA1004 : There was an error opening <projectName>.aar. 
# The file is probably corrupt. Try deleting it and building again.
cd "$1"
rm -f $2.aar
echo dummy > blah.txt
zip -r $2.aar blah.txt
rm blah.txt
