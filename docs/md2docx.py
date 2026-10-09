# Usage : python docs/md2docx.py docs/GDD.md docs/GDD.docx  (à relancer après chaque modification du GDD)
# Convertit docs/GDD.md en .docx (titres, paragraphes, listes, gras/italique, tableau), sans dépendance.
import re, sys, zipfile
from xml.sax.saxutils import escape
src, dst = sys.argv[1], sys.argv[2]
lines = open(src, encoding='utf-8').read().split('\n')
ACC = '2E7D32'
def runs(t, size=None, color=None):
    out = []
    for part in re.split(r'(\*\*[^*]+\*\*|_[^_]+_(?=\W|$))', t):
        if not part: continue
        b = part.startswith('**'); i = part.startswith('_') and part.endswith('_') and len(part) > 2
        txt = part[2:-2] if b else part[1:-1] if i else part
        rp = ''.join([ '<w:b/>' if b else '', '<w:i/>' if i else '',
                       f'<w:color w:val="{color}"/>' if color else '', f'<w:sz w:val="{size}"/>' if size else ''])
        out.append(f'<w:r><w:rPr>{rp}</w:rPr><w:t xml:space="preserve">{escape(txt)}</w:t></w:r>')
    return ''.join(out)
def para(t, style=None, ind=0, bullet=False, after=80, size=None, color=None, keep=False):
    ppr = f'<w:pStyle w:val="{style}"/>' if style else ''
    if keep: ppr += '<w:keepNext/>'
    ppr += f'<w:spacing w:after="{after}"/>'
    if bullet: ppr += f'<w:ind w:left="{360+ind*360}" w:hanging="240"/>'
    elif ind: ppr += f'<w:ind w:left="{ind*360}"/>'
    pre = f'<w:r><w:t xml:space="preserve">{"•" if ind==0 else "◦"}\t</w:t></w:r>' if bullet else ''
    if bullet: ppr += f'<w:tabs><w:tab w:val="left" w:pos="{360+ind*360}"/></w:tabs>'
    return f'<w:p><w:pPr>{ppr}</w:pPr>{pre}{runs(t,size,color)}</w:p>'
def table(rows):
    n = len(rows[0]); w = 9000 // n
    x = f'<w:tbl><w:tblPr><w:tblW w:w="9000" w:type="dxa"/><w:tblBorders>' + ''.join(
        f'<w:{s} w:val="single" w:sz="4" w:color="BBBBBB"/>' for s in ('top','left','bottom','right','insideH','insideV')) + \
        '</w:tblBorders><w:tblCellMar><w:left w:w="80" w:type="dxa"/><w:right w:w="80" w:type="dxa"/></w:tblCellMar></w:tblPr><w:tblGrid>' + \
        ''.join(f'<w:gridCol w:w="{w}"/>' for _ in range(n)) + '</w:tblGrid>'
    for k, r in enumerate(rows):
        x += '<w:tr>' + ('<w:trPr><w:tblHeader/></w:trPr>' if k == 0 else '')
        for c in r:
            c = c.replace('✅', '✓')
            shade = f'<w:shd w:val="clear" w:color="auto" w:fill="{ACC}"/>' if k == 0 else ''
            x += f'<w:tc><w:tcPr><w:tcW w:w="{w}" w:type="dxa"/>{shade}</w:tcPr>' + \
                 para(c, after=0, size=18, color='FFFFFF' if k == 0 else None).replace('<w:rPr>', '<w:rPr><w:b/>' if k == 0 else '<w:rPr>') + '</w:tc>'
        x += '</w:tr>'
    return x + '</w:tbl>' + para('', after=60)
body = []; i = 0
while i < len(lines):
    l = lines[i]; s = l.strip()
    if s.startswith('|'):
        rows = []
        while i < len(lines) and lines[i].strip().startswith('|'):
            cells = [c.strip() for c in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r'-+', c) for c in cells): rows.append(cells)
            i += 1
        body.append(table(rows)); continue
    if s.startswith('# '): body.append(para(s[2:], 'Title', after=120))
    elif s.startswith('## '): body.append(para(s[3:], 'Heading1', keep=True))
    elif re.match(r'^\s*- ', l):
        ind = (len(l) - len(l.lstrip())) // 2
        body.append(para(s[2:], bullet=True, ind=ind, after=40))
    elif s: body.append(para(s, ind=1 if l.startswith('  ') else 0))
    i += 1
doc = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>'
       + ''.join(body) + '<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134" w:header="567" w:footer="567" w:gutter="0"/></w:sectPr></w:body></w:document>')
styles = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri" w:cs="Calibri"/><w:sz w:val="21"/><w:lang w:val="fr-FR"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after="80" w:line="264" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults>
<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>
<w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:basedOn w:val="Normal"/><w:rPr><w:b/><w:color w:val="{ACC}"/><w:sz w:val="40"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:pPr><w:keepNext/><w:spacing w:before="240" w:after="80"/><w:pBdr><w:bottom w:val="single" w:sz="6" w:space="2" w:color="{ACC}"/></w:pBdr><w:outlineLvl w:val="0"/></w:pPr><w:rPr><w:b/><w:color w:val="{ACC}"/><w:sz w:val="28"/></w:rPr></w:style>
</w:styles>'''
ct = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/></Types>'''
rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>'''
drels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>'''
with zipfile.ZipFile(dst, 'w', zipfile.ZIP_DEFLATED) as z:
    z.writestr('[Content_Types].xml', ct); z.writestr('_rels/.rels', rels)
    z.writestr('word/document.xml', doc); z.writestr('word/styles.xml', styles); z.writestr('word/_rels/document.xml.rels', drels)
print('ok', dst)
