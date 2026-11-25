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
QA = ROOT / 'Docs/QA/2026-11-25-original-locations'
LATEST = ROOT / 'Docs/QA/2026-11-25-e-interaction'
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
    wider = ET.parse(QA/'editor-original-round2.xml').getroot()
    focused = ET.parse(LATEST/'editor-final.xml').getroot()
    assert focused.get('failed') == '0' and wider.get('failed') == '0'
    detail = (f"Wider Editor PlayMode run: {wider.get('passed')}/{wider.get('total')} passed. "
              f"Final focused original-route run: {focused.get('passed')}/{focused.get('total')} passed. "
              "These are separate, overlapping runs; their counts are not added.")
    result = {'label': 'Verified technical checks; player study pending', 'detail': detail,
              'short': f"{wider.get('passed')}/{wider.get('total')} wider checks\n{focused.get('passed')}/{focused.get('total')} focused checks",
              'groups': {'Wider regression run': {'passed': wider.get('passed'), 'total': wider.get('total')},
                         'OriginalRouteReviewTests': {'passed': focused.get('passed'), 'total': focused.get('total')}},
              'build_stamp': (ROOT/'Assets/_Project/Resources/build_stamp.txt').read_text().strip()}
    (LATEST/'evaluation-results.json').write_text(json.dumps(result, indent=2))
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
    content = json.loads((OUT/'source/current_presentation_content.json').read_text())
    slides = {int(k): v for k, v in content['slides'].items()}
    order = content['order']
    notes = [content['notes'].get(str(n), '') for n in range(1, 14)]
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
            diagram_shape(tree,1101,'ORIGINAL COURTYARD ROUTE · REBUILT APP',6.6,6.65,6.8,0.42,size=17)
            diagram_shape(tree,1000,'ZONE 2 · STOREROOM\nZONE 3 · VAULTS',6.5,7.0,7,1.1,size=24)
        if number==12:
            diagram_shape(tree,1100,'Empty E animation → Grounded interaction clip\nWrong world → Original Courtyard route\nTilted / damaged Jeep → Upright road reset',8.3,6.3,10.1,1.65,color='E5D3AF',size=21,ink='302419')
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
        'image4.png': QA/'app-final-round5/01-confirmed-village-and-minimap.png',
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
    pages.append(header + '<div class="summary"><b>Summary.</b> Legacy of the Temple is a third-person cultural investigation game built in Unity 6000.6.8f1 with URP. Players examine clues, solve physical puzzles and recover fictional heritage objects in a temple-inspired environment. '+E(result['detail'])+' The rebuilt macOS app was launched separately and its original locations, rendered HUD and task controls inspected. These are technical observations; the player study is pending. No participant scores, enjoyment ratings or learning gains are reported.</div>'
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
            ['OriginalRouteReviewTests',grp('OriginalRouteReviewTests')+'; original places, real E/R input, visible interaction pose, doorway, retry and audio.'],
            ['Wider regression run',grp('Wider regression run')+'; movement, steps, vehicles, puzzles and voice-system checks. Overlaps the focused run.'],
            ['Rebuilt macOS .app','Build report: 0 errors, 0 warnings. Original Courtyard route, minimap, road recovery and scrolling menu captured.'],
            ['E interaction regression','Before: visible feet dropped about 85 cm while the capsule stayed grounded. After assigning the real clip: about 2 cm movement in the same check.']])
        + '<div class="note">Technical checks establish only their asserted behaviour. They do not measure enjoyment, learning, cultural accuracy, every input path or release-build performance.</div>')
    pages.append('<h2>3. What the Results Mean</h2><h3>What is supported</h3>'
        + para(result['detail']+' The focused tests preserve the established temple footprint and check the sealed doorway, real mirror rays, wheel obstruction, R-key vehicle recovery, local retry and independent audio settings. The E regression measures posed mesh and head positions throughout an actual task interaction; collision alone would miss the reported visual dip.')
        + '<h3>Changes made in the implementation</h3>'
        + para('The main game preserves the original village, Courtyard and Sun Wall locations confirmed by the team. The detailed temple game prefab and original footprint were restored, while the unwanted Courtyard doorway stays sealed. The minimap now follows the established roads; R repairs the Jeep locally; same-scene retries restore the local clock budget. Five separate volume sliders sit in a scrolling pause menu. An existing licensed interaction clip replaces the empty E animation state.')
        + '<h3>What still needs observation</h3>'
        + para('The rebuilt app screenshots show the rendered minimap and original places. Focused tests exercise actual E and R input; the local retry check invokes the recovery method. Reproducible QA positions the player between locations and samples selected cases. It does not establish every possible path or camera angle. Audio bindings do not establish perceived clarity or musical quality. Saved Master/Music settings were 0%; raise them for an audible demonstration.')
        + '<h3>Limitations</h3>'
        + para('There is no analysed player sample, baseline score or comparison group. An after-play study can show understanding after play but cannot establish a learning gain caused by the game. A short route may not expose every participant to all six knowledge topics. Automated gameplay covers selected paths, while screenshot inspection samples a few views. Keeping the complete temple game mesh costs more geometry than the damaged reduced LODs. No new controlled FPS, memory or build-size benchmark is claimed. The game is heritage-inspired fiction rather than a verified reconstruction.')
        + '<h2>4. Next Step</h2>'
        + para('Complete an adult pilot study using the existing information sheet, consent form and after-play questionnaire. Check the wording and answer keys, record each participant’s task exposure, then fill the tables with real counts, scores and comments. Improve unclear objectives or puzzle feedback and repeat the affected checks.')
        + para('For a later pre/post study, validate the baseline Set A design in source/make_forms.py, make both forms available and use a matching participant code in each. The current local Apps Script creates the after-play form only. A larger, more varied sample and a text-based comparison condition would support stronger conclusions.'))
    def shot(path,caption):
        return '<figure><img src="'+path.as_uri()+'"><figcaption>'+E(caption)+'</figcaption></figure>'
    pages.append('<h2>References and Evidence</h2>'
        + '<h3>Project sources</h3><ol class="sources">'
        + '<li>Study protocol: Evaluation/Information_Sheet.pdf and Consent_Form.pdf.</li>'
        + '<li>Measures: Evaluation/Form_Questions.md, Google_Forms_Script.gs and source/make_forms.py. The script’s current output is after-play only.</li>'
        + '<li>Editor evidence: Docs/QA/2026-11-25-original-locations/editor-original-round2.xml and Docs/QA/2026-11-25-e-interaction/editor-final.xml. Counts describe separate overlapping runs.</li>'
        + '<li>Interaction regression: Assets/_Project/Tests/PlayMode/OriginalRouteReviewTests.cs. The before-fix failure is retained under Docs/QA/2026-11-25-completion-check/e-before-visible.xml.</li>'
        + '<li>Standalone evidence: Docs/QA/2026-11-25-original-locations/app-final-round5/ and Docs/QA/2026-11-25-e-interaction/. The latest build identity and reviewed captures are recorded in the evidence notes.</li>'
        + '<li>Format references: supplied Study_Methodology_and_Results.pdf and Think Your Way Out The Frozen Hour_pre.pdf. Their participants, statistics and quotations are not this project’s results.</li>'
        + '<li>Presentation template: supplied Heritage Mystery Project.pptx. Template illustration is separate from labelled gameplay evidence.</li></ol>'
        + '<h3>Original-route standalone captures</h3><div class="shots">'
        + shot(QA/'app-final-round5/01-confirmed-village-and-minimap.png','Original village approach and Courtyard entrance. The square minimap is rendered in the built app.')
        + shot(QA/'app-final-round5/02-confirmed-sunwall-and-minimap.png','Original Sun Wall wheel plaza and four mirrors. This is the unsolved state, before turning the controls.')+'</div>'
        + '<div class="note">Cultural scope: the brass ceremonial vessel, sandstone rosette and palm-leaf manuscript are fictional heritage objects. The shrine idol remains a respectful, non-interactable background element.</div>'
        + '<p class="small">Prepared for university evaluation · 25 November 2026. The original reference files and game source assets were not edited. No external research findings are asserted in this report.</p>')
    css='''@page{size:A4;margin:0}*{box-sizing:border-box}body{margin:0;color:#222;font:10.2pt/1.43 Arial,Helvetica,sans-serif}.page{width:210mm;height:297mm;padding:17mm 18mm 18mm;position:relative;break-after:page;overflow:hidden}.page:last-child{break-after:auto}h1{text-align:center;font-size:19pt;font-weight:600;margin:0 0 6px}h2{font-size:15pt;margin:18px 0 9px}h2:first-child{margin-top:0}h3{font-size:11.4pt;margin:15px 0 5px}p{margin:5px 0 10px}li{margin-bottom:7px}ol{padding-left:21px;margin-top:6px}.meta{text-align:center;font-size:9pt;color:#555;line-height:1.55}.summary{margin:21px 0 22px;padding:12px 0;border-top:1px solid #bbb;border-bottom:1px solid #bbb}table{border-collapse:collapse;width:100%;font-size:9.2pt;line-height:1.35;margin:10px 0 13px}th{background:#eee;text-align:left;padding:7px}td{padding:7px;border-bottom:1px solid #ddd;vertical-align:top}.note{background:#f4f1ea;border-left:3px solid #b38b46;padding:10px 12px;font-size:9.2pt;margin:12px 0}.small{font-size:8.5pt;color:#555}.foot{position:absolute;bottom:10mm;left:18mm;right:18mm;text-align:center;color:#666;font-size:8pt}.sources{font-size:9.4pt;overflow-wrap:anywhere}.shots{display:flex;gap:12px;margin-top:13px}.shots figure{margin:0;flex:1}.shots img{width:100%;display:block}.shots figcaption{font-size:8.5pt;color:#555;margin-top:7px;line-height:1.4}'''
    document='<!DOCTYPE html><html lang="en"><meta charset="utf-8"><title>Legacy of the Temple — Study Methodology and Results</title><style>'+css+'</style><body>'+''.join('<section class="page">'+page+f'<div class="foot">Legacy of the Temple · Study Methodology and Results &nbsp; | &nbsp; {i+1}</div></section>' for i,page in enumerate(pages))+'</body></html>'
    (OUT/'source/Study_Methodology_and_Results.html').write_text(document)
    summary = ('LEGACY OF THE TEMPLE — VERIFIED IMPLEMENTATION SUMMARY\n25 November 2026\n\n'+TEAM+'\n\n'
        'The main game preserves the original village, Courtyard and Sun Wall locations. New Game opens Courtyard. The existing detailed temple game prefab and its original footprint are retained; original Blender and Heritage Mystery sources remain untouched.\n\n'
        'The square minimap follows the actual roads and destinations. R repairs the Jeep and resets upright onto a nearby original road segment. Same-scene retry restores the local checkpoint and clock budget. The dark Courtyard doorway is sealed. A scrolling pause menu contains independent Master, Music, Ambience, Effects and Voice sliders.\n\n'
        'The E-key visual sinking bug was reproduced: the visible feet dropped about 85 cm while the capsule stayed grounded. The empty Interact state now uses the existing CC0 Quaternius interaction clip. The focused check shows about 2 cm foot movement and confirms that the real puzzle still responds.\n\n'
        +result['detail']+'\n'+result['build_stamp']+' — build report: 0 errors, 0 warnings.\n\n'
        'Standalone task screenshots and story audit are described in Docs/QA/2026-11-25-e-interaction/evidence-review.md. Scripted QA uses controlled positioning and is not an uninterrupted manual playthrough or an FPS benchmark. Saved Master and Music were at 0%; raise them to hear the sound.\n\n'
        'The report and PPT now describe this original route. The player study is pending: no respondents, survey means, learning gains or player quotations have been invented.\n')
    (OUT/'Updated_Project_Summary.txt').write_text(summary)


if __name__ == '__main__':
    result=read_results()
    make_deck(result)
    make_report(result)
    print(result['detail'])
