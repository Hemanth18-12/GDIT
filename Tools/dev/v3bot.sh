#!/bin/zsh
# Run the built game with the V3 test bot in batch mode: no window at all (screenshots are rendered off screen, without the HUD).
# usage: Tools/dev/v3bot.sh <mode flag, e.g. -v3Story> <output folder> [extra game arguments]
cd "$(dirname "$0")/../.."
mkdir -p /tmp/lot; rm -rf "$2"; mkdir -p "$2"; OUT="$(cd "$2" && pwd)"
"Builds/LegacyOfTheTemple.app/Contents/MacOS/My project" -batchmode -auditBot "$OUT" "$1" -noIntroVideo -introSkip -screen-width 1600 -screen-height 900 -logFile "$OUT/player.log" "${@:3}" >/dev/null 2>&1
echo "bot exit=$?"
