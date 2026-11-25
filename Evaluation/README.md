# Evaluation (playtest study pack for Legacy of the Temple)

## Current game route — corrected on 25 November 2026

The report, editable PPTX and PDF preview now describe the established village, Courtyard and Sun Wall locations confirmed by the team. New Game starts in Courtyard. The presentation keeps the supplied Heritage Mystery template and contains updated speaker notes. Technical results now use the separate 32/32 wider and 5/5 focused Editor runs, the rebuilt app, and the E-interaction regression. Player-study results remain pending. See `../Docs/QA/2026-11-25-e-interaction/evidence-review.md` for build identity, reproduced failure, corrected behaviour and limits.

## New evaluation deliverables — 25 November 2026

- `Study_Methodology_and_Results.pdf`: four-page report following the supplied example's structure, rewritten for this game. Player results are pending; technical evidence is reported separately.
- `Legacy_of_the_Temple_Evaluation.pptx`: editable ten-slide presentation adapted from the supplied Heritage Mystery template, with Hemanth, Aakash and Sai Teja's names/IDs and speaker notes. Open in PowerPoint or import into Canva.
- `Legacy_of_the_Temple_Evaluation.pdf`: rendered presentation preview.
- `Updated_Project_Summary.txt`: corrected implementation/verification summary.
- `source/Study_Methodology_and_Results.html`: editable report source.
- `source/Presentation_Speaker_Notes.txt`: the spoken scripts as a separate editable text file.

The report separates technical QA from participant results. The current original-route app was rebuilt and inspected; the E animation failure and its correction are retained as evidence. No player responses or survey statistics have been invented.

To regenerate the documents, run `python3 Evaluation/source/build_evaluation.py` from the project root. Render the report HTML with the installed Chrome (headless print-to-PDF, no browser headers/footers). Export the PPTX preview using `osascript Evaluation/source/export_presentation.applescript` with the installed Keynote. These steps do not install software or modify the supplied template.

The ten-slide outline and speaker scripts cover the problem, core loop, six-zone world, physical puzzles, Jeep, navigation/checkpoints, audio, scoped QA results and conclusion. A later player-study round can fill the report's pending fields. The website is deferred at the team's request.

Made in the same style as the reference pack (comparison study, information sheet, consent form, before and after forms), but about this game, with real screenshots from the game.

| File | What it is |
|---|---|
| `Comparison_Study.pdf` | Why, what and how the game teaches; comparison with textbooks, heritage visits, videos, quiz apps and other games; research summary; the study plan (results box left empty until the playtests) |
| `Information_Sheet.pdf` | For participants to read first |
| `Consent_Form.pdf` | For participants to sign |
| `Google_Forms_Script.gs` | Creates both Google Forms (Before you play: Set A; After you play: Set B), each with its own response sheet |
| `Form_Questions.md` | Every question in both forms, with the right answers marked, to check before running the script |
| `images/` | The 10 game screenshots used (copied from `Docs/Walkthrough/shots`, resized, not edited) |
| `source/` | The HTML and CSS of the PDFs, and `make_forms.py`, which writes the form script and question list |

## Fill in before printing
- `[Name] ([Roll no])` for each team member and `[Guide name]`: in `source/Comparison_Study.html` and `source/Information_Sheet.html`. Then rebuild the PDFs:
  `"/Applications/Brave Browser.app/Contents/MacOS/Brave Browser" --headless=new --no-pdf-header-footer --print-to-pdf="$PWD/Evaluation/Comparison_Study.pdf" "file://$PWD/Evaluation/source/Comparison_Study.html"` (same for the other two).
- After the playtests, fill in the results box in section 6.

## Create the Google Forms (about 2 minutes)
1. Sign in to Google as **hemanth.t24@iiits.in**.
2. Open https://script.google.com and click **New project**.
3. Delete the sample code, paste the whole of `Google_Forms_Script.gs`, and press Save.
4. Choose **createStudyForms** in the function list and click **Run**. Allow the permissions it asks for (Forms, Sheets).
5. Open the **Execution log**: it prints, for each form, the link to share with participants, the edit link and the response sheet.

The 12 knowledge questions are quiz questions worth 1 point each, with the right answer already marked, so Google Forms scores Set A and Set B by itself. To change a question, edit `source/make_forms.py`, run `python3 Evaluation/source/make_forms.py`, and run the script again.
Note: a form made in an iiits.in account is normally open only to iiits.in accounts; classmates can answer, but outsiders cannot.

## The forms (created 2026-11-23 in hemanth.t24@iiits.in)
| Form | Link for participants | Edit | Responses |
|---|---|---|---|
| Before you play (Set A) | https://docs.google.com/forms/d/e/1FAIpQLScDuCTgIpMZsvvElM1mkK8lOaDln8LGI1HtdTIDmlf7ulPcTw/viewform | https://docs.google.com/forms/d/15GE7E0LfsHvQsmtHEt3z0BTZHCnKKOLfKttmqczzFmA/edit | https://docs.google.com/spreadsheets/d/1yTRGQHHqU1_TVosbjktKQYvxKBvme9nVpowrSnWRtnk/edit |
| After you play (Set B) | https://docs.google.com/forms/d/e/1FAIpQLSelyyYj5LAECpRnVd_u-cE71FUMs6n5bghgvzxYjIwQdXAS3g/viewform | https://docs.google.com/forms/d/1yiber_2PEN3gm3R7qwJ9zmsnVs52fr9Y9Vi-m4g6Ayk/edit | https://docs.google.com/spreadsheets/d/1dvjz1JAtcMcxL0Al9xqmrOL5uLodOcPRt_MHtbKHdAg/edit |
Links copied from the Apps Script execution log screenshot; check one opens before sharing.
