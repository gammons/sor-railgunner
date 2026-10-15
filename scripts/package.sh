#!/usr/bin/env bash
# Builds the Release plugin and packs dist/SoRRailgunner-<version>.zip.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

plugin_src="src/SoRCharacters/Plugin.cs"
module_src="src/SoRCharacters/Railgunner/RailgunnerModule.cs"
out_dir="src/SoRCharacters/bin/Release/net471"

version="$(sed -n 's/.*public const string Version = "\([^"]*\)".*/\1/p' "$plugin_src")"
if [[ -z "$version" ]]; then
    echo "Could not read the version from $plugin_src" >&2
    exit 1
fi

sounds_readme="$(sed -n 's/.*File\.WriteAllText(readme, "\([^"]*\)");.*/\1/p' "$module_src")"
if [[ -z "$sounds_readme" ]]; then
    echo "Could not read the sounds README text from $module_src" >&2
    exit 1
fi

dotnet build -c Release

for file in SoRCharacters.dll SoRCharacters.Core.dll; do
    if [[ ! -f "$out_dir/$file" ]]; then
        echo "Missing build output: $out_dir/$file" >&2
        exit 1
    fi
done

staging="$(mktemp -d)"
trap 'rm -rf "$staging"' EXIT

plugin_dir="$staging/BepInEx/plugins/SoRCharacters"
mkdir -p "$plugin_dir/sounds"
cp "$out_dir/SoRCharacters.dll" "$out_dir/SoRCharacters.Core.dll" "$plugin_dir/"
printf '%s' "$sounds_readme" > "$plugin_dir/sounds/README.txt"
cp README.md LICENSE "$staging/"

mkdir -p dist
zip_path="$root/dist/SoRRailgunner-$version.zip"
rm -f "$zip_path"
(cd "$staging" && zip -qr -X "$zip_path" BepInEx README.md LICENSE)

echo "Created $zip_path"
unzip -l "$zip_path"
