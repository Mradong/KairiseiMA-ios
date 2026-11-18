#!/usr/bin/env bash
set -euo pipefail
[[ "$(uname -s)" == Darwin ]] || { echo 'iOS native compilation requires macOS/Xcode.' >&2; exit 1; }
root="$(cd "$(dirname "$0")/.." && pwd)"
source_commit=d1a202eb539b99511e8a33f3975fac409ab545bf
dependency="$root/source-review/kairisei-ma-ch"
[[ "$(git -C "$dependency" rev-parse HEAD)" == "$source_commit" ]] || { echo 'Unexpected dependency commit.' >&2; exit 1; }
[[ -z "$(git -C "$dependency" status --porcelain)" ]] || { echo 'Dependency must be unmodified.' >&2; exit 1; }
[[ "$(go env GOVERSION)" == go1.25.1 ]] || { echo 'Use Go 1.25.1.' >&2; exit 1; }
output="$root/native/build/ios-arm64"
plugin="$root/ios-client/Assets/Plugins/iOS"
mkdir -p "$output" "$plugin"
chmod u+x "$root/scripts/ios-clang.sh"
export GOOS=ios GOARCH=arm64 CGO_ENABLED=1 GOTOOLCHAIN=local
export CC="$root/scripts/ios-clang.sh"
export CGO_CFLAGS='-miphoneos-version-min=15.0'
export CGO_LDFLAGS='-miphoneos-version-min=15.0'
(cd "$root/native/bridge" && go build -mod=readonly -buildvcs=false -trimpath -buildmode=c-archive -o "$output/libkairisei_battlecore.a" .)
[[ "$(xcrun lipo -archs "$output/libkairisei_battlecore.a")" == arm64 ]] || { echo 'Expected ARM64 static archive.' >&2; exit 1; }
cp "$output/libkairisei_battlecore.a" "$plugin/libkairisei_battlecore.a"
cp "$output/libkairisei_battlecore.h" "$plugin/libkairisei_battlecore.h"
NATIVE_ROOT="$root" node --input-type=module <<'JS'
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=process.env.NATIVE_ROOT;
const archive=fs.readFileSync(path.join(root,'native/build/ios-arm64/libkairisei_battlecore.a'));
fs.writeFileSync(path.join(root,'native/build/ios-arm64/native-build.json'),JSON.stringify({stage:'synthetic-native-validation',gameplayReady:false,go:'1.25.1',target:'ios/arm64',minimumOS:'15.0',dependencyCommit:'d1a202eb539b99511e8a33f3975fac409ab545bf',archiveBytes:archive.length,archiveSHA256:crypto.createHash('sha256').update(archive).digest('hex'),deviceTested:false},null,2)+'\n');
JS
