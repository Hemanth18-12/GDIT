#!/bin/zsh
# Run PlayMode tests by filter; prints each test's result and failure message.
cd "$(dirname "$0")/../.."
/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$PWD/My project" -runTests -testPlatform PlayMode -testFilter "$1" -testResults /tmp/lot/pt.xml -logFile /tmp/lot/pt.log
echo "exit=$?"
grep -E "error CS" /tmp/lot/pt.log | sed 's/^.*Assets\/_Project\///' | sort -u | head
python3 - <<'PY'
import xml.etree.ElementTree as ET
try:
    for tc in ET.parse('/tmp/lot/pt.xml').getroot().iter('test-case'):
        print(tc.get('result'), tc.get('name'), tc.get('duration','')[:5])
        m=tc.find('failure/message')
        if m is not None: print('   ', (m.text or '').strip()[:500])
except Exception as e: print('no results', e)
PY
