-- Read the generated PowerPoint and export a PDF for visual inspection.
-- Do not save back to the supplied template or the generated PowerPoint.
set sourceFile to POSIX file "/Users/hemanth/Desktop/GDIT/Evaluation/Legacy_of_the_Temple_Evaluation.pptx"
set pdfFile to POSIX file "/Users/hemanth/Desktop/GDIT/Evaluation/Legacy_of_the_Temple_Evaluation.pdf"
tell application id "com.apple.Keynote"
    launch
    delay 2
    set deck to open sourceFile
    delay 2
    export deck to pdfFile as PDF
    close deck saving no
end tell
