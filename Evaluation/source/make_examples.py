"""Writes source/Example_Answers.html: how 20 imagined player types might answer the 'After you play' form,
plus the answer key. Clearly marked as examples on every page; NOT study data.
Run: python3 Evaluation/source/make_examples.py, then print the HTML to PDF."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib.util
spec = importlib.util.spec_from_file_location("mf", os.path.join(os.path.dirname(__file__), "make_forms.py"))
mf = importlib.util.module_from_spec(spec); sys.argv = ["x"]
src = open(spec.origin).read().split("open(os.path.join(OUT")[0]   # reuse the question data without writing files
exec(src, mf.__dict__)
Q = mf.SET_B; LEARN = mf.LEARN_ROWS; GAME = mf.GAME_ROWS
EXPLAIN = [
    "Light travels in straight lines. A higher lamp sends its rays down more steeply, so the shadow on the floor shrinks towards the pot's own size.",
    "When a mirror turns by an angle, the reflected beam turns by twice that angle (angle in = angle out on both sides). This is why each Sun Wall mirror moves the beam a lot.",
    "A floating raft always rests on the surface; if the water drops 1 m, the raft drops 1 m with it (as in the stepwell).",
    "Good reasoning uses all the evidence together, as on the evidence board: the inside man is the one person every clue points to.",
    "6 → 7 → 0 → 1 is 3 pushes: after the last stop the dial goes round to 0 again (like the vault discs).",
    "The game's message: \"Their value isn't money. It's memory.\" Heritage objects matter for the history and meaning they hold for a community.",
]
CORRECT = [next(i for i, c in enumerate(ch) if c.startswith("*")) for _, ch in Q]

# 20 imagined player types: (type, short description, age, gender, how sure 1-5, which answers right (1) / wrong option index,
#  learn ratings 6, study habits, game ratings 10, which helped, liked, improve)
P = [
 ("The keen gamer", "Plays every day; rushes through and skips text.", 20, "Male", 4, [1,1,1,1,0,1], [3,3,3,5,5,4], ["Videos"], [5,2,5,4,2,4,3,3,5,5], "Both the same", "The jeep chase and the fights.", "Let me skip the talking faster."),
 ("The history lover", "Rarely games; loves temples and old stories.", 19, "Female", 3, [1,0,1,1,1,1], [5,3,2,4,4,4], ["Textbook","Museum or heritage visits"], [4,3,2,3,5,5,4,5,4,5], "The game", "The village and the temple; the treasures' stories.", "The controls took me a while."),
 ("The careful thinker", "Reads every clue before acting.", 21, "Male", 5, [1,1,1,1,1,1], [4,4,2,5,4,5], ["Textbook","Class notes"], [4,2,4,5,3,4,5,4,4,4], "The game", "The storeroom: rebuilding what happened.", "Fights felt like they interrupt the puzzles."),
 ("The nervous beginner", "First 3D game with keyboard and mouse.", 18, "Female", 2, [1,0,1,1,0,1], [3,3,4,4,3,2], ["Class notes","Group study"], [3,4,2,2,5,4,3,4,3,3], "Not sure", "Meera's help and the subtitles.", "I got lost; a clearer arrow to the next place."),
 ("The science student", "Strong in physics, not interested in history.", 20, "Male", 5, [1,1,1,1,1,0], [2,5,2,5,4,4], ["Textbook","Quiz apps"], [4,2,4,4,3,4,5,2,3,4], "Both the same", "The mirror puzzle at the Sun Wall.", "More physics puzzles, fewer fights."),
 ("The distracted multitasker", "Plays on the phone while studying.", 19, "Male", 2, [0,0,1,1,0,1], [3,2,5,3,4,2], ["Videos"], [4,3,4,3,4,3,2,3,3,3], "My usual way of studying", "The bazaar chase.", "Too much walking between places."),
 ("The puzzle fan", "Loves escape rooms and riddles.", 22, "Female", 4, [1,1,1,1,1,1], [4,4,2,5,5,5], ["Quiz apps","Group study"], [5,3,4,4,2,5,5,4,5,5], "The game", "The vault discs and the light rings.", "Harder optional puzzles would be great."),
 ("The story seeker", "Plays games for characters and plot.", 20, "Female", 3, [1,1,0,1,1,1], [4,3,3,4,4,3], ["Videos","Class notes"], [5,2,4,4,3,5,4,5,5,5], "The game", "The twist about Madhav.", "Wanted more scenes with the Pujari."),
 ("The speed runner", "Wants the best score and time.", 21, "Male", 3, [1,0,1,0,1,1], [2,3,3,4,4,3], ["Videos"], [4,2,5,4,1,3,3,2,4,4], "Both the same", "The Dawn Clock pressure.", "Show my time per place."),
 ("The struggling student", "Finds science hard; prefers hands-on.", 19, "Male", 2, [1,0,1,1,0,1], [2,2,4,5,4,2], ["Group study"], [4,4,3,3,5,4,4,4,4,4], "The game", "Seeing the water rise in the stepwell.", "Hints could explain the idea, not only the move."),
 ("The sceptic", "Thinks games are a waste of study time.", 22, "Male", 4, [1,1,1,1,0,1], [3,4,2,3,2,4], ["Textbook"], [3,2,4,4,3,3,3,2,2,3], "My usual way of studying", "The graphics of the temple.", "Not sure what I learned that a book wouldn't teach."),
 ("The artist", "Notices colours, places and design.", 20, "Female", 3, [1,1,1,1,0,1], [4,3,3,4,4,3], ["Videos","Museum or heritage visits"], [4,3,4,3,4,4,3,5,4,4], "Both the same", "Lighting at dawn and the lanterns.", "Some places are too dark (vaults)."),
 ("The competitive one", "Plays fighting games, wants a challenge.", 21, "Male", 3, [1,0,1,1,1,1], [2,3,3,4,4,3], ["Quiz apps"], [4,1,5,4,1,4,3,2,4,4], "Both the same", "Bhaskar's boss fight.", "Fights were too easy."),
 ("The quiet observer", "Takes time, explores every corner.", 19, "Female", 4, [1,1,1,1,1,1], [4,4,2,4,4,4], ["Textbook","Class notes"], [4,3,4,4,4,4,4,4,4,4], "The game", "Finding evidence by looking carefully.", "The evidence board was hard to open (B)."),
 ("The language learner", "Comfortable in Telugu, slower in English.", 20, "Male", 2, [1,0,1,0,1,1], [4,3,3,4,4,2], ["Class notes","Group study"], [4,3,3,3,4,5,3,4,4,4], "Not sure", "The voices read the lines aloud.", "Subtitles in Telugu or Hindi."),
 ("The heritage volunteer", "Has volunteered at a museum.", 22, "Female", 4, [1,1,0,1,1,1], [5,3,2,4,5,4], ["Museum or heritage visits","Textbook"], [5,2,4,4,3,5,4,5,5,5], "The game", "The ending message about memory.", "Add real facts about temple architecture at the end."),
 ("The mobile gamer", "Plays only on a phone.", 18, "Male", 2, [0,0,1,1,0,1], [3,3,4,3,4,2], ["Videos","Quiz apps"], [3,3,2,3,4,3,3,3,3,3], "Not sure", "The jeep.", "Keyboard and mouse were hard; a controller option."),
 ("The planner", "Makes notes and plans every move.", 21, "Female", 5, [1,1,1,1,1,1], [4,4,2,5,4,5], ["Class notes"], [4,2,4,5,3,4,5,4,4,4], "The game", "Pushing the blocks round the pillar.", "A notebook to write clues in."),
 ("The tired evening player", "Playing after a long day of classes.", 20, "Male", 2, [1,0,1,1,0,1], [3,3,4,4,3,3], ["Videos"], [3,3,3,3,4,4,3,3,3,3], "Both the same", "The music and the village sounds.", "It was long for one sitting."),
 ("The teacher-to-be", "Wants to teach school science.", 22, "Female", 4, [1,1,1,1,1,1], [4,5,2,5,5,4], ["Textbook","Group study"], [5,2,4,4,4,5,5,5,4,5], "The game", "Physics hidden inside a mystery.", "A short summary of each idea after the puzzle."),
]

def answer(i, right):
    opts = [c.lstrip("*") for c in Q[i][1]]
    if right: return opts[CORRECT[i]], True
    wrong = [j for j in range(len(opts)) if j != CORRECT[i]]
    return opts[wrong[(i + len(opts)) % len(wrong)]], False

css = open(os.path.join(os.path.dirname(__file__), "style.css")).read()
h = [f"<!doctype html><html><head><meta charset='utf-8'><title>Example answers</title><style>{css}"
     ".wm{position:fixed;top:44%;left:-10%;width:120%;text-align:center;transform:rotate(-28deg);font-size:46pt;color:rgba(178,58,50,.10);font-weight:700;z-index:-1}"
     ".band{background:#fbe9e7;border:1.5px solid #b23a32;color:#7d2a24;padding:9px 14px;border-radius:6px;margin:10px 0;font-size:9.6pt}"
     ".m{page-break-inside:avoid;border:1px solid #c9d6e2;border-radius:6px;padding:10px 14px;margin:12px 0}"
     ".m h3{margin:0 0 2px;color:#245b88}.m table{margin:6px 0;font-size:8.8pt}.m td,.m th{padding:4px 7px}"
     ".ok{color:#1f7a45;font-weight:700}.no{color:#b23a32;font-weight:700}</style></head><body>"
     "<div class='wm'>EXAMPLE · NOT REAL DATA</div>",
     "<div class='title-block'><h1>Example answers: 20 player types</h1><p class='subtitle'>How different kinds of players might answer the \"After you play\" form, with the answer key</p>"
     "<div class='meta'><b>Game:</b> Legacy of the Temple · <b>Team:</b> Hemanth (S20240010236) · Aakash (S20240010245) · Sai Teja (S20240010136) · <b>Guide:</b> Dr. Himangshu Sarma</div></div>",
     "<div class='band'><b>These are imagined examples, not study data.</b> No real person gave these answers. They show the range of answers to expect from different kinds of players, how scoring works, and how to read the form. Do not enter them in the Google Form or report them as results; the study results must come only from real participants.</div>",
     "<h2>Answer key (Set B, 1 point each)</h2><table><tr><th style='width:4%'>#</th><th style='width:44%'>Question</th><th style='width:18%'>Correct answer</th><th>Why</th></tr>"]
for i, (q, ch) in enumerate(Q):
    h.append(f"<tr><td>{i+1}</td><td>{q}</td><td class='ok'>{ch[CORRECT[i]].lstrip('*')}</td><td>{EXPLAIN[i]}</td></tr>")
h.append("</table><h2>Overview of the 20 examples</h2><table><tr><th>#</th><th>Player type</th><th>Score / 6</th><th>How sure</th><th>Game was fun</th><th>Helped more</th></tr>")
for n, p in enumerate(P, 1):
    h.append(f"<tr><td>Example {n}</td><td>{p[0]}</td><td>{sum(p[5])}</td><td>{p[4]}</td><td>{p[8][0]}</td><td>{p[9]}</td></tr>")
h.append("</table><h2 class='page-break'>The 20 examples in detail</h2>")
for n, p in enumerate(P, 1):
    t, desc, age, g, sure, rights, learn, study, game, helped, liked, improve = p
    h.append(f"<div class='m'><h3>Example {n} · {t}</h3><div class='small'>{desc} Age {age}, {g}. Consent: Yes. Score {sum(rights)}/6, how sure {sure}/5.</div>")
    h.append("<table><tr><th>Knowledge question</th><th>Answer</th><th></th></tr>")
    for i, r in enumerate(rights):
        a, ok = answer(i, r)
        h.append(f"<tr><td>Q{i+1}</td><td>{a}</td><td class='{'ok' if ok else 'no'}'>{'✓ correct' if ok else '✗ wrong'}</td></tr>")
    h.append("</table><table><tr><th style='width:70%'>How you learn (1–5)</th><th>Rating</th></tr>" + "".join(f"<tr><td>{r}</td><td>{v}</td></tr>" for r, v in zip(LEARN, learn)) + "</table>")
    h.append(f"<div class='small'><b>Usually studies with:</b> {', '.join(study)}</div>")
    h.append("<table><tr><th style='width:70%'>The game (1–5)</th><th>Rating</th></tr>" + "".join(f"<tr><td>{r}</td><td>{v}</td></tr>" for r, v in zip(GAME, game)) + "</table>")
    h.append(f"<div class='small'><b>Which helped more:</b> {helped} · <b>Liked most:</b> {liked} · <b>To improve:</b> {improve}</div></div>")
h.append("<p class='small'>Example document generated by Evaluation/source/make_examples.py. Not data from any participant.</p></body></html>")
open(os.path.join(os.path.dirname(__file__), "Example_Answers.html"), "w", encoding="utf-8").write("\n".join(h))
print("wrote source/Example_Answers.html")
