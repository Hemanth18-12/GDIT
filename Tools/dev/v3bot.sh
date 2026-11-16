#!/bin/zsh
# Run the built game with the V3 test bot, hidden and in the background (does not take focus or show its window).
# usage: Tools/dev/v3bot.sh <mode flag, e.g. -v3Story> <output folder>
cd "$(dirname "$0")/../.."
mkdir -p /tmp/lot; rm -rf "$2"; mkdir -p "$2"; OUT="$(cd "$2" && pwd)"
open -n -g -j -W "Builds/LegacyOfTheTemple.app" --args -auditBot "$OUT" "$1" -noIntroVideo -introSkip -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile "$OUT/player.log"
echo "bot exit=$?"
