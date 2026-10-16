#!/usr/bin/env python3
# ═══════════════════════════════════════════════════════════════════════════
# MARKDOWN → PDF (yerel, üçüncü tarafa hiçbir şey gönderilmez)
#
# NEDEN: MALI-MUSAVIR-SORULARI.md CANLI bir belgedir; soru eklendikçe PDF'i
# yeniden üretilecek. Makinede pandoc/weasyprint/wkhtmltopdf YOK, cupsfilter
# text/html → pdf çeviremiyor ("filtre yok"). reportlab VAR.
#
# ⚠ TÜRKÇE: reportlab'in gömülü Helvetica'sı Latin-1'dir; ı ğ ş İ Ğ Ş YOKTUR.
#   Bu yüzden sistemden TTF yüklenir (DejaVu → Arial Unicode → Arial sırasıyla).
#
# Kullanım:  python3 betikler/md-pdf.py <girdi.md> <cikti.pdf>
# ═══════════════════════════════════════════════════════════════════════════
import sys, os, re, io

from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table,
                                TableStyle, HRFlowable, KeepTogether)

ADAYLAR = [
    ("/System/Library/Fonts/Supplemental/DejaVuSans.ttf",
     "/System/Library/Fonts/Supplemental/DejaVuSans-Bold.ttf"),
    ("/Library/Fonts/Arial Unicode.ttf", "/Library/Fonts/Arial Unicode.ttf"),
    ("/System/Library/Fonts/Supplemental/Arial.ttf",
     "/System/Library/Fonts/Supplemental/Arial Bold.ttf"),
    ("/System/Library/Fonts/Supplemental/Verdana.ttf",
     "/System/Library/Fonts/Supplemental/Verdana Bold.ttf"),
]


def yazi_tipi_kur():
    """Türkçe karakter taşıyan bir TTF bul ve kaydet. Bulunamazsa DUR (Madde 1:
    bozuk karakterli PDF sessizce üretilmez)."""
    for duz, kalin in ADAYLAR:
        if os.path.exists(duz):
            pdfmetrics.registerFont(TTFont("Govde", duz))
            pdfmetrics.registerFont(TTFont("GovdeKalin", kalin if os.path.exists(kalin) else duz))
            return os.path.basename(duz)
    sys.exit("HATA: Türkçe karakter taşıyan TTF bulunamadı; PDF ÜRETİLMEDİ "
             "(bozuk karakterli belge üretmektense durmak doğrudur).")


KACIS = [("&", "&amp;"), ("<", "&lt;"), (">", "&gt;")]


# Arial Unicode'da OLMAYAN isaretler (olculdu 23.09: kutu basiyordu) → metin karsiligi.
# ⚠ Lira isareti ₺ bir KUYUMCU belgesinde kutu olarak basilamaz; "TL" yazilir.
YOK_ISARET = {
    "\u20ba": "TL", "\U0001f534": "[ONCELIK 1]", "\U0001f7e1": "[ONCELIK 2]",
    "\u26aa": "[BILGI]", "\u2713": "EVET", "\u2192": "->", "\u2190": "<-",
}


def satir_ici(t):
    for a, b in YOK_ISARET.items():
        t = t.replace(a, b)
    for a, b in KACIS:
        t = t.replace(a, b)
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<i>\1</i>", t)
    t = re.sub(r"`(.+?)`", r'<font face="Courier" size="8.5">\1</font>', t)
    return t


def uret(md_yolu, pdf_yolu):
    ad = yazi_tipi_kur()
    md = io.open(md_yolu, encoding="utf-8").read()

    S = {
        "govde": ParagraphStyle("govde", fontName="Govde", fontSize=9.5, leading=13.5,
                                spaceAfter=4, textColor=colors.HexColor("#172033")),
        "h1": ParagraphStyle("h1", fontName="GovdeKalin", fontSize=17, leading=21,
                             spaceAfter=6, textColor=colors.HexColor("#14213d")),
        "h2": ParagraphStyle("h2", fontName="GovdeKalin", fontSize=12, leading=16,
                             spaceBefore=12, spaceAfter=5,
                             textColor=colors.HexColor("#14213d")),
        "h3": ParagraphStyle("h3", fontName="GovdeKalin", fontSize=10.5, leading=14,
                             spaceBefore=8, spaceAfter=3),
        "alinti": ParagraphStyle("alinti", fontName="Govde", fontSize=9.5, leading=13.5,
                                 leftIndent=10, spaceBefore=4, spaceAfter=4,
                                 borderPadding=(6, 6, 6, 8),
                                 backColor=colors.HexColor("#eef1f6"),
                                 textColor=colors.HexColor("#172033")),
        "madde": ParagraphStyle("madde", fontName="Govde", fontSize=9.5, leading=13.5,
                                leftIndent=12, bulletIndent=3, spaceAfter=2),
    }

    akis, tablo = [], []

    def tabloyu_bosalt():
        if not tablo:
            return
        veri = [[Paragraph(satir_ici(h), S["govde"]) for h in satir] for satir in tablo]
        t = Table(veri, hAlign="LEFT", colWidths=None, repeatRows=1)
        t.setStyle(TableStyle([
            ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#d7dce4")),
            ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#f2f4f7")),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("LEFTPADDING", (0, 0), (-1, -1), 5),
            ("RIGHTPADDING", (0, 0), (-1, -1), 5),
            ("TOPPADDING", (0, 0), (-1, -1), 4),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ]))
        akis.append(t)
        akis.append(Spacer(1, 6))
        tablo.clear()

    for ham in md.split("\n"):
        s = ham.rstrip()
        if s.startswith("|"):
            h = [c.strip() for c in s.strip("|").split("|")]
            if set("".join(h)) <= set("-: "):
                continue
            tablo.append(h)
            continue
        tabloyu_bosalt()
        if s.startswith("### "):
            akis.append(Paragraph(satir_ici(s[4:]), S["h3"]))
        elif s.startswith("## "):
            akis.append(Paragraph(satir_ici(s[3:]), S["h2"]))
        elif s.startswith("# "):
            akis.append(Paragraph(satir_ici(s[2:]), S["h1"]))
        elif s.startswith("---"):
            akis.append(Spacer(1, 3))
            akis.append(HRFlowable(width="100%", thickness=0.5,
                                   color=colors.HexColor("#d7dce4")))
            akis.append(Spacer(1, 5))
        elif s.strip() == ">":
            pass                      # bos alinti satiri cizilmez
        elif s.startswith("> "):
            akis.append(Paragraph(satir_ici(s[2:]), S["alinti"]))
        elif s.startswith(("- ", "* ")):
            akis.append(Paragraph(satir_ici(s[2:]), S["madde"], bulletText="•"))
        elif re.match(r"^\d+\. ", s):
            n, _, kalan = s.partition(". ")
            akis.append(Paragraph(satir_ici(kalan), S["madde"], bulletText=n + "."))
        elif s.strip() == "":
            akis.append(Spacer(1, 3))
        else:
            akis.append(Paragraph(satir_ici(s), S["govde"]))
    tabloyu_bosalt()

    def sayfa(kanvas, belge):
        kanvas.saveState()
        kanvas.setFont("Govde", 7.5)
        kanvas.setFillColor(colors.HexColor("#8a94a6"))
        kanvas.drawString(16 * mm, 10 * mm, "Mihenk · ByGatech A.Ş.")
        kanvas.drawRightString(A4[0] - 16 * mm, 10 * mm, "Sayfa %d" % kanvas.getPageNumber())
        kanvas.restoreState()

    SimpleDocTemplate(
        pdf_yolu, pagesize=A4,
        leftMargin=16 * mm, rightMargin=16 * mm,
        topMargin=16 * mm, bottomMargin=16 * mm,
        title=os.path.basename(md_yolu).replace(".md", ""),
        author="ByGatech A.Ş.",
    ).build(akis, onFirstPage=sayfa, onLaterPages=sayfa)
    return ad


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit("kullanım: md-pdf.py <girdi.md> <cikti.pdf>")
    font = uret(sys.argv[1], sys.argv[2])
    print("PDF üretildi: %s (yazı tipi: %s)" % (sys.argv[2], font))
