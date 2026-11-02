<?xml version="1.0" encoding="UTF-8"?>
<!--
  ByGatech e-Fatura / e-Arşiv fatura görüntü şablonu (UBL-TR 1.2.1 → HTML). 20.10.2026.
  Belgenin yasal aslı XML'dir; bu şablon yalnız görüntüdür. Entegratöre faturayla birlikte gider
  (Nilvera: InvoiceInfo.TemplateBase64String). XSLT 1.0 — entegratör motorlarıyla uyumlu kalsın diye
  uzantı fonksiyonu YOK. Kuyum satırında ayar · gram · has bilgisi kalemin açıklamasından (cbc:Description) gelir.
  GİB logosu: $gibLogo parametresi doluysa resim, boşsa idarenin adı yazıyla.
-->
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:inv="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
    xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
    xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
    exclude-result-prefixes="inv cac cbc">
  <xsl:output method="html" encoding="UTF-8" indent="no" doctype-system="about:legacy-compat"/>
  <xsl:decimal-format name="tr" decimal-separator="," grouping-separator="."/>
  <xsl:param name="gibLogo" select="''"/>

  <xsl:variable name="f" select="/inv:Invoice"/>
  <xsl:variable name="earsiv" select="$f/cbc:ProfileID = 'EARSIVFATURA'"/>
  <xsl:variable name="pb" select="$f/cbc:DocumentCurrencyCode"/>

  <xsl:template name="para">
    <xsl:param name="d"/>
    <xsl:value-of select="format-number($d, '#.##0,00', 'tr')"/>
    <xsl:text> </xsl:text>
    <xsl:choose>
      <xsl:when test="$pb = 'TRY' or $pb = 'TL' or not($pb)">TL</xsl:when>
      <xsl:otherwise><xsl:value-of select="$pb"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="tarih">
    <xsl:param name="d"/>
    <xsl:value-of select="concat(substring($d, 9, 2), '.', substring($d, 6, 2), '.', substring($d, 1, 4))"/>
  </xsl:template>

  <!-- Taraf bloğu: unvan (ya da ad soyad), adres, vergi dairesi, VKN/TCKN, iletişim -->
  <xsl:template name="taraf">
    <xsl:param name="p"/>
    <div class="ad">
      <xsl:choose>
        <xsl:when test="normalize-space($p/cac:PartyName/cbc:Name)"><xsl:value-of select="$p/cac:PartyName/cbc:Name"/></xsl:when>
        <xsl:otherwise><xsl:value-of select="concat($p/cac:Person/cbc:FirstName, ' ', $p/cac:Person/cbc:FamilyName)"/></xsl:otherwise>
      </xsl:choose>
    </div>
    <xsl:variable name="a" select="$p/cac:PostalAddress"/>
    <xsl:if test="normalize-space(concat($a/cbc:StreetName, $a/cbc:CitySubdivisionName, $a/cbc:CityName))">
      <div>
        <xsl:value-of select="$a/cbc:StreetName"/>
        <xsl:if test="normalize-space($a/cbc:CitySubdivisionName)"><xsl:text> </xsl:text><xsl:value-of select="$a/cbc:CitySubdivisionName"/></xsl:if>
        <xsl:if test="normalize-space($a/cbc:CityName)"><xsl:text> / </xsl:text><xsl:value-of select="$a/cbc:CityName"/></xsl:if>
      </div>
    </xsl:if>
    <xsl:for-each select="$p/cac:PartyIdentification/cbc:ID[@schemeID = 'VKN' or @schemeID = 'TCKN']">
      <div>
        <xsl:if test="normalize-space($p/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name)">
          <xsl:value-of select="$p/cac:PartyTaxScheme/cac:TaxScheme/cbc:Name"/><xsl:text> V.D. · </xsl:text>
        </xsl:if>
        <xsl:value-of select="@schemeID"/><xsl:text>: </xsl:text><b><xsl:value-of select="."/></b>
      </div>
    </xsl:for-each>
    <xsl:if test="normalize-space($p/cac:Contact/cbc:Telephone) or normalize-space($p/cac:Contact/cbc:ElectronicMail)">
      <div class="soluk">
        <xsl:value-of select="$p/cac:Contact/cbc:Telephone"/>
        <xsl:if test="normalize-space($p/cac:Contact/cbc:Telephone) and normalize-space($p/cac:Contact/cbc:ElectronicMail)"> · </xsl:if>
        <xsl:value-of select="$p/cac:Contact/cbc:ElectronicMail"/>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template match="/">
    <html lang="tr">
      <head>
        <meta charset="UTF-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <title><xsl:value-of select="$f/cbc:ID"/></title>
        <style>
          @page { size: A4; margin: 14mm; }
          * { box-sizing: border-box; }
          body { margin: 0; background: #fff; color: #1d1d1f; font: 10pt/1.45 "Inter", "Segoe UI", system-ui, -apple-system, Arial, sans-serif; font-variant-numeric: tabular-nums; }
          .sayfa { max-width: 190mm; margin: 0 auto; padding: 8mm 0; }
          .ust { display: table; width: 100%; border-bottom: 2px solid #1d1d1f; padding-bottom: 5mm; }
          .ust > div { display: table-cell; vertical-align: top; }
          .satici { width: 58%; }
          .satici .ad { font-size: 14pt; font-weight: 700; letter-spacing: -.01em; margin-bottom: 1mm; }
          .tur { width: 42%; text-align: right; }
          .gib { font-size: 7.5pt; letter-spacing: .06em; text-transform: uppercase; color: #6e6e73; line-height: 1.3; }
          .gib img { height: 16mm; }
          .rozet { display: inline-block; margin-top: 3mm; padding: 2.2mm 4mm; border: 1.5px solid #1d1d1f; border-radius: 2mm;
                   font-size: 13pt; font-weight: 700; letter-spacing: .08em; }
          .orta { display: table; width: 100%; margin-top: 6mm; }
          .orta > div { display: table-cell; vertical-align: top; }
          .alici { width: 55%; padding-right: 6mm; }
          .etiket { font-size: 7.5pt; letter-spacing: .1em; text-transform: uppercase; color: #6e6e73; margin-bottom: 1.5mm; }
          .alici .ad { font-size: 11.5pt; font-weight: 600; }
          .bilgi table { width: 100%; border-collapse: collapse; }
          .bilgi td { padding: 1.2mm 0; border-bottom: 1px solid #e5e5ea; font-size: 9pt; }
          .bilgi td:first-child { color: #6e6e73; width: 38%; }
          .bilgi td:last-child { text-align: right; font-weight: 600; }
          .ettn { font-family: ui-monospace, Menlo, Consolas, monospace; font-size: 8pt; font-weight: 500; word-wrap: break-word; }
          table.kalem { width: 100%; border-collapse: collapse; margin-top: 7mm; }
          .kalem th { font-size: 7.5pt; letter-spacing: .08em; text-transform: uppercase; color: #6e6e73; font-weight: 600;
                      text-align: right; padding: 2mm 1.5mm; border-bottom: 1.5px solid #1d1d1f; }
          .kalem th.sol, .kalem td.sol { text-align: left; }
          .kalem td { padding: 2.4mm 1.5mm; border-bottom: 1px solid #e5e5ea; text-align: right; vertical-align: top; }
          .kalem .urun { font-weight: 600; }
          .kalem .ayrinti { color: #6e6e73; font-size: 8.5pt; margin-top: .5mm; }
          .alt { display: table; width: 100%; margin-top: 6mm; }
          .alt > div { display: table-cell; vertical-align: top; }
          .notlar { width: 55%; padding-right: 6mm; font-size: 8.5pt; color: #3a3a3c; }
          .notlar p { margin: 0 0 1.5mm; }
          .toplam table { width: 100%; border-collapse: collapse; }
          .toplam td { padding: 1.4mm 0; font-size: 9.5pt; }
          .toplam td:last-child { text-align: right; }
          .toplam tr.genel td { border-top: 1.5px solid #1d1d1f; padding-top: 2.5mm; font-size: 12.5pt; font-weight: 700; }
          .iletim { margin-top: 8mm; padding-top: 3mm; border-top: 1px solid #e5e5ea; font-size: 8pt; color: #6e6e73; }
          .imza { float: right; letter-spacing: .04em; }
          .soluk { color: #6e6e73; }
          @media (max-width: 600px) {
            body { font-size: 9pt; }
            .sayfa { padding: 4mm 3mm; }
            .ust > div, .orta > div, .alt > div { display: block; width: 100%; padding-right: 0; }
            .tur { text-align: left; margin-top: 4mm; }
            .bilgi { margin-top: 5mm; }
            /* Kalem kartı: başlık satırı gizlenir, her hücre kendi etiketiyle (data-e) satır olur. */
            .kalem tr.baslik { display: none; }
            .kalem, .kalem tbody, .kalem tr, .kalem td { display: block; width: 100%; }
            .kalem tr { border-bottom: 1px solid #e5e5ea; padding: 2mm 0; }
            .kalem td { border: 0; padding: .6mm 0; text-align: right; }
            .kalem td[data-e]:before { content: attr(data-e); float: left; color: #6e6e73; }
            .kalem td.sira { display: none; }
            .kalem td.sol { text-align: left; }
            .toplam { margin-top: 5mm; }
          }
        </style>
      </head>
      <body>
        <div class="sayfa">
          <div class="ust">
            <div class="satici">
              <xsl:call-template name="taraf"><xsl:with-param name="p" select="$f/cac:AccountingSupplierParty/cac:Party"/></xsl:call-template>
            </div>
            <div class="tur">
              <div class="gib">
                <xsl:choose>
                  <xsl:when test="normalize-space($gibLogo)"><img alt="Gelir İdaresi Başkanlığı" src="{$gibLogo}"/></xsl:when>
                  <xsl:otherwise>T.C. Hazine ve Maliye Bakanlığı<br/>Gelir İdaresi Başkanlığı</xsl:otherwise>
                </xsl:choose>
              </div>
              <div class="rozet">
                <xsl:choose>
                  <xsl:when test="$earsiv">e-ARŞİV FATURA</xsl:when>
                  <xsl:otherwise>e-FATURA</xsl:otherwise>
                </xsl:choose>
              </div>
            </div>
          </div>

          <div class="orta">
            <div class="alici">
              <div class="etiket">Sayın</div>
              <xsl:call-template name="taraf"><xsl:with-param name="p" select="$f/cac:AccountingCustomerParty/cac:Party"/></xsl:call-template>
            </div>
            <div class="bilgi">
              <table>
                <tr><td>Fatura no</td><td><xsl:value-of select="$f/cbc:ID"/></td></tr>
                <tr><td>Tarih</td><td>
                  <xsl:call-template name="tarih"><xsl:with-param name="d" select="$f/cbc:IssueDate"/></xsl:call-template>
                  <xsl:if test="normalize-space($f/cbc:IssueTime)"><xsl:text> </xsl:text><xsl:value-of select="substring($f/cbc:IssueTime, 1, 5)"/></xsl:if>
                </td></tr>
                <tr><td>Senaryo</td><td><xsl:value-of select="$f/cbc:ProfileID"/></td></tr>
                <tr><td>Fatura tipi</td><td><xsl:value-of select="$f/cbc:InvoiceTypeCode"/></td></tr>
                <tr><td>ETTN</td><td class="ettn"><xsl:value-of select="$f/cbc:UUID"/></td></tr>
              </table>
            </div>
          </div>

          <table class="kalem">
            <tr class="baslik">
              <th class="sol">Sıra</th><th class="sol">Mal / hizmet</th><th>Miktar</th><th>Birim fiyat</th><th>KDV</th><th>KDV tutarı</th><th>Tutar</th>
            </tr>
            <xsl:for-each select="$f/cac:InvoiceLine">
              <tr>
                <td class="sol sira"><xsl:value-of select="cbc:ID"/></td>
                <td class="sol">
                  <div class="urun"><xsl:value-of select="cac:Item/cbc:Name"/></div>
                  <xsl:if test="normalize-space(cac:Item/cbc:Description)"><div class="ayrinti"><xsl:value-of select="cac:Item/cbc:Description"/></div></xsl:if>
                  <xsl:for-each select="cbc:Note[normalize-space(.)]"><div class="ayrinti"><xsl:value-of select="."/></div></xsl:for-each>
                </td>
                <td data-e="Miktar">
                  <xsl:value-of select="format-number(cbc:InvoicedQuantity, '#.##0,###', 'tr')"/>
                  <xsl:text> </xsl:text>
                  <xsl:choose>
                    <xsl:when test="cbc:InvoicedQuantity/@unitCode = 'C62'">adet</xsl:when>
                    <xsl:when test="cbc:InvoicedQuantity/@unitCode = 'GRM'">g</xsl:when>
                    <xsl:when test="cbc:InvoicedQuantity/@unitCode = 'KGM'">kg</xsl:when>
                    <xsl:otherwise><xsl:value-of select="cbc:InvoicedQuantity/@unitCode"/></xsl:otherwise>
                  </xsl:choose>
                </td>
                <td data-e="Birim fiyat"><xsl:call-template name="para"><xsl:with-param name="d" select="cac:Price/cbc:PriceAmount"/></xsl:call-template></td>
                <td data-e="KDV">%<xsl:value-of select="format-number(cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode = '0015'][1]/cbc:Percent, '0', 'tr')"/></td>
                <td data-e="KDV tutarı"><xsl:call-template name="para"><xsl:with-param name="d" select="sum(cac:TaxTotal/cbc:TaxAmount)"/></xsl:call-template></td>
                <td data-e="Tutar"><xsl:call-template name="para"><xsl:with-param name="d" select="cbc:LineExtensionAmount"/></xsl:call-template></td>
              </tr>
            </xsl:for-each>
          </table>

          <xsl:variable name="t" select="$f/cac:LegalMonetaryTotal"/>
          <div class="alt">
            <div class="notlar">
              <xsl:for-each select="$f/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory[normalize-space(cbc:TaxExemptionReason)]">
                <p><b>Özel matrah / istisna:</b><xsl:text> </xsl:text>
                  <xsl:value-of select="cbc:TaxExemptionReasonCode"/><xsl:text> · </xsl:text><xsl:value-of select="cbc:TaxExemptionReason"/></p>
              </xsl:for-each>
              <xsl:for-each select="$f/cbc:Note[normalize-space(.)]"><p><xsl:value-of select="."/></p></xsl:for-each>
            </div>
            <div class="toplam">
              <table>
                <tr><td>Mal / hizmet toplamı</td><td><xsl:call-template name="para"><xsl:with-param name="d" select="$t/cbc:LineExtensionAmount"/></xsl:call-template></td></tr>
                <xsl:if test="number($t/cbc:AllowanceTotalAmount) &gt; 0">
                  <tr><td>İskonto</td><td><xsl:call-template name="para"><xsl:with-param name="d" select="$t/cbc:AllowanceTotalAmount"/></xsl:call-template></td></tr>
                </xsl:if>
                <xsl:for-each select="$f/cac:TaxTotal/cac:TaxSubtotal">
                  <tr>
                    <td>
                      <xsl:value-of select="cac:TaxCategory/cac:TaxScheme/cbc:Name"/>
                      <xsl:if test="cbc:Percent"> %<xsl:value-of select="format-number(cbc:Percent, '0', 'tr')"/></xsl:if>
                      <xsl:if test="number(cbc:TaxableAmount) &gt; 0">
                        <span class="soluk"> (matrah <xsl:call-template name="para"><xsl:with-param name="d" select="cbc:TaxableAmount"/></xsl:call-template>)</span>
                      </xsl:if>
                    </td>
                    <td><xsl:call-template name="para"><xsl:with-param name="d" select="cbc:TaxAmount"/></xsl:call-template></td>
                  </tr>
                </xsl:for-each>
                <tr class="genel"><td>Ödenecek tutar</td><td><xsl:call-template name="para"><xsl:with-param name="d" select="$t/cbc:PayableAmount"/></xsl:call-template></td></tr>
              </table>
            </div>
          </div>

          <div class="iletim">
            <xsl:if test="$earsiv and normalize-space($f/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail)">
              e-Arşiv izni kapsamında elektronik ortamda iletilmiştir.
            </xsl:if>
            <span class="imza">ByGatech</span>
          </div>
        </div>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
