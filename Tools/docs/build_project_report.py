"""Build the project report from source inspection and this review's recorded evidence.

Uses the already-installed Chrome browser; does not install or download anything.
Run after Docs/QA/2026-11-23-review/final-results.json has been written.
"""
import csv
import html
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "Docs/QA/2026-11-23-review"
OUT = ROOT / "Docs/S20240010236_Legacy_of_the_Temple_Report.pdf"
RESULT = json.loads((QA / "final-results.json").read_text())
esc = lambda s: html.escape(str(s))


def p(text):
    return "<p>" + esc(text) + "</p>"


def table(headers, rows):
    return "<table><thead><tr>" + "".join("<th>" + esc(h) + "</th>" for h in headers) + "</tr></thead><tbody>" + "".join(
        "<tr>" + "".join("<td>" + esc(c) + "</td>" for c in row) + "</tr>" for row in rows) + "</tbody></table>"


def shot(relative, caption):
    path = QA / relative
    return '<figure><img src="' + path.as_uri() + '"><figcaption>' + esc(caption) + "</figcaption></figure>"


def h(n, title):
    return f"<h2><span>{n:02}</span> {esc(title)}</h2>"


pages = []


def page(content):
    pages.append('<section class="page">' + content + '</section>')


page('<div class="cover"><div class="eyebrow">UNITY • 3D ADVENTURE • CULTURAL HERITAGE</div>'
     '<h1>LEGACY<br>OF THE TEMPLE</h1><div class="rule"></div><h3>Project inspection, targeted corrections<br>and gameplay report</h3>'
     + p("Student ID: S20240010236") + p("Student name / programme / institution / guide: not supplied for this report.")
     + p("Review date: 23 November 2026 · Unity 6000.6.8f1 · URP · macOS")
     + shot(RESULT["cover_shot"], "Actual game capture from the reviewed build.")
     + '<div class="footnote">Fictional investigation. The environment is inspired by Indian temple heritage. This report does not present the game as a verified reconstruction of Jagannath Temple or its history.</div></div>')

page(h(2, "Introduction") + p("Legacy of the Temple is a third-person adventure and investigation game built in Unity. Players guide investigator Aryan through an existing temple and village environment, speak with characters, examine evidence, solve environmental puzzles and recover fictional heritage objects.")
     + h(3, "Why this project was selected") + p("The project combines an accessible exploration game with the theme of protecting community heritage. Temple-inspired architecture, objects and dialogue provide a setting in which observation and reasoning have a clear purpose. This is a design rationale, rather than a claim that an educational study has already proved the game's effectiveness.")
     + h(4, "Problem statement") + p("The requested review concerned blocked stairs and raised paths, unreliable Jeep entry and exit, missing dialogue sound and inconsistent lighting. A player who cannot move safely or read the scene cannot concentrate on investigation. The work therefore prioritised these obstacles while retaining the current game's working content.")
     + h(5, "Why a game-based approach is appropriate") + p("A game lets the player make a prediction, act and see feedback. Rotating a mirror, changing water level or comparing clues turns passive reading into a task. This design can support curiosity and discussion; learning gains must still be measured with real participants.")
     + h(6, "Target users") + p("The intended audience is students and general players interested in an introductory heritage-themed adventure. Faculty can use the project to discuss interaction design, Unity development and the difference between fictional game content and verified historical information.")
     + h(7, "Project objectives") + p("Provide readable exploration, responsive movement, dependable interaction, understandable puzzle feedback, accessible subtitles and a meaningful heritage-protection theme. Preserve the existing scenes and story; document actual corrections, reproducible checks and remaining limits."))

page(h(8, "Existing game features") + table(["Area", "Present in the updated source"], [
    ["Movement and camera", "CharacterController locomotion, running, crouching, jumping, camera orbit and collision avoidance."],
    ["Investigation", "Objective cards, character conversations, clue examination, journal, evidence board and map."],
    ["Puzzles", "Storeroom break-in, vault blocks and discs, water/mirror stepwell and Sun Wall mechanisms."],
    ["Vehicle", "Modelled Jeep, visible seated driver, opening door, wheel physics, headlights, engine sound and damage states."],
    ["Combat", "Existing Brawl encounters with light/heavy attacks, blocking, dodging, enemy reactions and finishers."],
    ["Presentation", "Queued voices, subtitles, speaker framing, music/ambience layers, zone lighting and quality settings."],
    ["Progress", "Checkpoints, puzzle state, score, time pressure and completion screens."],
]) + p("Presence in source does not by itself establish that a feature works. Section 20 separates executed tests from inspection and unresolved concerns.")
     + h(9, "Actual scenes, chapters and playable areas")
     + p("The updated story data contains six chapters and nine objectives, all assigned to the Courtyard scene. The main route builds six playable areas within that scene. Older Sanctum, ShrineCrime, PuzzleWall and JunglePath scene assets remain in the temple build. They are retained and inspected, but are not six separate scene transitions in the current main route.")
     + table(["Story chapter", "Current purpose"], [["1 · The Empty Stands", "Arrival, priest and bazaar investigation"], ["2 · The Storeroom", "Storeroom break-in and vault puzzles"], ["3 · The Stepwell", "Water and light reasoning"], ["4 · The Sun Wall", "Symbol/disc mechanism"], ["5 · The River Road", "Final pursuit/encounters and recovery"], ["6 · First Light", "Return and conclusion"]]))

areas = [
    ("Bazaar and temple approach", "Introduce movement, the Jeep, the priest and the investigation. Follow the existing courier trail and look for the next objective.", "Players practise noticing a suspicious action, following a route and relating a witness statement to an event. The environment introduces a temple-associated community rather than treating a building as an isolated object."),
    ("Storeroom courtyard", "Examine and operate the existing break-in puzzle: search around the damaged store and use its mechanisms to recover the chronicle.", "Players learn to observe an object before acting and connect physical traces to a possible explanation. The fictional storage context supports discussion of safeguarding cultural material."),
    ("Vaults", "Use blocks and pressure plates, then inspect and rotate the existing symbol discs. Recover the rosette after the encounter.", "Players practise planning several moves, checking conditions and correcting an unsuccessful arrangement. The lesson is logical reasoning, not a claim that this is an authentic temple security mechanism."),
    ("Stepwell", "Use the existing water-level and light/mirror mechanisms and associated controls. Follow local hints to unlock the route.", "Players explore cause and effect: a change in water level or mirror direction changes what can be reached or illuminated. These are game puzzles inspired by environmental observation, not historical claims about a real stepwell."),
    ("Sun Wall", "Read the local symbol clue and operate the existing discs and light/shadow mechanism, then continue the investigation.", "Players combine visual comparison, spatial reasoning and an ordered sequence of actions. Symbols serve a fictional puzzle rule; no ritual or religious meaning is inferred from their arrangement."),
    ("River road and return", "Complete the existing final area, recover the fictional objects and return them to the priest. Review score and elapsed time.", "Players connect the result of their actions with responsibility for shared cultural property. The conclusion is intended to encourage care for heritage; actual attitude change requires participant evaluation."),
]
for start in (0, 2, 4):
    body = h(10 if start == 0 else 11, "Why each area exists and what players learn" + (" (continued)" if start else ""))
    for name, purpose, learning in areas[start:start+2]:
        body += '<h3>' + esc(name) + '</h3><div class="label">Gameplay purpose</div>' + p(purpose) + '<div class="label">Educational purpose</div>' + p(learning)
    if start == 0:
        body += shot(RESULT["story_shot"], "Current main-route gameplay; the surrounding chapter context is fictional.")
    page(body)

page(h(12, "Educational and cultural importance")
     + p("The game can introduce the ideas of architectural observation, community memory, evidence-based reasoning and responsible treatment of cultural objects. These are intended learning opportunities, not measured study outcomes. The project should distinguish its own invented events from reliable information about a living religious tradition.")
     + p("Jagannath is represented as a respectful, non-interactable background shrine element. No changes in this review make the shrine a stolen or carried object. The original brief specifies three fictional objects: a brass ceremonial vessel, a carved sandstone rosette and a palm-leaf manuscript.")
     + p("A content discrepancy remains: the updated main-route dialogue says 'Five treasures' and includes newer recovery objectives. The original brief and the updated story should be reconciled by the author before submission. This review preserves the updated story rather than silently rewriting it.")
     + h(13, "Gameplay mechanics")
     + p("The player moves a capsule controller while the visual character plays its existing animations. Interaction selects a nearby usable object or NPC, with priority for the current objective. Dialogue and cards temporarily own gameplay input. Puzzle mechanisms publish state changes, which update objectives, score and checkpoints. The current main route advances within one world scene.")
     + h(14, "Movement and interaction controls") + table(["Input", "Action"], [["W / A / S / D or arrow keys", "Move relative to the camera"], ["Mouse", "Look around"], ["Shift", "Run"], ["Space", "Jump; advance dialogue when dialogue owns input"], ["C / Ctrl", "Crouch"], ["E", "Interact; enter/exit Jeep when stopped"], ["Caps Lock", "Toggle auto-run"], ["G", "Flashlight on foot; horn in Jeep"], ["H / Tab", "Guide / current objective help"], ["J / M", "Journal / map"], ["Esc", "Pause / close the active overlay"]])
     + p("Bindings above were checked against current scripts. They describe the default keyboard path; some systems read keys directly, so a universal rebinding feature is not claimed."))

page(h(15, "Puzzle mechanics") + p("Operate highlighted parts with E and follow each puzzle's local control prompt. Blocks change plate states; discs store orientations; mirrors redirect light; valves alter water. Wrong arrangements should leave the player able to retry. The guide offers staged hints; using hints can reduce the recorded score. Checkpoint and puzzle-state tests verify the tested state paths, rather than every possible solution order.")
     + h(16, "Character and NPC systems")
     + p("Aryan uses the existing investigator model and Animator. Pujari Ananda and Meera retain their original imported appearance. The updated game also already contains courier, villager and robber encounter systems. This review adds no characters and does not replace models or create animation clips. Some retained legacy scenes contain older stand-in enemies; the full cast/identity review remains outside these targeted repairs.")
     + p("NPC dialogue selects current story lines or a short objective reminder. Subtitles and speaker framing remain available when no matching recording exists. An unprefixed NPC line now raises the same voice event as a line written with a speaker prefix.")
     + h(17, "Audio and visual design")
     + p("The existing voice database contains 166 lines, including prior locally generated speech. This review creates no speech files and does not describe those clips as human performances. Voice playback uses the existing priority queue, cooldowns, subtitles and music ducking. Existing sound systems cover footsteps, vehicle sounds, impacts, puzzle actions and ambience.")
     + p("Lighting retains URP and ACES. The correction reduces indigo shadow grading, uses a more neutral fill, lowers distracting grain/bloom/vignette and gives local zone volumes priority over the global look. Interior warmth and differences between zones are retained. No new textures, models or large downloads were added.")
     + table(["Additional controls", "Action"], [["Jeep W / S, A / D", "Accelerate / reverse, steer"], ["Jeep Space / L / E", "Brake, headlights, get out while slow"], ["Left mouse / R", "Light / heavy attack"], ["Q / V / F", "Block / dodge / finisher"], ["Dialogue Space / Enter / E", "Reveal or advance the current line; choice controls follow the prompt"]]))

page(h(18, "Problems discovered during inspection") + table(["Problem", "Confirmed root cause"], [
    ["Raised paths stopped the hero", "PlayerController reversed any ascent more than one metre above its starting elevation; jungle treads also rise 35 cm against a 30 cm controller step limit."],
    ["Risky vehicle placement", "Entry moved the Transform through obstacles. Exit tested only one ray and one location without checking capsule clearance."],
    ["Inconsistent lighting", "The global volume at priority 100 overrode zone volumes at priority 10. Indigo lift and cool fills further tinted dark surfaces."],
    ["Some NPC lines had no voice event", "UIManager raised OnSubtitleLine only for lines containing a speaker prefix, although prefab dialogue also contains plain lines."],
    ["False substitute for missing speech", "VoicePlayer generated talk blips when a recording was missing. These were not spoken dialogue."],
    ["Compiler warnings", "Two obsolete Unity API calls in the pre-existing separate CarFootball editor utility."],
]) + h(19, "Corrections implemented") + table(["Files / component", "Targeted change"], [
    ["PlayerController.cs; CharacterGrounding.cs", "Remove the spawn-relative height clamp; use slope/step collision rules; allow the authored 35 cm treads; stop upward velocity at a ceiling."],
    ["Jeep.cs", "Use capsule movement during approach; guard interaction transitions; test candidate exit ground, slope and capsule room; try nearby/opposite-side exits; allow parking brakes to settle."],
    ["LookTuner.cs; ZoneLook.cs", "Neutralise excessive shadow colour and reduce effects; adjust fill and zone presets; let local grading override the global grade."],
    ["UIManager.cs; VoicePlayer.cs", "Raise the voice hook for plain lines too. Keep missing recordings silent with subtitles and a labelled missing-recording slot."],
    ["CarFootballSceneBuilder.cs", "Update only two obsolete API calls. Preserve the unrelated game and its settings."],
    ["ReviewRegressionTests.cs; AuditBot.cs", "Add real-scene movement/exit regression checks and an opt-in lighting/stairs capture path."],
]) + p("Backups were taken before edits in Docs/QA/2026-11-23-review/backup. The existing backup-visual-1 tag was retained. Original Heritage Mystery Project and Blender sources were not modified."))

page(h(20, "Testing results") + p(RESULT["test_summary"]) + table(["Executed check", "Observed result / limit"], RESULT["test_rows"])
     + p("Tests exercise the actual controller and authored colliders in Unity Editor Play Mode. Stair regression checks use the game's on-screen input route to avoid keyboard focus loss in the batch runner. Standalone checks use the built application. The story bot stages the character near objects; it does not establish that every route is naturally discoverable or reachable on foot.")
     + p("Early failed test attempts are retained in round1/round2 logs and XML. The first keyboard-based test harness did not deliver movement. Later passing measurements come from a corrected input harness; they are not inferred from the existence of files.")
     + shot("editor/courtyard-stairs-top.png", "Editor Play Mode: investigator at the actual upper courtyard tread; measured foot elevation 1.205 m, with a 0.667 m jump and return to the same surface.")
     + p("Evidence directory: Docs/QA/2026-11-23-review/. XML contains individual assertions; build and player logs contain the executed results. Screenshots were opened and visually inspected during the review."))

page('<h2>Lighting evidence</h2>' + shot("before/s4-look-Sanctum.png", "Before: existing built game's Sanctum, with strong blue/purple shadows and dark character separation.")
     + shot(RESULT["sanctum_shot"], "After: newly built game, same authored Sanctum scene. Neutral fill exposes floor and character detail while retaining warm lamps. This is a visual comparison, not a calibrated rendering benchmark.")
     + p(RESULT["visual_judgement"]))

page(h(21, "Current limitations") + "<ul>" + "".join("<li>" + esc(x) + "</li>" for x in RESULT["limitations"]) + "</ul>"
     + p("Dynamic objective reminders do not have a unique recording for every possible state. Missing lines use subtitles and are logged as MISSING RECORDING; no synthetic blip is presented as speech. Docs/QA/2026-11-23-review/recording-requirements.csv lists static lines requiring recordings and a template for state-dependent reminders.")
     + h(22, "Future improvements") + p("Reconcile object counts and cast identity with the final story; record and review the outstanding spoken lines; verify routes with manual first-time players; review older scene enemy models; improve temple material consistency and remove remaining exposed placeholder forms. Extend vehicle exit tests to slopes, ledges and tightly enclosed parking. Conduct the planned participant evaluation before making claims about learning outcomes.")
     + h(23, "How to play") + p("Open Builds/LegacyOfTheTemple.app and choose New Game, or open My project in Unity, open the MainMenu scene and press Play. Follow the current objective, use WASD and the mouse to explore, hold Shift to run and press E near a person or usable object. Space advances dialogue while a conversation is open. H opens the guide; read the current puzzle's local prompts before operating its parts.")
     + p("Use E by the Jeep door, W/S to drive and Space to brake. Stop in clear space before pressing E to leave. Use J to review evidence and M for the map. Solve the currently assigned area, watch the objective update, then proceed to the next. Return recovered fictional treasures to the priest for the ending and score screen. Esc opens pause/settings; check master and voice volume if speech is silent.")
     + h(24, "Conclusion") + p("The review makes targeted corrections to movement, vehicle safety checks, voice-event dispatch and lighting while preserving the updated game's content. The executed checks and screenshots support the specific outcomes reported here. Outstanding content, visual and coverage limits are documented so the project can be explained accurately to faculty."))

css = """
@page {size:A4;margin:15mm 15mm 17mm;}
*{box-sizing:border-box} body{margin:0;color:#263333;font:10.5pt/1.43 'Helvetica Neue',Arial,sans-serif}
.page{break-before:page} .page:first-child{break-before:auto} h1,h2,h3{break-after:avoid;color:#163e40}
h1{font:46pt/1.06 Georgia,serif;letter-spacing:1px;margin:20mm 0 8mm}
h2{font:20pt/1.16 Georgia,serif;border-bottom:1px solid #c6aa6e;padding-bottom:3mm;margin:7mm 0 3mm}
h2 span{color:#a57935;font:11pt Arial;margin-right:2mm} h3{font-size:14pt;margin:6mm 0 2mm}
p{margin:2.5mm 0}table{width:100%;border-collapse:collapse;margin:4mm 0;font-size:9.3pt;line-height:1.32}
th{background:#163e40;color:white;text-align:left}th,td{padding:2.4mm;border:1px solid #d4dcd8;vertical-align:top}
tr{break-inside:avoid} tr:nth-child(even){background:#f3f5f1} figure{margin:5mm 0;break-inside:avoid}
figure img{width:100%;max-height:88mm;object-fit:contain;background:#17272a;border:1px solid #b9c8c4}
figcaption,.footnote{font-size:9pt;line-height:1.35;color:#536664;margin-top:2mm}
.cover .eyebrow{letter-spacing:1.4px;font-size:9pt;color:#a57935;margin-top:8mm}.rule{height:2px;width:44mm;background:#b88f4a}
.cover h3{font-weight:400;line-height:1.5;font-size:17pt}.cover figure{margin-top:11mm}.cover .footnote{margin-top:8mm}
.label{font-weight:bold;color:#86652d;font-size:9pt;text-transform:uppercase;letter-spacing:.5px;margin-top:3mm}
li{margin:2.5mm 0} ul{padding-left:5mm}
"""
document = "<!doctype html><html><head><meta charset='utf-8'><title>Legacy of the Temple — project report</title><style>" + css + "</style></head><body>" + "".join(pages) + "</body></html>"
source = QA / "project-report.html"
source.write_text(document)
browser = Path("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome")
subprocess.run([str(browser), "--headless=new", "--disable-gpu", "--no-pdf-header-footer", "--allow-file-access-from-files",
                "--user-data-dir=/private/tmp/lot-project-report-browser", "--print-to-pdf=" + str(OUT), source.as_uri()], check=True, timeout=120)
print(OUT)
