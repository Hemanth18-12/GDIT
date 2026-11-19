# Builds Docs/GAME_WALKTHROUGH.pdf from the real screenshots in Docs/Walkthrough/shots and the text in walkthrough_content.py.
# Run with a Python that has pypdf, reportlab and pillow:  python Tools/docs/build_walkthrough.py
# Steps: downscale shots to 1600 px JPEG (copies; the PNGs are not changed) -> HTML -> PDF with headless Brave/Chrome
# -> find each section's page -> rebuild with the table of contents filled in -> stamp page numbers.
import html, io, os, re, subprocess, sys, tempfile
from PIL import Image
from pypdf import PdfReader, PdfWriter
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.dirname(__file__))
import walkthrough_content as C

SHOTS = os.path.join(ROOT, "Docs", "Walkthrough", "shots")
OUT = os.path.join(ROOT, "Docs", "GAME_WALKTHROUGH.pdf")
WORK = tempfile.mkdtemp(prefix="walkthrough_")
BROWSERS = ["/Applications/Brave Browser.app/Contents/MacOS/Brave Browser", "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"]

def img(name):
    src = os.path.join(SHOTS, name)
    if not os.path.exists(src): raise SystemExit("missing shot: " + name)
    dst = os.path.join(WORK, os.path.splitext(name)[0] + ".jpg")
    if not os.path.exists(dst):
        im = Image.open(src).convert("RGB")
        if im.width > 1600: im = im.resize((1600, round(im.height * 1600 / im.width)), Image.LANCZOS)
        im.save(dst, "JPEG", quality=84, optimize=True)
    return "file://" + dst

def t(s):  # escape, then allow **bold** and `code`
    s = html.escape(s, quote=False)
    s = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", s)
    return re.sub(r"`([^`]+)`", r"<code>\1</code>", s)

CSS = """
@page { size: A4 portrait; margin: 14mm 14mm 17mm 14mm; }
* { box-sizing: border-box; }
body { font-family: 'Avenir Next', 'Helvetica Neue', Arial, sans-serif; font-size: 11.5pt; line-height: 1.42; color: #2a1d12; margin: 0; }
h1, h2, h3 { font-family: 'Iowan Old Style', 'Palatino', Georgia, serif; color: #4a2e14; }
h2 { font-size: 21pt; margin: 0 0 6px; padding-bottom: 4px; border-bottom: 2.5px solid #b8912f; }
h3 { font-size: 14pt; margin: 14px 0 6px; color: #6b4418; break-after: avoid; page-break-after: avoid; }
.section { page-break-before: always; }
.cover { height: 260mm; background: linear-gradient(160deg, #2b1a0c 0%, #4a2e14 55%, #6b4418 100%); color: #f3e2b5; padding: 30mm 18mm; border: 3px solid #b8912f; }
.cover h1 { font-size: 40pt; color: #e8c766; margin: 30mm 0 4mm; letter-spacing: 2px; }
.cover .sub { font-size: 20pt; color: #f3e2b5; margin-bottom: 18mm; }
.cover .meta { font-size: 12.5pt; line-height: 1.8; color: #e9d7a8; }
.cover img { width: 100%; border: 2px solid #b8912f; margin-top: 10mm; }
.box { background: #fbf4e2; border: 1.5px solid #b8912f; border-left: 6px solid #b8912f; padding: 7px 11px; margin: 8px 0; border-radius: 3px; }
.box b.k { color: #7a4f12; }
.shot { margin: 8px 0 12px; page-break-inside: avoid; }
.shot img { width: 100%; border: 1px solid #6b4418; display: block; }
.lab { position: relative; }
.badge { position: absolute; transform: translate(-50%, -50%); background: #b8912f; color: #2b1a0c; font-weight: 700; font-size: 10pt; padding: 2px 7px; border-radius: 10px; border: 2px solid #2b1a0c; white-space: nowrap; }
.shot .cap { font-size: 11pt; margin-top: 4px; }
.shot .cap b { color: #7a4f12; }
.row { display: flex; gap: 10px; }
.row .shot { flex: 1; }
.row .shot .cap { font-size: 10.5pt; }
table { border-collapse: collapse; width: 100%; margin: 6px 0; font-size: 10.8pt; }
td, th { border: 1px solid #c9a95a; padding: 4px 7px; vertical-align: top; }
th { background: #4a2e14; color: #f3e2b5; text-align: left; }
tr:nth-child(even) td { background: #fbf4e2; }
ol, ul { margin: 4px 0 6px 0; padding-left: 22px; }
li { margin: 2px 0; }
code { background: #f1e6c8; padding: 0 3px; font-size: 10pt; }
.toc td { border: none; border-bottom: 1px dotted #b8912f; padding: 5px 4px; font-size: 12pt; }
.toc td.p { text-align: right; width: 18mm; }
.note { font-size: 10.5pt; color: #5a4630; }
.keep { page-break-inside: avoid; }
"""

CUR = {"steps": True}
def shot_html(s, n):
    badges = "".join(f'<span class="badge" style="left:{x}%;top:{y}%">{t(txt)}</span>' for x, y, txt in s.get("labels", []))
    pic = f'<div class="lab"><img src="{img(s["file"])}">{badges}</div>' if badges else f'<img src="{img(s["file"])}">'
    lead = f"<b>Step {n}.</b> " if CUR["steps"] else ""
    return f'<div class="shot">{pic}<div class="cap">{lead}{t(s["caption"])}</div></div>'

def section_html(sec):
    CUR["steps"] = sec.get("steps", True)
    h = [f'<div class="section" id="{sec["id"]}"><h2>{t(sec["title"])}</h2>']
    for para in sec.get("intro", []): h.append(f"<p>{t(para)}</p>")
    if "objective" in sec: h.append(f'<div class="box"><b class="k">Objective:</b> {t(sec["objective"])}</div>')
    for blk in sec.get("blocks", []):
        kind = blk[0]
        if kind == "h": h.append(f"<h3>{t(blk[1])}</h3>")
        elif kind == "p": h.append(f"<p>{t(blk[1])}</p>")
        elif kind == "ol": h.append('<ol class="keep">' + "".join(f"<li>{t(x)}</li>" for x in blk[1]) + "</ol>")
        elif kind == "ul": h.append('<ul class="keep">' + "".join(f"<li>{t(x)}</li>" for x in blk[1]) + "</ul>")
        elif kind == "table":
            rows = blk[1]; h.append('<table class="keep"><tr>' + "".join(f"<th>{t(c)}</th>" for c in rows[0]) + "</tr>" + "".join("<tr>" + "".join(f"<td>{t(c)}</td>" for c in r) + "</tr>" for r in rows[1:]) + "</table>")
        elif kind == "box": h.append(f'<div class="box">{t(blk[1])}</div>')
        elif kind == "shots":
            n0 = blk[2] if len(blk) > 2 else 1
            for i, s in enumerate(blk[1]): h.append(shot_html(s, n0 + i))
        elif kind == "pair":
            n0 = blk[2] if len(blk) > 2 else 1
            h.append('<div class="row">' + "".join(shot_html(s, n0 + i) for i, s in enumerate(blk[1])) + "</div>")
        elif kind == "note": h.append(f'<p class="note">{t(blk[1])}</p>')
    h.append("</div>")
    return "\n".join(h)

def build_html(pages):
    toc = "".join(f'<tr><td>{t(s["title"])}</td><td class="p">{pages.get(s["id"], "")}</td></tr>' for s in C.SECTIONS if s.get("toc", True))
    cover = C.COVER
    body = [f"""<div class="cover"><h1>{t(cover['title'])}</h1><div class="sub">{t(cover['subtitle'])}</div>
<div class="meta">{'<br>'.join(t(x) for x in cover['meta'])}</div><img src="{img(cover['image'])}"></div>""",
            f'<div class="section" id="toc"><h2>Contents</h2><table class="toc">{toc}</table><p class="note">{t(C.TOC_NOTE)}</p></div>']
    body += [section_html(s) for s in C.SECTIONS]
    return f"<!doctype html><html><head><meta charset='utf-8'><title>{t(cover['title'])} walkthrough</title><style>{CSS}</style></head><body>{''.join(body)}</body></html>"

def render(html_text, pdf_path):
    hp = os.path.join(WORK, "walkthrough.html"); open(hp, "w", encoding="utf-8").write(html_text)
    browser = next(b for b in BROWSERS if os.path.exists(b))
    subprocess.run([browser, "--headless=new", "--disable-gpu", "--no-pdf-header-footer", "--allow-file-access-from-files", f"--print-to-pdf={pdf_path}", "file://" + hp],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=300)

def section_pages(pdf_path):
    r = PdfReader(pdf_path); found = {}
    for i, p in enumerate(r.pages):
        txt = (p.extract_text() or "").replace("\n", " ")
        for s in C.SECTIONS:
            if s["id"] not in found and txt.strip().startswith(s["title"][:30]): found[s["id"]] = i + 1
    return found, len(r.pages)

def stamp(src, dst):
    r = PdfReader(src); w = PdfWriter(clone_from=r)
    for i, page in enumerate(w.pages):
        if i > 0:
            buf = io.BytesIO(); W, H = float(page.mediabox.width), float(page.mediabox.height)
            c = canvas.Canvas(buf, pagesize=(W, H)); c.setFont("Helvetica", 9.5); c.setFillColorRGB(0.42, 0.27, 0.09)
            c.drawCentredString(W / 2, 22, f"{C.COVER['title']}: complete walkthrough  -  page {i + 1} of {len(w.pages)}"); c.save()
            buf.seek(0); page.merge_page(PdfReader(buf).pages[0])
    with open(dst, "wb") as f: w.write(f)

if __name__ == "__main__":
    p1 = os.path.join(WORK, "pass1.pdf"); render(build_html({}), p1)
    pages, n1 = section_pages(p1)
    p2 = os.path.join(WORK, "pass2.pdf"); render(build_html(pages), p2)
    pages2, n2 = section_pages(p2)
    missing = [s["id"] for s in C.SECTIONS if s["id"] not in pages2]
    stamp(p2, OUT)
    print(f"pages {n2} (pass 1: {n1}), size {os.path.getsize(OUT) / 1e6:.1f} MB, sections found {len(pages2)}/{len(C.SECTIONS)}, missing {missing}")
    print("toc:", pages2 == pages, pages2)
