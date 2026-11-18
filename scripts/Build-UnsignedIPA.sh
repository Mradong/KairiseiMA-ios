#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo 'Usage: Build-UnsignedIPA.sh <exported-Xcode-directory> <new-output-directory>' >&2
  exit 2
fi
if [[ "$(uname -s)" != Darwin ]]; then
  echo 'This step requires macOS and Xcode. It cannot compile an iOS executable on Windows.' >&2
  exit 2
fi
script_root="$(cd "$(dirname "$0")" && pwd -P)"
task_root="$(cd "$script_root/.." && pwd -P)"
xcode_root="$(cd "$1" && pwd -P)"
output_root="$(python3 -c 'import os,sys; print(os.path.realpath(sys.argv[1]))' "$2")"
case "$output_root" in "$task_root"/*) ;; *) echo 'Build output must be inside KairiseiMA-ios.' >&2; exit 2 ;; esac
if [[ -e "$output_root" ]]; then echo 'Output exists; refusing to mix or overwrite a prior build.' >&2; exit 2; fi
if [[ ! -d "$xcode_root/Unity-iPhone.xcodeproj" || ! -f "$xcode_root/recovery-build.json" ]]; then
  echo 'Expected a Unity Xcode export and its recovery-build.json stage record.' >&2
  exit 2
fi
command -v node >/dev/null
xcrun --sdk iphoneos --find clang >/dev/null
mkdir -p "$output_root"
xcodebuild -version > "$output_root/xcode-version.txt"
xcrun --sdk iphoneos --show-sdk-version > "$output_root/iphoneos-sdk-version.txt"
cp "$xcode_root/recovery-build.json" "$output_root/recovery-build.json"
xcodebuild archive \
  -project "$xcode_root/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone -configuration Release -sdk iphoneos \
  -destination 'generic/platform=iOS' \
  -archivePath "$output_root/Archive.xcarchive" \
  -derivedDataPath "$output_root/DerivedData" \
  CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO CODE_SIGN_IDENTITY= \
  COMPILER_INDEX_STORE_ENABLE=NO \
  > "$output_root/xcodebuild.log" 2>&1

shopt -s nullglob
applications=("$output_root/Archive.xcarchive/Products/Applications/"*.app)
if [[ ${#applications[@]} -ne 1 || ! -d "${applications[0]}" ]]; then
  echo 'Xcode did not produce exactly one iOS application.' >&2
  exit 1
fi
application="${applications[0]}"
plutil -convert json -o "$output_root/application-info.json" "$application/Info.plist"
node "$script_root/verify-ios-build.mjs" "$application" "$output_root/application-info.json" '17.4.1' > "$output_root/structural-validation.json"
if [[ -f "$application/embedded.mobileprovision" ]]; then
  echo 'An unexpected provisioning profile appeared in this unsigned build.' >&2
  exit 1
fi
mkdir -p "$output_root/Packaging/Payload"
ditto "$application" "$output_root/Packaging/Payload/$(basename "$application")"
ipa="$output_root/KairiseiMA-ios-connection-test-unsigned.ipa"
COPYFILE_DISABLE=1 ditto -c -k --norsrc --noextattr --keepParent "$output_root/Packaging/Payload" "$ipa"
unzip -tq "$ipa" > "$output_root/zip-validation.txt"
shasum -a 256 "$ipa" > "$output_root/ipa-sha256.txt"
printf '%s\n' "Unsigned connection-test IPA created: $ipa" 'This is not a restored playable game. It still requires local Apple-ID signing and device validation.'
