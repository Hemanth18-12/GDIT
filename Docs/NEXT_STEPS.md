# Next steps: after the ending, future work, and how to build and submit

## (a) What the player sees and can do after finishing
1. When the treasures are returned to the priest, the **ending screen** shows:
   - The ending (Dawn Light if before 05:45, Late Dawn if later, Lost Dawn if the boss escaped) and the closing line: "Every temple holds a story. Every artifact carries a memory. Protecting heritage means protecting our history."
   - Treasures recovered (out of 5), every piece of evidence found, and who was named as the inside man.
   - Points and a grade (S, A, B or C) for each place, the Dawn Clock time, play time, total score, overall grade and hints used.
   - **New (2026-11-19):** the run is saved in a best-ten table, and the screen says which number this run is.
2. **Enter** returns to the main menu.
3. From the menu the player can:
   - start a **New Game**: zone scores reset, but the best-runs table stays;
   - **Continue** from the last save;
   - see **Credits**, which now include the five best runs on this computer;
   - change **Settings** or look at **Controls**.
4. **Fixed today:**
   - Finishing before dawn now really gives the "Dawn Light" ending. Before, it always said "Late Dawn" because it checked a flag from the old story.
   - Runs are now saved to the high-score table. The table existed but nothing ever saved to it, and no screen showed it.
   - The Credits text described the old three-item story; it now matches V3.
5. **Not built:**
   - there is no separate credits roll after the ending;
   - there is no "new game plus" or chapter select (the F9 demo menu does that in development builds).

## (b) Future work (realistic)
1. **Real character models for the robbers and villagers.** The robbers are fantasy sculpts (decision D-04). Adding face rigs would let the Pujari, villagers and robbers move their mouths when they speak.
2. **More animation.** Kick, grab and knockdown clips (for example from Mixamo) for richer fights. Today these moves do not exist in the project.
3. **Modelled environments.** The places are textured blocks plus Blender hero props. Modular Blender kits for walls, roofs and stairs would look much better. Baked lighting needs the zones saved into the scene instead of built at run time.
4. **The Sun Wall timing beat and the vault guards' patrol stealth** from the design (decisions D-16 and D-17) were simplified. Adding them back would deepen two puzzles.
5. **A second platform and a release build.** Windows build, shader stripping, a smaller download (the macOS development build is 925 MB), controller-only play testing.
6. **Accessibility and polish.** Remappable keys, a colour-blind check for the light puzzles, recorded voice actors instead of synthetic voices.

## (c) How to build and submit
**Build:**
1. Close the game if it is running.
2. In Unity (open the `My project` folder), use the project's build method, or from Terminal in the repository folder run:
   ```
   /Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity -batchmode -quit \
     -projectPath "$PWD/My project" -executeMethod LegacyOfTemples.World.Editor.AuditBuildTools.Build -logFile /tmp/build.log
   ```
3. The game is written to **`Builds/LegacyOfTheTemple.app`**. `Docs/QA/last-build.txt` must say `Succeeded: errors=0, warnings=0`.
4. It is a **development build**, so the F9 demo menu works. For a release build, change `BuildOptions.Development` in `Assets/_Project/Scripts/World/Editor/AuditBuildTools.cs` (line 35) to `BuildOptions.None`. **Check this before the viva**, if your college wants a release build.

**Run:** double-click `Builds/LegacyOfTheTemple.app`, then choose New Game. If macOS blocks it the first time, right-click it and choose Open.

**Hand in (suggested; check your college's rules):**
1. The game: zip `Builds/LegacyOfTheTemple.app` (`Builds/` is not in git; about 925 MB before zipping).
2. The source: the git repository (`Assets`, `Packages`, `ProjectSettings`, `Tools`, `Docs`). Do not include `Library/`, `Temp/` or `Builds/`.
3. Documents: `Docs/README.md`, `Docs/GAME_DESIGN_DOCUMENT.md`, `Docs/TECHNICAL_REPORT.md`, `Docs/TEST_REPORT.md`, `Docs/GAME_EXPLAINED.md`, `Docs/KNOWN_ISSUES.md`, `ATTRIBUTION.md`.
4. The video: `Docs/QA/full_run.mov`.
