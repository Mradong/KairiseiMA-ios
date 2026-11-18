#!/usr/bin/env bash
# Runs only on an ephemeral GitHub-hosted macOS runner. Credentials arrive
# through Actions Secrets, never through source files or command output.
set +x
set -euo pipefail
umask 077

[[ "$(uname -s)" == Darwin && "${GITHUB_ACTIONS:-}" == true ]] || {
  echo 'This helper requires a GitHub Actions macOS runner.' >&2; exit 1;
}
[[ "$(uname -m)" == x86_64 ]] || { echo 'This workflow requires the Intel macOS runner.' >&2; exit 1; }
: "${RUNNER_TEMP:?Missing runner temporary directory}"
private_dir="$RUNNER_TEMP/kairisei-unity-license"
mkdir -p "$private_dir"
mode="${1:-}"

if [[ "$mode" == return ]]; then
  [[ -f "$private_dir/personal-ulf-active" && -f "$private_dir/client-path" ]] || exit 0
  IFS= read -r client < "$private_dir/client-path"
  : "${UNITY_EMAIL:?Missing UNITY_EMAIL}" "${UNITY_PASSWORD:?Missing UNITY_PASSWORD}"
  if "$client" --return-ulf --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" > "$private_dir/return.log" 2>&1; then
    echo 'Returned the Personal ULF license acquired for this runner.'
  else
    echo '::warning::Unity could not confirm license return. Private activation logs were not uploaded.'
    exit 1
  fi
  exit 0
fi
[[ "$mode" == prepare ]] || { echo 'Expected prepare or return.' >&2; exit 1; }
: "${UNITY_EMAIL:?Add UNITY_EMAIL as an Actions Secret}"
: "${UNITY_PASSWORD:?Add UNITY_PASSWORD as an Actions Secret}"
: "${GITHUB_ENV:?Missing Actions environment file}"

hub_version=3.22.6
hub_app='/Applications/Unity Hub.app'
hub_executable="$hub_app/Contents/MacOS/Unity Hub"
if [[ ! -x "$hub_executable" ]]; then
  image="$private_dir/UnityHub-$hub_version-x64.dmg"
  mount_dir="$private_dir/hub-mount"
  mkdir -p "$mount_dir"
  curl --fail --location --silent --show-error --connect-timeout 30 --max-time 600 \
    --output "$image" "https://public-cdn.cloud.unity3d.com/hub/prod/$hub_version/UnityHubSetup-$hub_version-x64.dmg"
  hdiutil attach "$image" -nobrowse -readonly -quiet -mountpoint "$mount_dir"
  trap 'hdiutil detach "$mount_dir" -quiet || true' EXIT
  sudo ditto "$mount_dir/Unity Hub.app" "$hub_app"
  hdiutil detach "$mount_dir" -quiet
  trap - EXIT
fi
installed_version=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$hub_app/Contents/Info.plist")
[[ "$installed_version" == "$hub_version" ]] || {
  echo '::error::The installed Hub version differs from the pinned CLI-compatible version.'; exit 1;
}
codesign --verify --deep --strict "$hub_app"

client=''
while IFS= read -r candidate; do
  [[ -x "$candidate" ]] || continue
  [[ -n "$client" ]] || client="$candidate"
  if [[ "$candidate" == *UnityLicensingClient_V1* ]]; then client="$candidate"; break; fi
done < <(find "$hub_app/Contents" -type f -name Unity.Licensing.Client -print)
[[ -n "$client" ]] || { echo '::error::Official Hub licensing client not found.'; exit 1; }
printf '%s\n' "$client" > "$private_dir/client-path"
"$client" --help > "$private_dir/client-help.txt" 2>&1
grep -q -- '--activate-ulf' "$private_dir/client-help.txt" || {
  echo '::error::The official client does not support the requested activation method.'; exit 1;
}
if [[ ! -d '/Library/Application Support/Unity' ]]; then
  sudo install -d -m 755 -o "$(id -un)" -g "$(id -gn)" '/Library/Application Support/Unity'
fi
license_file='/Library/Application Support/Unity/Unity_lic.ulf'
if ! "$client" --activate-ulf --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" > "$private_dir/activate.log" 2>&1; then
  if [[ -s "$license_file" ]]; then touch "$private_dir/personal-ulf-active"; fi
  echo '::error::Official Personal ULF activation failed. No license or credential contents were printed or uploaded.'
  exit 1
fi
touch "$private_dir/personal-ulf-active"
[[ -s "$license_file" ]] || { echo '::error::The official client did not produce a ULF license.'; exit 1; }
# GameCI v4 parses this ULF to discover its serial even when its own activation
# step is skipped. Mask both values before passing the signed file to GameCI.
node --input-type=module - "$license_file" <<'NODE'
import fs from 'node:fs';
import crypto from 'node:crypto';
const license = fs.readFileSync(process.argv[2], 'utf8');
const data = license.match(/<DeveloperData\s+Value="([^"]+)"\s*\/>/);
if (!license.includes('<License') || !data) throw new Error('Unexpected official ULF structure');
const serial = Buffer.from(data[1], 'base64').toString('binary').slice(4);
if (!serial.startsWith('F')) throw new Error('Expected a Personal license; no paid serial was configured');
const escape = s => s.replace(/%/g, '%25').replace(/\r/g, '%0D').replace(/\n/g, '%0A');
console.log('::add-mask::' + escape(license));
console.log('::add-mask::' + escape(serial));
const delimiter = 'UNITY_LICENSE_' + crypto.randomBytes(16).toString('hex');
fs.appendFileSync(process.env.GITHUB_ENV, 'UNITY_LICENSE<<' + delimiter + '\n' + license + '\n' + delimiter + '\n');
NODE
echo 'Official Personal ULF activation completed for this runner.'
