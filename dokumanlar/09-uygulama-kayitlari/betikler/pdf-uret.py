#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Markdown -> PDF (Türkçe karakterli, tablolu). Kullanım:
       python3 betikler/pdf-uret.py <girdi.md> <cikti.pdf> ["Alt başlık"]
   Bağımlılık: reportlab (kurulu). Font: /Library/Fonts/Arial Unicode.ttf (tam Unicode).
   Desteklenen markdown: # başlıklar, | tablolar |, - listeler, 1. listeler, > alıntı,
   ``` kod blokları, **kalın**, `kod`, [metin](bağlantı), --- ayraç.
"""
import re
import sys
import os
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (BaseDocTemplate, Frame, PageTemplate, Paragraph,
                                Spacer, Table, TableStyle, KeepTogether, HRFlowable)

FONT_ADAYLARI = [
    ("/Library/Fonts/Arial Unicode.ttf", None),
    ("/System/Library/Fonts/Supplemental/Arial.ttf", "/System/Library/Fonts/Supplemental/Arial Bold.ttf"),
]
MONO_ADAYLARI = ["/System/Library/Fonts/Menlo.ttc", "/System/Library/Fonts/Supplemental/Courier New.ttf"]


def font_kur():
    for normal, kalin in FONT_ADAYLARI:
        if os.path.exists(normal):
            pdfmetrics.registerFont(TTFont("Gov", normal))
            if kalin and os.path.exists(kalin):
                pdfmetrics.registerFont(TTFont("GovB", kalin))
            else:
                pdfmetrics.registerFont(TTFont("GovB", normal))
            return "Gov", "GovB"
    return "Helvetica", "Helvetica-Bold"


def mono_kur(yedek):
    for yol in MONO_ADAYLARI:
        if os.path.exists(yol) and yol.endswith(".ttf"):
            pdfmetrics.registerFont(TTFont("GovM", yol))
            return "GovM"
    return yedek


NORMAL, KALIN = font_kur()
MONO = mono_kur(NORMAL)

# ── Markdown satır içi -> reportlab işaretlemesi ──────────────────────────────


def kacis(m):
    return m.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def satir_ici(m):
    m = kacis(m)
    # Kod aralıkları önce yer tutucuya alınır: içlerindeki * ve _ işaretleri kalın/italik
    # sayılmasın (ör. `Pages/*.razor`) — aksi hâlde reportlab "</font> saw instead of </i>" ile kırılır.
    kodlar = []
    def _kod(mt):
        kodlar.append(mt.group(1))
        return "\x00%d\x00" % (len(kodlar) - 1)
    m = re.sub(r"`([^`]+)`", _kod, m)
    m = re.sub(r"\*\*([^*]+)\*\*", r'<font face="%s">\1</font>' % KALIN, m)
    m = re.sub(r"(?<!\*)\*([^*\n]+)\*(?!\*)", r"<i>\1</i>", m)
    m = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", r"\1", m)   # bağlantı metni kalır, URL düşer
    m = re.sub(r"\x00(\d+)\x00", lambda mt: '<font face="%s" size="8.5">%s</font>' % (MONO, kodlar[int(mt.group(1))]), m)
    return m


def stiller(taban):
    return {
        "h1": ParagraphStyle("h1", fontName=KALIN, fontSize=17, leading=21, spaceBefore=2, spaceAfter=7, textColor=colors.HexColor("#15305c")),
        "h2": ParagraphStyle("h2", fontName=KALIN, fontSize=12.5, leading=16, spaceBefore=11, spaceAfter=5, textColor=colors.HexColor("#15305c")),
        "h3": ParagraphStyle("h3", fontName=KALIN, fontSize=10.8, leading=14, spaceBefore=8, spaceAfter=4, textColor=colors.HexColor("#33465e")),
        "p": ParagraphStyle("p", fontName=NORMAL, fontSize=9, leading=12.6, spaceAfter=4.5, alignment=TA_LEFT),
        "li": ParagraphStyle("li", fontName=NORMAL, fontSize=9, leading=12.6, spaceAfter=2.5, leftIndent=9, bulletIndent=2),
        "alinti": ParagraphStyle("alinti", fontName=NORMAL, fontSize=8.6, leading=12, leftIndent=7, rightIndent=5,
                                 spaceBefore=3, spaceAfter=6, textColor=colors.HexColor("#3d3d3d"),
                                 borderPadding=(5, 5, 5, 5), backColor=colors.HexColor("#f2f5f9")),
        "kod": ParagraphStyle("kod", fontName=MONO, fontSize=8, leading=10.5, leftIndent=6,
                              backColor=colors.HexColor("#f4f4f4"), borderPadding=(4, 4, 4, 4), spaceAfter=5),
        "hucre": ParagraphStyle("hucre", fontName=NORMAL, fontSize=7.6, leading=9.8),
        "hucreB": ParagraphStyle("hucreB", fontName=KALIN, fontSize=7.6, leading=9.8, textColor=colors.white),
    }


S = stiller(NORMAL)


def tablo_yap(satirlar, genislik):
    hucreler = []
    for i, ham in enumerate(satirlar):
        parcalar = [h.strip() for h in ham.strip().strip("|").split("|")]
        stil = S["hucreB"] if i == 0 else S["hucre"]
        hucreler.append([Paragraph(satir_ici(p), stil) for p in parcalar])
    sutun = max(len(r) for r in hucreler)
    for r in hucreler:
        while len(r) < sutun:
            r.append(Paragraph("", S["hucre"]))
    t = Table(hucreler, colWidths=[genislik / sutun] * sutun, repeatRows=1)
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#15305c")),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#c3cbd6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 3.5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 3.5),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#f6f8fb")]),
    ]))
    return t


def akis(md, genislik):
    ogeler = []
    satirlar = md.split("\n")
    i = 0
    while i < len(satirlar):
        s = satirlar[i]
        d = s.strip()
        if not d:
            i += 1
            continue
        if d.startswith("```"):
            blok = []
            i += 1
            while i < len(satirlar) and not satirlar[i].strip().startswith("```"):
                blok.append(kacis(satirlar[i]))
                i += 1
            i += 1
            ogeler.append(Paragraph("<br/>".join(blok) or " ", S["kod"]))
            continue
        if d.startswith("|") and i + 1 < len(satirlar) and re.match(r"^\|[\s:\-|]+\|$", satirlar[i + 1].strip()):
            blok = [d]
            i += 2
            while i < len(satirlar) and satirlar[i].strip().startswith("|"):
                blok.append(satirlar[i].strip())
                i += 1
            ogeler.append(Spacer(1, 3))
            ogeler.append(tablo_yap(blok, genislik))
            ogeler.append(Spacer(1, 6))
            continue
        if re.match(r"^(-{3,}|\*{3,})$", d):
            ogeler.append(HRFlowable(width="100%", thickness=0.5, color=colors.HexColor("#c3cbd6"), spaceBefore=5, spaceAfter=5))
            i += 1
            continue
        m = re.match(r"^(#{1,4})\s+(.*)$", d)
        if m:
            seviye = len(m.group(1))
            stil = S["h1"] if seviye == 1 else (S["h2"] if seviye == 2 else S["h3"])
            ogeler.append(Paragraph(satir_ici(m.group(2)), stil))
            i += 1
            continue
        if d.startswith(">"):
            blok = []
            while i < len(satirlar) and satirlar[i].strip().startswith(">"):
                blok.append(satirlar[i].strip()[1:].strip())
                i += 1
            ogeler.append(Paragraph(satir_ici(" ".join(blok)), S["alinti"]))
            continue
        m = re.match(r"^[-*]\s+(.*)$", d)
        if m:
            ogeler.append(Paragraph(satir_ici(m.group(1)), S["li"], bulletText="•"))
            i += 1
            continue
        m = re.match(r"^(\d+)[.)]\s+(.*)$", d)
        if m:
            ogeler.append(Paragraph(satir_ici(m.group(2)), S["li"], bulletText=m.group(1) + "."))
            i += 1
            continue
        ogeler.append(Paragraph(satir_ici(d), S["p"]))
        i += 1
    return ogeler


def uret(girdi, cikti, altbilgi):
    md = open(girdi, encoding="utf-8").read()
    kenar = 15 * mm
    doc = BaseDocTemplate(cikti, pagesize=A4, leftMargin=kenar, rightMargin=kenar,
                          topMargin=14 * mm, bottomMargin=15 * mm, title=os.path.basename(girdi))
    genislik = doc.width

    def sayfa(kanvas, belge):
        kanvas.saveState()
        kanvas.setFont(NORMAL, 7.5)
        kanvas.setFillColor(colors.HexColor("#6b7683"))
        kanvas.drawString(kenar, 9 * mm, altbilgi)
        kanvas.drawRightString(A4[0] - kenar, 9 * mm, "Sayfa %d" % belge.page)
        kanvas.setStrokeColor(colors.HexColor("#c3cbd6"))
        kanvas.setLineWidth(0.4)
        kanvas.line(kenar, 12 * mm, A4[0] - kenar, 12 * mm)
        kanvas.restoreState()

    doc.addPageTemplates([PageTemplate(id="tek", frames=[Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="g")], onPage=sayfa)])
    doc.build(akis(md, genislik))
    return os.path.getsize(cikti)


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("kullanım: python3 pdf-uret.py <girdi.md> <cikti.pdf> [altbilgi]")
        sys.exit(2)
    alt = sys.argv[3] if len(sys.argv) > 3 else "ByGatech A.Ş. — Kuyumcu ERP"
    boyut = uret(sys.argv[1], sys.argv[2], alt)
    print("PDF yazıldı: %s (%s bayt) · font: %s" % (sys.argv[2], f"{boyut:,}".replace(",", "."), NORMAL))
