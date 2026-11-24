"""Writes Evaluation/Google_Forms_Script.gs (paste into script.google.com to create both Google Forms)
and Evaluation/Form_Questions.md (the same questions, to read and check). One source for both.
Run: python3 Evaluation/source/make_forms.py"""
import json, os

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.dirname(HERE)
GAME = "Legacy of the Temple"
LIKERT = ["1 - Strongly disagree", "2", "3", "4", "5 - Strongly agree"]

# the six knowledge topics; Set A before playing, Set B after (same idea, new scene or numbers). The first choice marked * is correct.
SET_A = [
    ("A candle stands on the floor in front of a small stone statue, with a wall behind the statue. You move the candle closer to the statue. What happens to the statue's shadow on the wall?",
     ["*It gets bigger", "It gets smaller", "It stays the same size", "It disappears"]),
    ("A sunbeam hits a flat mirror at 40° from the line straight out of the mirror (the normal). At what angle from that line does the beam leave the mirror?",
     ["20°", "*40°", "80°", "90°"]),
    ("Two water tanks are joined by a pipe at the bottom. You pour water into the left tank only. When the water has settled, how do the levels compare?",
     ["The left tank is higher", "The right tank is higher", "*Both are at the same level", "The right tank stays empty"]),
    ("A storeroom lock is scratched only on the inside, and the door was not forced. What is the most likely explanation?",
     ["A thief broke it from the outside", "*Someone inside opened it", "The door was never locked", "The wind opened it"]),
    ("A stone disc has 6 stops (0 to 5) and turns only forward, one stop per push. It is at stop 1 and must reach stop 5. How many pushes are needed?",
     ["2", "*4", "5", "6"]),
    ("You find an old carved stone half-buried near a temple. What is the best thing to do?",
     ["Take it home to keep it safe", "Clean it with a wire brush", "*Leave it where it is and tell the temple or the local authorities", "Sell it to a collector"]),
]
SET_B = [
    ("A lamp hangs above a clay pot on the floor. You lift the lamp higher. What happens to the pot's shadow on the floor?",
     ["*It gets smaller", "It gets bigger", "It stays the same size", "It moves onto the wall"]),
    ("You turn a flat mirror by 10° while a beam of light is reflecting off it. By how much does the reflected beam turn?",
     ["5°", "10°", "*20°", "40°"]),
    ("A wooden raft floats in a stepwell tank. You drain the tank until the water level drops by 1 m. What happens to the raft?",
     ["*It goes down 1 m and still floats", "It stays where it was", "It sinks to the bottom", "It goes down 2 m"]),
    ("Four clues were found: a list in neat handwriting, scratches inside a lock, a ledger signed \"M.\" and a temple key ring. What is the best way to name a suspect?",
     ["Pick whoever was standing nearest", "*Pick the one person that all the clues point to", "Pick the person with the most money", "Pick someone at random"]),
    ("A dial has 8 stops (0 to 7) and turns only forward, one stop per push. It is at stop 6 and must reach stop 1. How many pushes are needed?",
     ["*3", "5", "7", "2"]),
    ("The priest says the stolen temple pieces \"carry the community's memory\". Which reason best explains why they are priceless?",
     ["The gold in them", "Their price at a market", "*The history and meaning they hold for the people", "Only how old they are"]),
]
LEARN_ROWS = [
    "I enjoy learning about history and heritage.",
    "I enjoy learning science (light, water, forces).",
    "I find it hard to stay focused while studying.",
    "I understand a topic better when I solve a problem than when I read about it.",
    "I think games can help people learn.",
    "I feel confident working out what happened from clues.",
]
GAME_ROWS = [
    "The game was fun.",
    "The puzzles were too hard.",
    "The controls were easy to learn.",
    "I always knew what I had to do next.",
    "The hints (H) helped me when I was stuck.",
    "The voices and subtitles were clear.",
    "The puzzles helped me understand the ideas behind them (light, water, clues).",
    "The story made me care about protecting heritage.",
    "I would like to keep playing the game.",
    "I would recommend this game to a friend.",
]
STUDY = ["Textbook", "Class notes", "Videos", "Quiz apps", "Group study", "Museum or heritage visits", "Other"]

def knowledge(set_name, qs, intro):
    return {"page": f"Section 2 · Quick knowledge check (Set {set_name})", "help": intro,
            "items": [{"type": "quiz", "title": q, "choices": c} for q, c in qs]
                     + [{"type": "scale", "title": "How sure were you of your answers above?", "low": "Guessing", "high": "Very sure"}]}

PRE = {"title": f"{GAME} · Before you play",
       "description": "Thank you for taking part! A few questions before you play (about 5 minutes). Your answers are linked only to your participant ID.",
       "sections": [
           {"page": None, "items": [
               {"type": "text", "title": "Roll no"},
               {"type": "text", "title": "Participant ID (for example P001)"},
               {"type": "choice", "title": "I have read the information sheet and signed the consent form", "choices": ["Yes", "No"]},
               {"type": "text", "title": "Age", "required": False},
               {"type": "choice", "title": "Gender", "choices": ["Male", "Female"]},
               {"type": "choice", "title": "How often do you play video games?", "choices": ["Never", "A few times a year", "Monthly", "Weekly", "Daily"]},
               {"type": "choice", "title": "Have you played a 3D game with a keyboard and mouse before?", "choices": ["Yes", "No"]},
               {"type": "choice", "title": "Have you visited a historic temple or monument in the last year?", "choices": ["Yes", "No"]}]},
           knowledge("A", SET_A, "Six short puzzles. Each question is worth 1 point. Please do not look anything up; guessing is fine."),
           {"page": "Section 3 · How you learn", "help": "1 = Strongly disagree, 5 = Strongly agree.", "items": [
               {"type": "grid", "title": "How you learn", "rows": LEARN_ROWS, "cols": LIKERT},
               {"type": "checkbox", "title": "How do you usually study?", "choices": STUDY, "required": False}]}]}

POST = {"title": f"{GAME} · After you play",
        "description": "Thanks for playing! A few last questions (about 5 minutes).",
        "sections": [
            {"page": None, "items": [
                {"type": "text", "title": "Roll no"},
                {"type": "choice", "title": "I have read the information sheet and signed the consent form", "choices": ["Yes", "No"]},
                {"type": "text", "title": "Age", "required": False},
                {"type": "choice", "title": "Gender", "choices": ["Male", "Female"]}]},
            knowledge("B", SET_B, "The same six ideas with a new scene or new numbers. Each question is worth 1 point."),
            {"page": "Section 3 · How you learn", "help": "Use the same scale as before: 1 = Strongly disagree, 5 = Strongly agree.", "items": [
                {"type": "grid", "title": "How you learn", "rows": LEARN_ROWS, "cols": LIKERT},
                {"type": "checkbox", "title": "How do you usually study?", "choices": STUDY, "required": False}]},
            {"page": "Section 4 · The game", "help": "For each statement, select 1 = Strongly disagree through 5 = Strongly agree.", "items": [
                {"type": "grid", "title": "Rate the following statements", "rows": GAME_ROWS, "cols": LIKERT},
                {"type": "choice", "title": "Which helped you understand the topics more?", "choices": ["The game", "My usual way of studying", "Both the same", "Not sure"]},
                {"type": "paragraph", "title": "What did you like most?"},
                {"type": "paragraph", "title": "What was confusing or should be improved?"}]}]}

GS = r"""/**
 * Creates the Google Form "After you play" for the Legacy of the Temple playtest study, plus its response spreadsheet.
 * HOW TO USE: sign in to Google as the study account, open https://script.google.com, New project,
 * delete the sample code, paste this whole file, Save, choose the function createStudyForms, Run,
 * allow the permissions, then open View > Logs (or Execution log): it prints the form's links.
 * The knowledge questions are quiz questions (1 point each, the right answer marked), so Google Forms scores them.
 * Generated by Evaluation/source/make_forms.py: change the questions there, not here.
 */
var FORMS = __DATA__;

function createStudyForms() {
  FORMS.forEach(function (spec) {
    var form = FormApp.create(spec.title);
    form.setDescription(spec.description);
    form.setIsQuiz(true);
    form.setShowLinkToRespondAgain(false);
    form.setProgressBar(true);
    form.setConfirmationMessage('Thank you! Please tell the researcher you have finished.');
    spec.sections.forEach(function (sec) {
      if (sec.page) { var pb = form.addPageBreakItem().setTitle(sec.page); if (sec.help) pb.setHelpText(sec.help); }
      sec.items.forEach(function (q) { addItem(form, q); });
    });
    var sheet = SpreadsheetApp.create(spec.title + ' (responses)');
    form.setDestination(FormApp.DestinationType.SPREADSHEET, sheet.getId());
    Logger.log(spec.title + '\n  share with participants: ' + form.getPublishedUrl() + '\n  edit: ' + form.getEditUrl() + '\n  responses: ' + sheet.getUrl());
  });
}

function addItem(form, q) {
  var required = q.required !== false, item;
  if (q.type === 'text') item = form.addTextItem().setTitle(q.title);
  else if (q.type === 'paragraph') item = form.addParagraphTextItem().setTitle(q.title);
  else if (q.type === 'choice') { item = form.addMultipleChoiceItem().setTitle(q.title); item.setChoiceValues(q.choices); }
  else if (q.type === 'checkbox') { item = form.addCheckboxItem().setTitle(q.title); item.setChoiceValues(q.choices); }
  else if (q.type === 'scale') item = form.addScaleItem().setTitle(q.title).setBounds(1, 5).setLabels(q.low, q.high);
  else if (q.type === 'grid') item = form.addGridItem().setTitle(q.title).setRows(q.rows).setColumns(q.cols);
  else if (q.type === 'quiz') {
    item = form.addMultipleChoiceItem().setTitle(q.title);
    item.setChoices(q.choices.map(function (c) { var ok = c.charAt(0) === '*'; return item.createChoice(ok ? c.substring(1) : c, ok); }));
    item.setPoints(1);
  }
  item.setRequired(required);
}
"""

def md():
    out = [f"# {GAME}: study forms", "", "The form is created by `Google_Forms_Script.gs`. Correct answers are marked **(correct)**; participants do not see them.", ""]
    for spec in (POST,):
        out += [f"## {spec['title']}", "", spec["description"], ""]
        for sec in spec["sections"]:
            out += [f"### {sec['page'] or 'Section 1 · About you'}", ""] + ([sec["help"], ""] if sec.get("help") else [])
            n = 0
            for q in sec["items"]:
                n += 1; req = "" if q.get("required", True) else " (optional)"
                if q["type"] == "quiz":
                    out.append(f"{n}. {q['title']}")
                    out += [f"   - {c[1:]} **(correct)**" if c.startswith("*") else f"   - {c}" for c in q["choices"]]
                elif q["type"] in ("choice", "checkbox"):
                    out.append(f"{n}. {q['title']}{req}" + (" (tick all that apply)" if q["type"] == "checkbox" else ""))
                    out += [f"   - {c}" for c in q["choices"]]
                elif q["type"] == "scale":
                    out.append(f"{n}. {q['title']} (1 = {q['low']} … 5 = {q['high']})")
                elif q["type"] == "grid":
                    out.append(f"{n}. {q['title']} (each row 1 = Strongly disagree … 5 = Strongly agree)")
                    out += [f"   - {r}" for r in q["rows"]]
                else:
                    out.append(f"{n}. {q['title']}{req} ({'short answer' if q['type'] == 'text' else 'long answer'})")
            out.append("")
    return "\n".join(out)

open(os.path.join(OUT, "Google_Forms_Script.gs"), "w", encoding="utf-8").write(GS.replace("__DATA__", json.dumps([POST], indent=1, ensure_ascii=False)))
open(os.path.join(OUT, "Form_Questions.md"), "w", encoding="utf-8").write(md())
print("wrote Google_Forms_Script.gs and Form_Questions.md")
