#!/bin/zsh
# Batch compile check / run an editor method: prints compile errors, warnings and [Batch] lines.
cd "$(dirname "$0")/../.."
/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$PWD/My project" -executeMethod ${1:-LegacyOfTemples.World.Editor.BatchTools.CompileCheck} -logFile /tmp/lot/cc.log
echo "exit=$?"
grep -E "error CS|warning CS" /tmp/lot/cc.log | sed 's/^.*Assets\/_Project\///' | sort -u | head -${2:-25}
grep -E "^\[Batch\]|Exception:" /tmp/lot/cc.log | sort -u | head -8
