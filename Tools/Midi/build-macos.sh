#!/bin/sh
set -eu
cd "$(dirname "$0")/../.."
xcrun clang -dynamiclib -arch arm64 -arch x86_64 -mmacosx-version-min=11.0 \
  -Wall -Wextra -Wno-deprecated-declarations -framework CoreMIDI -framework CoreFoundation \
  Tools/Midi/MacMidi.c -o Assets/Plugins/macOS/XRMidiMac.dylib
