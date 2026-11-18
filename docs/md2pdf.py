# Usage : python docs/md2pdf.py docs/RAPPORT_SAE.md docs/RAPPORT_SAE.pdf  (à relancer après chaque modification)
# Convertit un .md simple (titres, paragraphes, listes, tableaux, gras/italique/code) en HTML,
# puis l'imprime en PDF avec Edge (ou Chrome) en mode headless. Aucune dépendance Python.
import html, os, re, subprocess, sys, tempfile

src, dst = sys.argv[1], sys.argv[2]
lines = open(src, encoding='utf-8').read().split('\n')

def inline(t):
    t = html.escape(t, quote=False)
    t = re.sub(r'`([^`]+)`', r'<code>\1</code>', t)
    t = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', t)
    t = re.sub(r'(?<!\w)_([^_]+)_(?!\w)', r'<em>\1</em>', t)
    t = re.sub(r'\[([^\]]+)\]\([^)]+\)', r'\1', t)   # liens : on garde le texte
    return t

out, i = [], 0
while i < len(lines):
    l = lines[i]
    if not l.strip():
        i += 1; continue
    m = re.match(r'(#{1,3}) (.*)', l)
    if m:
        n = len(m.group(1)); out.append(f'<h{n}>{inline(m.group(2))}</h{n}>'); i += 1; continue
    if l.startswith('|'):                       # tableau : 1re ligne = en-tête, 2e = séparateur
        rows = []
        while i < len(lines) and lines[i].startswith('|'):
            rows.append([c.strip() for c in lines[i].strip().strip('|').split('|')]); i += 1
        t = '<table><thead><tr>' + ''.join(f'<th>{inline(c)}</th>' for c in rows[0]) + '</tr></thead><tbody>'
        for r in rows[2:]:
            t += '<tr>' + ''.join(f'<td>{inline(c)}</td>' for c in r) + '</tr>'
        out.append(t + '</tbody></table>'); continue
    if l.startswith('- '):
        items = []
        while i < len(lines) and lines[i].startswith('- '):
            items.append(f'<li>{inline(lines[i][2:])}</li>'); i += 1
        out.append('<ul>' + ''.join(items) + '</ul>'); continue
    para = []                                   # paragraphe : lignes jusqu'à une ligne vide ou un bloc
    while i < len(lines) and lines[i].strip() and not re.match(r'(#{1,3} |\||- )', lines[i]):
        para.append(inline(lines[i])); i += 1
    out.append('<p>' + '<br>'.join(para) + '</p>')

css = '''
@page { size: A4; margin: 16mm 15mm; }
body { font-family: "Segoe UI", Arial, sans-serif; font-size: 10pt; line-height: 1.45; color: #1d1d1d; }
h1 { font-size: 21pt; color: #2E7D32; margin: 0 0 4px; }
h2 { font-size: 13.5pt; color: #2E7D32; border-bottom: 2px solid #2E7D32; padding-bottom: 2px; margin: 18px 0 8px; break-after: avoid; }
h3 { font-size: 11pt; margin: 12px 0 4px; break-after: avoid; }
p { margin: 0 0 7px; }
ul { margin: 0 0 8px; padding-left: 18px; }
li { margin-bottom: 3px; }
code { font-family: Consolas, monospace; font-size: 8.8pt; background: #f1f4f1; padding: 0 3px; border-radius: 3px; }
table { border-collapse: collapse; width: 100%; font-size: 8.4pt; margin: 6px 0 10px; break-inside: auto; }
th, td { border: 1px solid #c9d3c9; padding: 4px 5px; vertical-align: top; text-align: left; }
th { background: #2E7D32; color: #fff; }
tr { break-inside: avoid; }
td:first-child { font-weight: 600; background: #f3f7f3; width: 14%; }
'''
page = f'<!doctype html><html lang="fr"><head><meta charset="utf-8"><title>{os.path.basename(src)}</title><style>{css}</style></head><body>{"".join(out)}</body></html>'

tmp = os.path.join(tempfile.gettempdir(), 'md2pdf_' + os.path.basename(src) + '.html')
open(tmp, 'w', encoding='utf-8').write(page)
browsers = [r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
            r'C:\Program Files\Google\Chrome\Application\chrome.exe']
exe = next(b for b in browsers if os.path.exists(b))
subprocess.run([exe, '--headless=new', '--disable-gpu', '--no-pdf-header-footer',
                f'--print-to-pdf={os.path.abspath(dst)}', 'file:///' + tmp.replace('\\', '/')], check=True)
print('ok', dst)
