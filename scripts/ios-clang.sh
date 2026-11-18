#!/usr/bin/env bash
set -euo pipefail
sdk="$(xcrun --sdk iphoneos --show-sdk-path)"
exec xcrun --sdk iphoneos clang -arch arm64 -isysroot "$sdk" -miphoneos-version-min=15.0 "$@"
