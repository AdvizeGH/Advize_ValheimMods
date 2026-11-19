#!/usr/bin/env bash
# Local Linux build helper for PlantEverything.
#
# Upstream's Advize_PlantEverything.csproj targets net48 and expects the
# publicized game assemblies to already exist, so building it on Linux needs a
# few adjustments. Rather than editing the checked-in csproj (which upstream CI
# builds on Windows), we apply them via an MSBuild override file:
#
#   1. Microsoft.NETFramework.ReferenceAssemblies -> provides the net48
#      reference assemblies without a Windows Developer Pack.
#   2. ILRepack.Lib.MSBuild.Task 2.0.18.2 -> 2.0.48 -> the old task depends
#      on Microsoft.Build.Utilities.v4.0 and cannot load under modern MSBuild.
#
# The publicized game assemblies are also required: the mod reads private
# members (ZNetScene.m_namedPrefabs, ZNetView.m_forceDisableInit, ...) and
# upstream references assembly_*_publicized.dll. They are not shipped with the
# game or with BepInExPack, so we generate them from the game assemblies with a
# small Mono.Cecil tool (see tools/publicize).
#
# Usage: ./build-linux.sh [Release|Debug]

set -euo pipefail

CONFIG="${1:-Release}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

GAME_DIR="${VALHEIM_GAME_DIR:-/mnt/butterboard/SteamLibrary/steamapps/common/Valheim}"
MANAGED="$GAME_DIR/valheim_Data/Managed"
BEPINEX_CORE="${BEPINEX_CORE_DIR:-$HOME/.var/app/io.github.ebkr.r2modman/config/r2modmanPlus-local/Valheim/cache/denikson-BepInExPack_Valheim/5.4.2351/BepInExPack_Valheim/BepInEx/core}"
WORK="${PE_BUILD_DIR:-/tmp/opencode/pe-build}"
PUBLICIZE="$HERE/tools/publicize"

if [ ! -d "$MANAGED" ]; then
  echo "error: game Managed dir not found: $MANAGED" >&2
  echo "       set VALHEIM_GAME_DIR to your Valheim install" >&2
  exit 1
fi

# --- 1. generate publicized assemblies -------------------------------------
mkdir -p "$WORK/publicized"
for a in assembly_valheim assembly_guiutils; do
  if [ ! -f "$WORK/publicized/${a}_publicized.dll" ]; then
    echo "publicizing $a ..."
    (cd "$PUBLICIZE" && dotnet build -c Release -v q --nologo >/dev/null)
    (cd "$PUBLICIZE" && dotnet run -c Release --no-build -- \
      "$MANAGED/$a.dll" "$WORK/publicized/${a}_publicized.dll" "$MANAGED")
  fi
done

# --- 2. assemble a GamePath layout the csproj's HintPaths can resolve ------
# The csproj uses Windows separators; MSBuild on Unix normalises them, so the
# layout just needs to mirror the game's own directory structure.
GAMELAYOUT="$WORK/gamelayout"
rm -rf "$GAMELAYOUT"
mkdir -p "$GAMELAYOUT/BepInEx/core" "$GAMELAYOUT/valheim_Data/Managed/publicized_assemblies"
cp "$BEPINEX_CORE/0Harmony.dll" "$BEPINEX_CORE/BepInEx.dll" "$GAMELAYOUT/BepInEx/core/"
for f in "$MANAGED"/*.dll; do
  ln -sf "$f" "$GAMELAYOUT/valheim_Data/Managed/$(basename "$f")"
done
cp "$WORK/publicized/"*_publicized.dll "$GAMELAYOUT/valheim_Data/Managed/publicized_assemblies/"

# --- 3. build --------------------------------------------------------------
cat > "$WORK/local.props" <<'EOF'
<Project>
  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Update="ILRepack.Lib.MSBuild.Task" Version="2.0.48" />
  </ItemGroup>
</Project>
EOF

echo "building PlantEverything ($CONFIG) ..."
dotnet build "$HERE/Advize_PlantEverything.csproj" \
  -c "$CONFIG" \
  -p:GamePath="$GAMELAYOUT" \
  -p:CustomAfterMicrosoftCommonTargets="$WORK/local.props" \
  -v m --nologo

echo
echo "output: $HERE/bin/$CONFIG/Advize_PlantEverything.dll"
