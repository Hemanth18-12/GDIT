"""Build the evaluation report and editable deck, using only Python's standard library.

The supplied Heritage Mystery PPTX is opened read-only. Its layouts, artwork,
embedded fonts and theme are retained; text and selected evidence panels change.
Render the HTML with the installed Chrome and the PPTX with installed Keynote.
"""
from pathlib import Path
from copy import deepcopy
import html
import json
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Evaluation'
QA = ROOT / 'Docs/QA/2026-11-25-evaluation'
TEMPLATE = Path('/Users/hemanth/Downloads/Heritage Mystery Project.pptx')
TEAM = 'Hemanth (S20240010236) · Aakash (S20240010245) · Sai Teja (S20240010136)'
E = html.escape
A = 'http://schemas.openxmlformats.org/drawingml/2006/main'
P = 'http://schemas.openxmlformats.org/presentationml/2006/main'
R = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
PKG = 'http://schemas.openxmlformats.org/package/2006/relationships'
CT = 'http://schemas.openxmlformats.org/package/2006/content-types'
NS = {'a': A, 'p': P, 'r': R}
for prefix, uri in NS.items():
    ET.register_namespace(prefix, uri)


def xml(node):
    return ET.tostring(node, encoding='utf-8', xml_declaration=True)


def read_results():
    path = QA / 'editor-tests.xml'
    if not path.exists():
        return {'label': 'Verification running', 'detail': 'The new combined PlayMode run has not yet produced its final XML.', 'short': 'Awaiting final run'}
    root = ET.parse(path).getroot()
    cases = root.findall('.//test-case')
    groups = {}
    for group in ['UpgradeVerificationTests', 'NewBazaarPlayTests']:
        selected = [c for c in cases if group in c.get('fullname', '')]
        groups[group] = {'total': len(selected), 'passed': sum(c.get('result') == 'Passed' for c in selected)}
    g, z = groups['UpgradeVerificationTests'], groups['NewBazaarPlayTests']
    detail = f"Fresh combined Editor PlayMode run: {root.get('passed')}/{root.get('total')} passed; {root.get('failed')} failed. Upgrade checks: {g['passed']}/{g['total']}; six-zone suite: {z['passed']}/{z['total']}."
    result = {'label': f"{root.get('passed')}/{root.get('total')} PlayMode checks passed", 'detail': detail,
              'short': f"{g['passed']}/{g['total']} upgrade checks\n{z['passed']}/{z['total']} zone checks", 'groups': groups,
              'total': root.get('total'), 'passed': root.get('passed'), 'failed': root.get('failed'),
              'start_utc': root.get('start-time'), 'end_utc': root.get('end-time'),
              'failures': [{'name':c.get('name'), 'message':c.findtext('./failure/message', '')} for c in cases if c.get('result') != 'Passed']}
    (QA / 'evaluation-results.json').write_text(json.dumps(result, indent=2))
    return result


def replace_text(shape, text, size=None):
    body = shape.find('p:txBody', NS)
    old = body.findall('a:p', NS)
    for para in old:
        body.remove(para)
    for i, line in enumerate(text.split('\n')):
        original = old[min(i, len(old) - 1)]
        para = ET.SubElement(body, f'{{{A}}}p')
        pp = original.find('a:pPr', NS)
        if pp is not None:
            para.append(deepcopy(pp))
        run = ET.SubElement(para, f'{{{A}}}r')
        rp = original.find('.//a:rPr', NS)
        rp = deepcopy(rp) if rp is not None else ET.Element(f'{{{A}}}rPr', {'lang': 'en-US'})
        if size:
            rp.set('sz', str(size * 100))
        run.append(rp)
        ET.SubElement(run, f'{{{A}}}t').text = line
    bp = body.find('a:bodyPr', NS)
    for child in list(bp):
        if child.tag in {f'{{{A}}}spAutoFit', f'{{{A}}}noAutofit', f'{{{A}}}normAutofit'}:
            bp.remove(child)
    ET.SubElement(bp, f'{{{A}}}normAutofit')


def notes_xml(text):
    root = ET.Element(f'{{{P}}}notes')
    cs = ET.SubElement(root, f'{{{P}}}cSld')
    tree = ET.SubElement(cs, f'{{{P}}}spTree')
    nv = ET.SubElement(tree, f'{{{P}}}nvGrpSpPr')
    ET.SubElement(nv, f'{{{P}}}cNvPr', {'id':'1','name':''})
    ET.SubElement(nv, f'{{{P}}}cNvGrpSpPr')
    ET.SubElement(nv, f'{{{P}}}nvPr')
    ET.SubElement(tree, f'{{{P}}}grpSpPr')
    sp = ET.SubElement(tree, f'{{{P}}}sp')
    nv = ET.SubElement(sp, f'{{{P}}}nvSpPr')
    ET.SubElement(nv, f'{{{P}}}cNvPr', {'id':'2','name':'Presenter notes'})
    ET.SubElement(nv, f'{{{P}}}cNvSpPr')
    ET.SubElement(ET.SubElement(nv, f'{{{P}}}nvPr'), f'{{{P}}}ph', {'type':'body','idx':'1'})
    ET.SubElement(sp, f'{{{P}}}spPr')
    tx = ET.SubElement(sp, f'{{{P}}}txBody')
    ET.SubElement(tx, f'{{{A}}}bodyPr')
    ET.SubElement(tx, f'{{{A}}}lstStyle')
    for line in text.split('\n'):
        para = ET.SubElement(tx, f'{{{A}}}p')
        run = ET.SubElement(para, f'{{{A}}}r')
        ET.SubElement(run, f'{{{A}}}t').text = line
    ET.SubElement(root, f'{{{P}}}clrMapOvr').append(ET.Element(f'{{{A}}}masterClrMapping'))
    return xml(root)


def diagram_shape(tree, ident, text, x, y, w, h, color='302419', size=27, ink='F6E8C8'):
    sp = ET.SubElement(tree, f'{{{P}}}sp')
    nv = ET.SubElement(sp, f'{{{P}}}nvSpPr')
    ET.SubElement(nv, f'{{{P}}}cNvPr', {'id':str(ident),'name':f'Editable diagram {ident}'})
    ET.SubElement(nv, f'{{{P}}}cNvSpPr')
    ET.SubElement(nv, f'{{{P}}}nvPr')
    pr = ET.SubElement(sp, f'{{{P}}}spPr')
    xf = ET.SubElement(pr, f'{{{A}}}xfrm')
    ET.SubElement(xf, f'{{{A}}}off', {'x':str(int(x*914400)), 'y':str(int(y*914400))})
    ET.SubElement(xf, f'{{{A}}}ext', {'cx':str(int(w*914400)), 'cy':str(int(h*914400))})
    ET.SubElement(pr, f'{{{A}}}prstGeom', {'prst':'rect'}).append(ET.Element(f'{{{A}}}avLst'))
    ET.SubElement(ET.SubElement(pr, f'{{{A}}}solidFill'), f'{{{A}}}srgbClr', {'val':color})
    ln=ET.SubElement(pr, f'{{{A}}}ln', {'w':'19050'})
    ET.SubElement(ET.SubElement(ln, f'{{{A}}}solidFill'), f'{{{A}}}srgbClr', {'val':'C89B52'})
    tx=ET.SubElement(sp, f'{{{P}}}txBody')
    ET.SubElement(tx,f'{{{A}}}bodyPr',{'anchor':'ctr','lIns':'110000','rIns':'110000'})
    ET.SubElement(tx,f'{{{A}}}lstStyle')
    for line in text.split('\n'):
        p=ET.SubElement(tx,f'{{{A}}}p')
        ET.SubElement(p,f'{{{A}}}pPr',{'algn':'ctr'})
        r=ET.SubElement(p,f'{{{A}}}r')
        rp=ET.SubElement(r,f'{{{A}}}rPr',{'sz':str(size*100),'lang':'en-US'})
        ET.SubElement(ET.SubElement(rp,f'{{{A}}}solidFill'),f'{{{A}}}srgbClr',{'val':ink})
        ET.SubElement(rp,f'{{{A}}}latin',{'typeface':'Arial'})
        ET.SubElement(r,f'{{{A}}}t').text=line
    return sp


def diagram_picture(tree, ident, relationship, x, y, w, h):
    pic=ET.SubElement(tree,f'{{{P}}}pic')
    nv=ET.SubElement(pic,f'{{{P}}}nvPicPr')
    ET.SubElement(nv,f'{{{P}}}cNvPr',{'id':str(ident),'name':'Current source-run Courtyard capture'})
    ET.SubElement(nv,f'{{{P}}}cNvPicPr').append(ET.Element(f'{{{A}}}picLocks',{'noChangeAspect':'1'}))
    ET.SubElement(nv,f'{{{P}}}nvPr')
    fill=ET.SubElement(pic,f'{{{P}}}blipFill')
    ET.SubElement(fill,f'{{{A}}}blip',{f'{{{R}}}embed':relationship})
    ET.SubElement(fill,f'{{{A}}}stretch').append(ET.Element(f'{{{A}}}fillRect'))
    sp=ET.SubElement(pic,f'{{{P}}}spPr')
    xf=ET.SubElement(sp,f'{{{A}}}xfrm')
    ET.SubElement(xf,f'{{{A}}}off',{'x':str(int(x*914400)),'y':str(int(y*914400))})
    ET.SubElement(xf,f'{{{A}}}ext',{'cx':str(int(w*914400)),'cy':str(int(h*914400))})
    ET.SubElement(sp,f'{{{A}}}prstGeom',{'prst':'rect'}).append(ET.Element(f'{{{A}}}avLst'))


def make_deck(result):
    slides = {
        1: ['UNITY 6 · CULTURAL INVESTIGATION · PROJECT DEFENSE', 'LEGACY OF\nTHE TEMPLE', 'Explore a temple-inspired world. Read the evidence.\nRecover the community’s fictional heritage objects.', TEAM + '\nDEPT. OF CSE · IIIT SRI CITY · NOVEMBER 2026'],
        2: ['01 / PROBLEM & MOTIVATION', 'HERITAGE CAN FEEL\nDISTANT.', 'Static descriptions can explain a place. Our game asks the player to observe, question and act, connecting an investigation to the meaning of community heritage.', 'OBSERVATION → UNDERSTANDING → CARE', 'LEGACY OF THE TEMPLE · 02'],
        3: ['02 / SIX-ZONE WORLD', 'ONE INVESTIGATION. SIX CONNECTED ZONES.', '01–02 · BAZAAR / COURTYARD', 'Follow a courier · reconstruct a lamp', '03–04 · VAULTS / STEPWELL', 'Fit gears · raise a floating raft', '05–06 · SUN WALL / JUNGLE', 'Reflect sunlight · follow the escape route', '03'],
        4: ['03 / TECHNICAL ARCHITECTURE', 'FOUR SYSTEMS. ONE PLAYABLE WORLD.', 'UNITY 6\nC# + URP', 'WORLD', 'modular temple + six zones', 'GAMEPLAY', 'clues + physical puzzles', 'INTERFACE', 'HUD + dialogue + retry', 'AUDIO', 'five volume controls', '04'],
        5: ['04 / TEMPLE REPLACEMENT', 'MODULAR TEMPLE.\nSOLID BOUNDARY.\nCLEARER WORLD.', 'Runtime raw temple spawns were replaced by a modular Kalinga-inspired structure. The Courtyard doorway now forms a solid investigation boundary.', 'SOURCE CHANGE · KALINGA BUILDER + GATE SEAL', '05'],
        6: ['05 / STORY & CULTURAL CARE', 'A TEMPLE.\nA THEFT.\nA TRAIL OF EVIDENCE.', 'Aryan speaks with Pujari Ananda and eyewitness Meera. A broken lock, bootprints and red cloth lead towards Bhaskar and Kishore, and the recovery of fictional heritage objects.', 'FICTIONAL OBJECTS · VESSEL / ROSETTE / MANUSCRIPT', '06'],
        7: ['06 / GAMEPLAY LOOP', 'THE INVESTIGATION HAS A RHYTHM', '01', '02', '03', '04', '05', 'EXPLORE', 'orient to the objective', 'INSPECT', 'read clues and dialogue', 'SOLVE', 'test physical puzzles', 'CONFRONT', 'resolve the encounter', 'RECOVER', 'restore heritage objects', 'Movement, evidence, puzzles and recovery give each zone a clear purpose.', '07'],
        8: ['07 / NAVIGATION, RECOVERY & AUDIO', 'HELP THE PLAYER STAY WITH THE TASK', 'SQUARE MINIMAP', 'bottom-left player heading\nobjective beacon + distance', 'LOCAL RECOVERY', 'Jeep [R] repair + road reset\nretry at the section checkpoint', 'FIVE AUDIO CONTROLS', 'master · music · ambience\neffects · voice', 'Devotional-style music, temple ambience, subtitles and voice hooks support the setting.', '08'],
        9: ['08 / PHYSICAL PUZZLES', 'TURN AN IDEA INTO ACTION.', 'In the Sun Wall zone, rotating mirrors redirects a light beam towards the chariot wheel. Vault gears and the stepwell’s sluices offer other visible cause-and-effect puzzles.', 'GAME CAPTURE · SUN WALL MIRROR PUZZLE', '09'],
        10: ['09 / PLAYER EXPERIENCE', 'GET BACK\nTO THE TASK.', 'Press [R] to repair and reset the Jeep upright on the road. Section retries use a local checkpoint, keeping investigation progress close to the current task.', 'AUDIO', 'Master / Music / Ambience / SFX / Voice', '10'],
        11: ['10 / STUDY METHODOLOGY', 'HOW WE WILL EVALUATE THE EXPERIENCE', 'CONSENT', 'Adults 18+ · voluntary\nseparate recording consent', 'PLAY', '30-minute guided route\nnote time, hints and barriers', 'MEASURE', 'six transfer questions\n1–5 ratings + comments', 'PLAYER RESULTS PENDING · TECHNICAL QA REPORTED SEPARATELY', '11'],
        12: ['11 / TECHNICAL RESULTS & LIMITS', 'VERIFY THE SYSTEM.\nTHEN STUDY THE PLAYER.', 'EDITOR PLAYMODE', result['short'] + '\nFresh run · 25 Nov 2026', 'STILL TO ESTABLISH', 'Player understanding\nEnjoyment + usability\nCurrent release-build parity', 'TECHNICAL CHECKS DO NOT MEASURE LEARNING', '12'],
        13: ['12 / CONCLUSION & LIVE DEMO', 'DISCOVERY\nCREATES CARE.', 'Legacy of the Temple combines investigation, physical puzzles and a heritage-protection story. The next evaluation pairs a reproducible game demo with real player feedback.', 'EVERY ARTIFACT CARRIES A MEMORY.', 'THANK YOU · Q&A'],
    }
    notes = [
        f'We are {TEAM}. Legacy of the Temple is a desktop, third-person Unity game inspired by Odisha temple architecture. It is not a validated XR reconstruction. The original slide template supplies the typography, gold and brown palette and illustrative cover art; that artwork is not a game screenshot.',
        'The project explores whether active observation and solving a problem can make a heritage theme more engaging. This is a design motivation, not an established learning outcome. The planned study measures both puzzle understanding and player experience.',
        'The six runtime zones are Village Bazaar and Tea Lane, Temple Gate and Courtyard, Underground Storage Vaults, Stepwell and Monsoon Shrine, Sun Wall and Chariot Plaza, and Jungle Road and River Ruins. They share the TempleRing world. Avoid promising that every zone is equally close to the centre: the jungle includes an extended road. The strip contains actual Editor captures from zones 1, 4 and 5.',
        'NewWorldDirector coordinates the six-zone environment. KalingaTempleBuilder constructs the modular temple. Gameplay uses C# systems, CharacterController movement, PhysX vehicle components and interactive puzzle states. UI supplies the minimap, objectives, dialogue, subtitles and local retries. AudioManager and AudioDirector support five user-facing volume controls. This is a logical architecture diagram, not a performance profile.',
        'The correction replaces runtime raw temple spawns, rather than deleting the original source asset. A solid Courtyard doorway prevents an unwanted portal transition. This slide uses a current source-run capture. Do not claim photo-scanned construction, automatic LODs, no hovering objects anywhere or guaranteed frame rate: these are not established by the evidence used here. Original Blender and Heritage Mystery sources remain read-only.',
        'The protected story objects are a fictional brass ceremonial vessel, carved sandstone rosette and palm-leaf manuscript. Investigator Aryan, Pujari Ananda, Meera, Bhaskar and Kishore are the core cast. The shrine idol remains a respectful background element, never stolen, carried or touched. The original template’s wording about a stolen sacred artifact has been corrected.',
        'The player explores, inspects clues, solves environmental puzzles, resolves an encounter and recovers objects. Evidence connects dialogue and puzzle actions to the story. In the intended ending, the stolen pieces return to their community; the message is protecting memory and heritage. Use the live game to demonstrate the current encounter implementation.',
        'The actual minimap is bottom-left, as requested in the latest task summary. It uses heading, objective and distance information. Jeep [R] calls road reset and repair. DawnClock retries from a saved local checkpoint. The five controls are master, music, ambience, sound effects and voice. The tests check selected runtime values and recovery state; they do not prove every slider interaction, every map marker or subjective sound quality.',
        'Demonstrate the Sun Wall mirrors: rotate, observe the beam, then align the target. Additional examples are vault pressure plates, a gear train and rotating discs, and stepwell sluices with a rising raft. These are game representations; do not present the simplified simulation as a rigorous experiment in optics or hydraulics.',
        'The picture is a world capture from the TempleRing verification scene. Batch/windowless capture can omit Canvas UI, so it must not be offered as visual proof that the minimap renders. The runtime upgrade test checks MinimapHud.Visible. Show the corner HUD and [R] input during the interactive demo. The retry test invokes the recovery method directly rather than pressing a retry button.',
        'Player-study status: pending, as confirmed by the team. Use the existing information sheet, consent form and after-play questionnaire. Adults aged 18 or above may volunteer; recording is optional. A suggested 30-minute route samples courtyard reconstruction, Sun Wall mirrors, stepwell water and evidence deduction; record which tasks each player actually reached. Score six questions covering shadows, reflection, flotation, evidence, cyclic sequences and heritage value. Rate enjoyment, difficulty, controls, goals, hints, voice/subtitles, puzzle understanding, heritage care, continued play and recommendation from 1 to 5. The local script currently creates the after-play form only. A paired pre/post design needs a checked baseline form and matched participant IDs before collection.',
        result['detail'] + ' Evidence: Docs/QA/2026-11-25-evaluation/editor-tests.xml. The installed app was launched separately and its images saved under app/. It is an existing binary and cannot establish parity with newer source changes. No new participant responses, learning effect, survey means or guaranteed 60 FPS result is claimed. Historical frame samples are small Editor samples at 640×480; this run is functional QA, not a controlled benchmark. Review evidence notes for failures and capture limitations.',
        'For the live demo: show the sealed Courtyard doorway, bottom-left minimap, one physical puzzle, Jeep recovery and the five audio controls. Keep the protected shrine element untouched. End with the message: Every temple holds a story. Every artifact carries a memory. Protecting heritage means protecting our history. Invite questions about design, implementation and the pending study.',
    ]
    # Ten-slide evaluation sequence from the new brief, using the supplied template.
    order=[1,2,7,4,9,10,8,11,12,13]
    slides[4]=['03 / WORLD LAYOUT','SIX ZONES AROUND THE TEMPLE HUB.','TEMPLE\nHUB','NORTH · Z6','Jungle Road + River Ruins','EAST · Z5','Sun Wall + Chariot Plaza','WEST · Z4','Monsoon Stepwell','SOUTH · Z1','Village Bazaar + Tea Lane','04']
    slides[8]=['06 / UI & NAVIGATION','KEEP THE INVESTIGATION READABLE','SQUARE MINIMAP','bottom-left player heading\nobjective beacon + distance','OBJECTIVES','active task + clue markers\nzone and vehicle context','LOCAL CHECKPOINTS','retry near the current task\nkeep the player in the scene','Runtime visibility was checked; the batch image does not prove HUD rendering.','07']
    slides[9]=['04 / PHYSICAL PUZZLE TOOLKIT','TURN AN IDEA INTO ACTION.','Redirect light with mirrors. Fit a gear train and sequence lock in the vaults. Open sluices to raise the stepwell raft. Each action gives feedback that the player can observe.','EDITABLE SCHEMATIC · NOT A GAME CAPTURE','05']
    slides[10]=['05 / JEEP MECHANICS','RECOVER.\nREPAIR.\nCONTINUE.','The PhysX Jeep uses wheel colliders. Press [R] after a crash to repair damage and reset upright on a road position. The upgrade test checks the recovery method’s resulting state.','RECOVERY','upright position + cleared damage','06']
    slides[11]=['07 / AUDIO & CULTURAL SOUNDSCAPES','A CALMER SOUND FOR THE INVESTIGATION','MUSIC','devotional-style music\nsoft melodic layers','AMBIENCE','temple bells + birds\nbackground atmosphere','CONTROL','master · music\nambience · effects · voice','FIVE CONTROLS · SUBTITLES · VOICE-READY DIALOGUE','08']
    slides[12][0]='08 / TECHNICAL EXECUTION & QA'
    slides[12][-1]='09'
    slides[13][0]='09 / CONCLUSION & LIVE DEMO'
    slides[7][0]='02 / GAME DESIGN & CORE LOOP'
    slides[7][-1]='03'
    notes[3]='The six zones share the TempleRing world. This is a schematic layout, not a measured map: north is the jungle route, east the Sun Wall, west the stepwell and south the bazaar. Zone 2 occupies the Courtyard plaza and Zone 3 the deeper vaults. The central construction is Kalinga-inspired. The newer source replaces runtime raw temple mesh spawns with modular construction; original source assets are preserved. The solid Courtyard investigation boundary is checked by the upgrade test. Modular construction makes it easier to reason about placement and boundaries, but this evidence does not establish automatic LODs, photo-scanning or that every prop is free of hovering. The older app still showed the dark doorway during a separate launch, so a current release build needs its own parity check.'
    notes[8]='This schematic explains the toolkit rather than showing a successful game capture. In the Sun Wall, the player turns mirrors and observes the beam direction before aligning the target. In the vaults, pressure plates, a gear train and rotating discs connect movement and object placement to progress. At the stepwell, sluice controls change water level and the raft rises. A sequence lock asks the player to apply an ordered clue rather than guess indefinitely. These mechanics provide immediate game feedback. They are simplified representations, so we should not describe them as a calibrated optics or hydraulics laboratory. The six-zone suite passed its selected puzzle and recovery scenarios. A separate visual issue was observed in the saved Sun Wall camera view, where geometry obstructed the image; that capture is not used as proof of a visible beam. The player study will test whether people understand the intended ideas after play.'
    notes[9]='The Jeep is a movement and navigation system for the road sections. It uses Unity PhysX wheel colliders, which represent wheel contact and suspension behaviour. Recovery is important because a flipped or stuck vehicle can interrupt the investigation. Pressing R calls repair and road reset: damage state is cleared, orientation returns upright, and the vehicle is repositioned on a road location. The upgrade test begins from a deliberately tilted position, calls that method, and checks that the vehicle is upright and damage is zero. The zone scenario also exercises Jeep entry, driving and exit. These checks cover selected behaviour; a method call does not prove every keyboard context, collision or possible recovery location. The editable diagram shows the intended state transition, not fabricated before-and-after screenshots. During a live demo, demonstrate a safe recovery on an open road and confirm that movement resumes.'
    notes[7]='The square minimap is anchored at the bottom-left. It is intended to show player heading, the current objective and local navigation context, alongside the normal objective panel. A yellow beacon and distance help the player decide which direction to move. A local checkpoint keeps retry close to the current section rather than forcing a long return from the initial temple spawn. The automated upgrade check confirms the visibility state and the local recovery position while staying in TempleRing. The batch screenshot omits Canvas UI, so it is not visual proof that the map renders correctly. The separate existing app image also lacks the map and still shows the old doorway. We keep those observations distinct from current-source test results. The next interactive check should observe the actual HUD, move and turn the player, and use the retry control through its normal interface.'
    notes[10]='The soundtrack aims for a calm devotional-style atmosphere that supports investigation. The implementation summary describes regenerated temple music and ambience, with melodic textures, bells and dawn birds. These are game soundscapes, not a claim to reproduce an authenticated ritual recording or a particular raga. The interface has five volume controls: master, music, ambience, sound effects and voice. This lets a player reduce music while keeping dialogue or effects audible. Subtitles and voice-ready dialogue hooks also support understanding. The upgrade test sets selected runtime volume values and confirms that they reach the audio systems. That is a technical state check rather than a listening study or a complete test of every slider gesture. During the demo, briefly vary music and voice, then restore comfortable levels. The pending player questionnaire asks whether voices and subtitles are clear and whether the game’s presentation helps players follow the task.'
    # Keep each script suitable for roughly 60–90 seconds at an unhurried pace.
    additions={
        0:' Our core loop is explore, inspect evidence, solve a physical puzzle, resolve an encounter and recover a fictional heritage object. We use the setting to connect a task with the idea that objects carry community memory. The current evaluation includes technical gameplay checks and a prepared study protocol. Technical passes do not tell us how much players learned or how much they enjoyed the game. This presentation separates those questions. The study report follows the supplied example’s structure but contains only our own project content. We will demonstrate the game, explain the changes and identify what still needs evaluation.',
        1:' In this game, a clue has a purpose: it supports a decision or opens a path. Dialogue connects that action with the story of a community protecting its memory. The recovered vessel, rosette and manuscript are fictional, and the shrine idol is a background element that the player never handles. We chose investigation because it gives the player a reason to look carefully. That is our design hypothesis. We still need real participant responses to know whether the tasks are readable and whether the heritage message comes through. We will use six transfer questions, ratings and open comments rather than assume that a completed puzzle automatically means understanding.',
        6:' For example, inspecting the Courtyard supplies a clue, and solving a reconstruction task changes what becomes possible next. Different zones vary the physical action while keeping the investigation loop familiar. The minimap, prompts and checkpoint recovery are intended to help the player remain focused on the current task. The six-zone automated run checks selected routes, interactions, encounters and recovery conditions, but it does not replace a first-time player. A person may misunderstand a prompt even when the underlying system works. Our proposed after-play study asks about next-objective clarity, control difficulty and puzzle understanding so that feedback can point to a specific part of the loop.',
        11:' The fresh result is eight passes out of eight checks: two upgrade tests and six zone scenarios. There are no new C# compile diagnostics in the captured run, but we do not turn that into a blanket zero-warning claim about every subsystem. We do not claim a guaranteed frame rate or publish a build size as a performance measure. The existing app was launched separately; its visible differences mean it is not evidence of current-source parity. The prepared player study is still pending, so there are no enjoyment means or learning scores to present.',
        12:' A clear demo order is the temple boundary, a physical puzzle, the navigation UI, Jeep recovery and the volume controls. We will explain what each action demonstrates and avoid using a screenshot as proof of something it does not show. Our next step is to run an adult pilot with consent, note which tasks each player reached, and collect six knowledge answers, ten ratings and open feedback. A later baseline and comparison condition could support stronger learning claims. The aim is a dependable experience where careful observation and respect for heritage belong in the same gameplay loop.',
    }
    for i, extra in additions.items():notes[i]+=extra
    with zipfile.ZipFile(TEMPLATE) as src:
        data = {name: src.read(name) for name in src.namelist()}
    types = ET.fromstring(data['[Content_Types].xml'])
    for number in order:
        replacements=slides[number]
        key = f'ppt/slides/slide{number}.xml'
        root = ET.fromstring(data[key])
        shapes = [sp for sp in root.findall('.//p:sp', NS) if sp.findall('.//a:t', NS)]
        assert len(shapes) == len(replacements), (number, len(shapes), len(replacements))
        for index, (shape, text) in enumerate(zip(shapes, replacements)):
            size = 17 if number == 1 and index == 3 else None
            if number == 3 and index in [2,4,6]: size = 24
            if number == 3 and index in [3,5,7]: size = 20
            if number == 5 and index == 1: size = 60
            if number == 8 and index in [3,5,7]: size = 26
            if number == 10 and index == 4: size = 21
            if number == 11 and index in [3,5,7]: size = 22
            if number == 11 and index == 8: size = 24
            replace_text(shape, text, size)
            if number == 1 and index == 3:
                shape.find('.//a:xfrm/a:ext', NS).set('cx', '16000000')
                shape.find('.//a:xfrm/a:ext', NS).set('cy', '650000')
                shape.find('.//a:xfrm/a:off', NS).set('y', '9060000')
            if number == 11 and index in [3,5,7]:
                shape.find('.//a:xfrm/a:ext', NS).set('cy', '1050000')
                shape.find('.//a:xfrm/a:ext', NS).set('cx', '3333750')
            if number == 12 and index in [3,5]:
                shape.find('.//a:xfrm/a:ext', NS).set('cy', '1900000')
        tree=root.find('p:cSld/p:spTree',NS)
        if number==4:
            diagram_picture(tree,1100,'rIdEvalTemple',6.9,4.0,6.2,3.4875)
            diagram_shape(tree,1101,'TEMPLE HUB · CURRENT SOURCE CAPTURE',6.6,6.65,6.8,0.42,size=17)
            diagram_shape(tree,1000,'Z2 · COURTYARD PLAZA\nZ3 · UNDERGROUND VAULTS',6.5,7.0,7,1.1,size=24)
        if number==12:
            diagram_shape(tree,1100,'Raw temple spawns → Modular Kalinga construction\nLong restart → Local section checkpoint\nTilted / damaged Jeep → Upright road reset',8.3,6.3,10.1,1.65,color='E5D3AF',size=21,ink='302419')
            diagram_shape(tree,1101,'FPS / build-size benchmark: pending',8.3,8.15,10.1,0.4,color='EBDCB9',size=17,ink='302419')
        if number in [9,10]:
            diagram_shape(tree,1000,'PHYSICAL PUZZLE TOOLKIT' if number==9 else 'JEEP RECOVERY',0.5,1.6,8.9,0.7,color='211710',size=25)
            labels=['LIGHT SOURCE','ROTATE MIRRORS','ALIGN THE TARGET','Gears · Sluices · Sequence locks'] if number==9 else ['CRASH / TILT','PRESS [R]','UPRIGHT ON ROAD','Damage state cleared · Continue driving']
            for j,label in enumerate(labels[:3]):
                diagram_shape(tree,1001+j,label,1.1,2.7+j*1.75,7.5,1.05,size=29)
                if j<2:diagram_shape(tree,1010+j,'↓',4.1,3.8+j*1.75,1.4,0.55,color='211710',size=27)
            diagram_shape(tree,1020,labels[3],0.8,8.5,8.3,0.65,color='211710',size=22)
        data[key] = xml(root)
        relkey = f'ppt/slides/_rels/slide{number}.xml.rels'
        rel = ET.fromstring(data[relkey])
        if number==4:
            ET.SubElement(rel,f'{{{PKG}}}Relationship',{'Id':'rIdEvalTemple','Type':R+'/image','Target':'../media/image4.png'})
        ET.SubElement(rel, f'{{{PKG}}}Relationship', {'Id':'rIdEvalNotes', 'Type':R+'/notesSlide', 'Target':f'../notesSlides/notesSlide{number}.xml'})
        data[relkey] = xml(rel)
        data[f'ppt/notesSlides/notesSlide{number}.xml'] = notes_xml(notes[number-1])
        nrel = ET.Element(f'{{{PKG}}}Relationships')
        ET.SubElement(nrel, f'{{{PKG}}}Relationship', {'Id':'rId1','Type':R+'/slide','Target':f'../slides/slide{number}.xml'})
        data[f'ppt/notesSlides/_rels/notesSlide{number}.xml.rels'] = xml(nrel)
        ET.SubElement(types, f'{{{CT}}}Override', {'PartName':f'/ppt/notesSlides/notesSlide{number}.xml','ContentType':'application/vnd.openxmlformats-officedocument.presentationml.notesSlide+xml'})
    # Selected panels use project captures, while the supplied illustrative cover remains.
    for media, path in {
        'image4.png': QA/'editor-courtyard-sealed.png',
    }.items():
        if path.exists():
            data['ppt/media/'+media] = path.read_bytes()
    data['[Content_Types].xml'] = xml(types)
    # Reorder the presentation without altering the original file.
    presentation=ET.fromstring(data['ppt/presentation.xml'])
    ids=presentation.find('p:sldIdLst',NS)
    original=list(ids)
    ids[:]=[original[n-1] for n in order]
    data['ppt/presentation.xml']=xml(presentation)
    for number in set(range(1,14))-set(order):
        for key in [f'ppt/slides/slide{number}.xml',f'ppt/slides/_rels/slide{number}.xml.rels']:
            data.pop(key,None)
    presentation_rels=ET.fromstring(data['ppt/_rels/presentation.xml.rels'])
    for item in list(presentation_rels):
        target=item.get('Target','')
        if target.startswith('slides/slide') and int(target.removeprefix('slides/slide').removesuffix('.xml')) not in order:
            presentation_rels.remove(item)
    data['ppt/_rels/presentation.xml.rels']=xml(presentation_rels)
    for item in list(types):
        part=item.get('PartName','')
        if part.startswith('/ppt/slides/slide') and part.endswith('.xml'):
            if int(part.removeprefix('/ppt/slides/slide').removesuffix('.xml')) not in order:types.remove(item)
    data['[Content_Types].xml']=xml(types)
    # Update editable package metadata, avoiding stale prototype / date information.
    core = ET.fromstring(data['docProps/core.xml'])
    meta = {'title':'Legacy of the Temple — Project Evaluation', 'subject':'Unity game implementation, study methodology and technical evidence', 'creator':TEAM, 'description':'Adapted from the supplied Heritage Mystery template. Player study pending. Technical results are scoped to their evidence.'}
    for name, value in meta.items():
        node = core.find('{http://purl.org/dc/elements/1.1/}'+name)
        if node is not None: node.text = value
    data['docProps/core.xml'] = xml(core)
    with zipfile.ZipFile(OUT/'Legacy_of_the_Temple_Evaluation.pptx', 'w', zipfile.ZIP_DEFLATED) as dst:
        for key, value in data.items(): dst.writestr(key,value)
    (OUT/'source/Presentation_Speaker_Notes.txt').write_text('\n\n'.join(f'SLIDE {i+1}\n{notes[n-1]}' for i,n in enumerate(order)))


def para(text): return '<p>'+E(text)+'</p>'


def table(head, rows):
    return '<table><thead><tr>'+''.join('<th>'+E(c)+'</th>' for c in head)+'</tr></thead><tbody>'+''.join('<tr>'+''.join('<td>'+E(c)+'</td>' for c in row)+'</tr>' for row in rows)+'</tbody></table>'


def make_report(result):
    pages=[]
    header='<h1>Study Methodology and Results</h1><div class="meta">Legacy of the Temple · '+E(TEAM)+'<br>Dept. of CSE, IIIT Sri City · November 2026</div>'
    pages.append(header + '<div class="summary"><b>Summary.</b> Legacy of the Temple is a third-person cultural investigation game built in Unity 6000.6.8f1 with URP. Players examine clues, solve physical puzzles and recover fictional heritage objects in a temple-inspired environment. '+E(result['detail'])+' A separate launch captured the existing macOS app. These are technical observations; the player study is pending. No participant scores, enjoyment ratings or learning gains are reported.</div>'
        + '<h2>1. Method</h2><h3>1.1 Design</h3>'
        + para('The evaluation has two parts: functional verification of the current game systems, and a planned single-session study with an after-play measure. The player study will examine understanding of puzzle ideas, usability and attitudes towards protecting heritage. Technical test passes will be reported separately from participant responses.')
        + '<h3>1.2 Participants and ethics</h3>'
        + para('No real participant-response dataset was supplied for this report. Recruitment is planned among consenting adults aged 18 or above. Each participant should read the existing information sheet and sign the consent form, with separate optional permission for screenshots or video. Participation is voluntary and may stop at any time. Report the actual recruited, excluded and analysed counts after collection; do not treat the example-answer document as collected data.')
        + '<h3>1.3 Procedure</h3><ol>'
        + '<li>Explain the study, confirm eligibility and consent, and assign a participant code. Keep any roll-number linkage separate from published results.</li>'
        + '<li>Provide a short controls tutorial, then a suggested 30-minute route sampling courtyard reconstruction, Sun Wall mirrors, stepwell water and evidence deduction. Record which tasks each participant reaches, help given, retries and barriers.</li>'
        + '<li>Complete the after-play form: six knowledge questions, confidence, learning-habit items, ten game-rating statements, a preference question and two open questions.</li>'
        + '<li>Score answers and summarise ratings and comments. Questions tied to unseen tasks must be identified when interpreting outcomes.</li></ol>'
        + '<h3>1.4 Measures</h3>'
        + para('Each knowledge item is worth one point (total 0–6). Confidence and agreement statements use a 1–5 scale. Report correct counts per item, score mean, median and range, and agreement counts (4–5). For a small convenience sample, use descriptive summaries and explain missing answers.'))
    g=result.get('groups',{})
    def grp(name):
        v=g.get(name)
        return f"{v['passed']}/{v['total']} passed" if v else 'Awaiting final XML'
    pages.append('<h2>2. Results</h2><h3>2.1 Knowledge questions</h3>'
        + para('Player results are pending. The six topics below adapt the structure of the supplied study report to this game. The existing after-play form uses a new situation or new numbers rather than asking the player to repeat a level’s solution.')
        + table(['Topic','Game connection','Correct / N'],[
            ['Shadow and light','Courtyard lamp and shadow alignment','Pending'],['Mirror reflection','Sun Wall rotating mirrors','Pending'],['Floating raft','Stepwell sluices and water level','Pending'],['Evidence deduction','Combining traces to support a suspect','Pending'],['Cyclic sequence','Rotating discs and sequence locks','Pending'],['Heritage value','Community memory and fictional objects','Pending']])
        + '<h3>2.2 Ratings of the game</h3>'
        + para('The ten statements cover fun, difficulty, controls, next-objective clarity, hints, voices and subtitles, puzzle understanding, care for heritage, continued play and recommendation. Means and agreement counts remain pending. No values have been copied from the reference game.')
        + '<h3>2.3 Learning habits, preference and comments</h3>'
        + para('The form asks about interest in history/heritage and science, focus, learning through problems, games and reasoning from clues. It also asks whether the game, usual study or both helped more, followed by what players liked and what was confusing. Responses and quotations are pending.')
        + '<h3>2.4 Technical verification</h3>'
        + table(['Evidence collected on 25 November 2026','Outcome / scope'],[
            ['UpgradeVerificationTests',grp('UpgradeVerificationTests')+'; selected temple, minimap-state, Jeep, retry and audio checks.'],
            ['NewBazaarPlayTests',grp('NewBazaarPlayTests')+'; one automated gameplay scenario per zone.'],
            ['Existing macOS .app','Launched separately; Courtyard and Sanctum images captured. Binary parity with current source is not established.'],
            ['Batch screenshots','World geometry visible; Canvas HUD may be absent. Not visual proof of minimap rendering.']])
        + '<div class="note">Technical checks establish only their asserted behaviour. They do not measure enjoyment, learning, cultural accuracy, every input path or release-build performance.</div>')
    pages.append('<h2>3. What the Results Mean</h2><h3>What is supported</h3>'
        + para(result['detail']+' The upgrade tests inspect the raw-temple object, gate-trigger/material state, modular temple presence, minimap visibility state, Jeep upright/damage reset, local checkpoint position and selected audio values. The zone suite exercises gameplay routes and puzzle/recovery states.')
        + '<h3>Changes made in the implementation</h3>'
        + para('The supplied implementation summary describes modular Kalinga temple construction, a sealed Courtyard doorway, a bottom-left square minimap, Jeep [R] repair and road reset, section checkpoint retries, regenerated devotional-style music/ambience and unified master, music, ambience, effects and voice controls. This documentation task records those changes; it does not alter game code.')
        + '<h3>What still needs observation</h3>'
        + para('A runtime visibility flag is not a screenshot of a rendered minimap. The recovery tests invoke methods directly and do not cover every keyboard or menu path. Audio values do not establish perceived clarity or musical quality. The existing installed app must be rebuilt and checked against the same source revision before claiming that all source upgrades ship together. Any failure in the new run must remain visible in the evidence notes.')
        + '<h3>Limitations</h3>'
        + para('There is no analysed player sample, baseline score or comparison group. An after-play study can show understanding after play but cannot establish a learning gain caused by the game. A short route may not expose every participant to all six knowledge topics. Automated gameplay covers selected paths, while screenshot inspection samples a few views. Historical frame-rate samples at 640×480 in the Editor are not a guaranteed 60 FPS benchmark. The game is heritage-inspired fiction rather than a verified reconstruction.')
        + '<h2>4. Next Step</h2>'
        + para('Complete an adult pilot study using the existing information sheet, consent form and after-play questionnaire. Check the wording and answer keys, record each participant’s task exposure, then fill the tables with real counts, scores and comments. Improve unclear objectives or puzzle feedback and repeat the affected checks.')
        + para('For a later pre/post study, validate the baseline Set A design in source/make_forms.py, make both forms available and use a matching participant code in each. The current local Apps Script creates the after-play form only. A larger, more varied sample and a text-based comparison condition would support stronger conclusions.'))
    def shot(path,caption):
        return '<figure><img src="'+path.as_uri()+'"><figcaption>'+E(caption)+'</figcaption></figure>'
    pages.append('<h2>References and Evidence</h2>'
        + '<h3>Project sources</h3><ol class="sources">'
        + '<li>Study protocol: Evaluation/Information_Sheet.pdf and Consent_Form.pdf.</li>'
        + '<li>Measures: Evaluation/Form_Questions.md, Google_Forms_Script.gs and source/make_forms.py. The script’s current output is after-play only.</li>'
        + '<li>Fresh technical run: Docs/QA/2026-11-25-evaluation/editor-tests.xml and editor-tests.log. Exact test names, outcomes and failures are retained.</li>'
        + '<li>Implementation checks: Assets/_Project/Tests/PlayMode/UpgradeVerificationTests.cs and NewBazaarPlayTests.cs.</li>'
        + '<li>Standalone evidence: Docs/QA/2026-11-25-evaluation/app/audit.md, player.log and PNG captures from the existing binary.</li>'
        + '<li>Format references: supplied Study_Methodology_and_Results.pdf and Think Your Way Out The Frozen Hour_pre.pdf. Their participants, statistics and quotations are not this project’s results.</li>'
        + '<li>Presentation template: supplied Heritage Mystery Project.pptx. Template illustration is separate from labelled gameplay evidence.</li></ol>'
        + '<h3>Current source-run world captures</h3><div class="shots">'
        + shot(QA/'editor-courtyard-sealed.png','Courtyard: modular temple and doorway in the current-source verification run.')
        + shot(QA/'editor-templering-world.png','TempleRing world view. Windowless capture does not demonstrate the Canvas minimap.')+'</div>'
        + '<div class="note">Cultural scope: the brass ceremonial vessel, sandstone rosette and palm-leaf manuscript are fictional heritage objects. The shrine idol remains a respectful, non-interactable background element.</div>'
        + '<p class="small">Prepared for university evaluation · 25 November 2026. The original reference files and game source assets were not edited. No external research findings are asserted in this report.</p>')
    css='''@page{size:A4;margin:0}*{box-sizing:border-box}body{margin:0;color:#222;font:10.2pt/1.43 Arial,Helvetica,sans-serif}.page{width:210mm;height:297mm;padding:17mm 18mm 18mm;position:relative;break-after:page;overflow:hidden}.page:last-child{break-after:auto}h1{text-align:center;font-size:19pt;font-weight:600;margin:0 0 6px}h2{font-size:15pt;margin:18px 0 9px}h2:first-child{margin-top:0}h3{font-size:11.4pt;margin:15px 0 5px}p{margin:5px 0 10px}li{margin-bottom:7px}ol{padding-left:21px;margin-top:6px}.meta{text-align:center;font-size:9pt;color:#555;line-height:1.55}.summary{margin:21px 0 22px;padding:12px 0;border-top:1px solid #bbb;border-bottom:1px solid #bbb}table{border-collapse:collapse;width:100%;font-size:9.2pt;line-height:1.35;margin:10px 0 13px}th{background:#eee;text-align:left;padding:7px}td{padding:7px;border-bottom:1px solid #ddd;vertical-align:top}.note{background:#f4f1ea;border-left:3px solid #b38b46;padding:10px 12px;font-size:9.2pt;margin:12px 0}.small{font-size:8.5pt;color:#555}.foot{position:absolute;bottom:10mm;left:18mm;right:18mm;text-align:center;color:#666;font-size:8pt}.sources{font-size:9.4pt;overflow-wrap:anywhere}.shots{display:flex;gap:12px;margin-top:13px}.shots figure{margin:0;flex:1}.shots img{width:100%;display:block}.shots figcaption{font-size:8.5pt;color:#555;margin-top:7px;line-height:1.4}'''
    document='<!DOCTYPE html><html lang="en"><meta charset="utf-8"><title>Legacy of the Temple — Study Methodology and Results</title><style>'+css+'</style><body>'+''.join('<section class="page">'+page+f'<div class="foot">Legacy of the Temple · Study Methodology and Results &nbsp; | &nbsp; {i+1}</div></section>' for i,page in enumerate(pages))+'</body></html>'
    (OUT/'source/Study_Methodology_and_Results.html').write_text(document)
    summary=('LEGACY OF THE TEMPLE — UPDATED IMPLEMENTATION SUMMARY\n25 November 2026\n\n'+TEAM+'\n\n'
        'The current source implements a modular Kalinga-inspired temple, a sealed Courtyard investigation doorway, a bottom-left square minimap, Jeep [R] repair and road reset, section checkpoint retries, devotional-style music and ambience, and master/music/ambience/SFX/voice volume controls. Original Blender and Heritage Mystery assets remain read-only.\n\n'
        +result['detail']+'\n\n'
        'The existing macOS app was also launched and captured separately. This binary is not evidence that the latest source upgrades are packaged. Batch world captures may omit the Canvas HUD, so a minimap visibility assertion is distinguished from visual rendering proof. See the evidence review for precise limits.\n\n'
        'The player study is pending. The evaluation report contains a game-specific methodology and empty result fields; no participants, survey means, learning gains or guaranteed performance numbers are invented.\n')
    (OUT/'Updated_Project_Summary.txt').write_text(summary)


if __name__ == '__main__':
    result=read_results()
    make_deck(result)
    make_report(result)
    print(result['detail'])
