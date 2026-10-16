using System.Text.RegularExpressions;
using Bunit;
using Kuyumcu.Api.Modules.Meta;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Bilesenler;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-06 — DÜZENLENEBİLİRLİK ŞABLONU
// Kimlikler: B-4 · B-13 · B-18 · B-34 · B-54 · Y-15 (+ P-00 triyajı B-60/B-67/B-68/B-70)
//
// ⚠ Bu dosya MEKANİZMA sınar, ekran sayısı değil (PLAN § 1 md.1):
//   1. Mevcut SatirIciDuzenle sözleşmesi (Enter/Esc/blur) GERİLEMEZ — hasım R10
//   2. Enter TAM 1 istek üretir · Esc vazgeçer · açılışta odak ilk alandadır
//   3. Dolu formda İLK Kaydet istek üretir (B-34 blur/binding yarışı)
//   4. Her doğrulama hatası ALAN ALTINDA ve alan ANAHTARLI id ile çizilir
//   5. Yazma yapan her sayfa ya mühürlü kalıp taşır ya gerekçe ya KÜTÜKLÜ istisna
//
// ⚠ AŞKIN K4: negatif iddia tek başına test değildir — "istek yok" her zaman
//   pozitif eşiyle (alan altı mesaj VAR, düzenleme AÇIK kalır) çiftlenir.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class P06DuzenlenebilirlikTestleri : BunitContext
{
    public P06DuzenlenebilirlikTestleri()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DilServisi>();
    }

    // ═══ 1. SatirIciDuzenle — sözleşme gerilemesi (hasım R10) ════════════════

    private IRenderedComponent<SatirIciDuzenle> Satir(
        Action<string>? kaydedildi = null,
        string? deger = "Ahmet",
        decimal? asgari = null,
        decimal? azami = null)
        => Render<SatirIciDuzenle>(p =>
        {
            p.Add(x => x.Varlik, "cari").Add(x => x.Alan, "aciklama").Add(x => x.Id, "42")
             .Add(x => x.Etiket, "Açıklama").Add(x => x.Deger, deger)
             .Add(x => x.Kaydedildi, EventCallback.Factory.Create<string>(this, v => kaydedildi?.Invoke(v)));
            if (asgari is not null) p.Add(x => x.AsgariDeger, asgari.Value);
            if (azami is not null) p.Add(x => x.AzamiDeger, azami.Value);
        });

    /// <summary>P06-1 (B-13) — Enter TAM 1 istek üretir; ikiye katlamaz.</summary>
    [Fact]
    public void P06_ENTER_tam_bir_istek_uretir()
    {
        var sayac = 0; string? yazilan = null;
        var e = Satir(v => { yazilan = v; sayac++; });

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        e.Find("#si-cari-aciklama-42").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(1, sayac);
        Assert.Equal("Mehmet", yazilan);
    }

    /// <summary>P06-2 (B-13) — Esc VAZGEÇER: istek yok VE eski değer geri gelir.</summary>
    [Fact]
    public void P06_ESC_vazgecer_ve_ESKI_degere_doner()
    {
        var sayac = 0;
        var e = Satir(_ => sayac++);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        e.Find("#si-cari-aciklama-42").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, sayac);                                            // negatif
        Assert.Equal("Ahmet", e.Find("#si-cari-aciklama-42-ac").TextContent); // POZİTİF eş
    }

    /// <summary>
    /// P06-3 (B-13) — açılışta ODAK ilk alandadır. bUnit gerçek odağı taşımaz;
    /// burada sözleşmenin KOD karşılığı (girdi @ref + FocusAsync çağrısı ve
    /// AcilistaOdaklan varsayılanı) kilitlenir. document.activeElement kanıtı
    /// QA turunda GERÇEK TARAYICIYLA çapraz doğrulanır (plan DoD md.7).
    /// </summary>
    [Fact]
    public void P06_ACILISTA_ilk_alana_odaklanma_SOZLESMESI_kodda_vardir()
    {
        var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "SatirIciDuzenle.razor");

        Assert.Contains("@ref=\"girdiRef\"", kaynak, StringComparison.Ordinal);
        Assert.Contains("girdiRef.FocusAsync()", kaynak, StringComparison.Ordinal);
        Assert.Contains("odaklanilacak = AcilistaOdaklan", kaynak, StringComparison.Ordinal);
        // Varsayılan AÇIK olmalı — kapalı varsayılan sözleşmeyi sessizce boşaltırdı.
        Assert.Contains("public bool AcilistaOdaklan { get; set; } = true;", kaynak, StringComparison.Ordinal);
        // Odak düzenlemeyi AÇTIĞIMIZDA istenmelidir, kapandığında değil.
        var e = Satir();
        e.Find("#si-cari-aciklama-42-ac").Click();
        Assert.NotNull(e.Find("#si-cari-aciklama-42"));
    }

    /// <summary>
    /// P06-4 (B-34) — DOLU formda İLK Kaydet istek üretir.
    /// Gerçek tarayıcı sırası: girdide oninput → düğmede mousedown → girdide
    /// blur → düğmede click. Binding yarışı yüzünden ilk tık "yutulursa"
    /// kullanıcı iki kez tıklamak zorunda kalır (QA turu bulgusu).
    /// </summary>
    [Fact]
    public void P06_DOLU_formda_ILK_kaydet_istek_uretir()
    {
        var sayac = 0; string? yazilan = null;
        var e = Satir(v => { yazilan = v; sayac++; });

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("Mehmet");
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42").Blur();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Equal(1, sayac);            // ilk tık İSTEK ÜRETTİ (ne 0 ne 2)
        Assert.Equal("Mehmet", yazilan);   // ve YAZILAN değer taslak değil, son hâl
    }

    /// <summary>P06-5 (B-34/B-67) — boş gönderim SESSİZ kalmaz: alan altı mesaj + açık kalır.</summary>
    [Fact]
    public void P06_BOS_gonderimde_alan_alti_mesaj_cikar_ve_duzenleme_ACIK_kalir()
    {
        var sayac = 0;
        var e = Satir(_ => sayac++);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("   ");
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Equal(0, sayac);                                   // negatif
        var mesaj = e.Find("#si-cari-aciklama-42-hata");          // POZİTİF eş
        Assert.False(string.IsNullOrWhiteSpace(mesaj.TextContent));
        Assert.Equal("alert", mesaj.GetAttribute("role"));
        Assert.NotNull(e.Find("#si-cari-aciklama-42"));           // düzenleme AÇIK
    }

    // ═══ 2. B-18 — iş kuralı sınırı ALAN ALTINDA ════════════════════════════

    /// <summary>P06-6 (B-18) — aralık dışı değer alan altı mesaj verir, istek ATMAZ.</summary>
    [Fact]
    public void P06_ARALIK_disi_deger_alan_alti_mesaj_verir()
    {
        var sayac = 0;
        var e = Satir(_ => sayac++, deger: "10", asgari: 0m, azami: 100m);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("250");
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Equal(0, sayac);
        var mesaj = e.Find("#si-cari-aciklama-42-hata").TextContent;
        Assert.Contains("100", mesaj, StringComparison.Ordinal);   // ÜST SINIR yazılı
        Assert.Equal("true", e.Find("#si-cari-aciklama-42").GetAttribute("aria-invalid"));
    }

    /// <summary>P06-7 (B-18) — sınır İÇİNDEKİ değer geçer (kural aşırı sıkı değil).</summary>
    [Fact]
    public void P06_ARALIK_icindeki_deger_GECER()
    {
        var sayac = 0;
        var e = Satir(_ => sayac++, deger: "10", asgari: 0m, azami: 100m);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("100");   // üst sınır DÂHİLDİR
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Equal(1, sayac);
    }

    /// <summary>P06-8 (B-18) — sayı olmayan giriş de ALAN ALTINDA söylenir.</summary>
    [Fact]
    public void P06_SAYI_OLMAYAN_giris_alan_alti_mesaj_verir()
    {
        var sayac = 0;
        var e = Satir(_ => sayac++, deger: "10", asgari: 0m, azami: 100m);

        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42").Input("abc");
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        Assert.Equal(0, sayac);
        Assert.False(string.IsNullOrWhiteSpace(e.Find("#si-cari-aciklama-42-hata").TextContent));
    }

    /// <summary>
    /// P06-9 (B-18 · ALAN_ANAHTARSIZ_DOGRULAMA=0) — hata mesajı ALAN ANAHTARLI
    /// bir id taşır. "İşaretli alanları düzeltin" tek başına yasaktır: mesajın
    /// hangi alana ait olduğu DOM'dan okunabilmelidir.
    /// </summary>
    [Fact]
    public void P06_DOGRULAMA_mesaji_ALAN_ANAHTARLI_id_tasir()
    {
        var e = Satir(deger: "");
        e.Find("#si-cari-aciklama-42-ac").Click();
        e.Find("#si-cari-aciklama-42-kaydet").MouseDown();
        e.Find("#si-cari-aciklama-42-kaydet").Click();

        var mesaj = e.Find("#si-cari-aciklama-42-hata");
        // id, varlık + ALAN + kayıt kimliğini taşır → mesaj sahipsiz değildir.
        Assert.Equal("si-cari-aciklama-42-hata", mesaj.Id);
        // Girdi de mesaja aria-describedby ile BAĞLIDIR (ekran okuyucu kanıtı).
        Assert.Equal("si-cari-aciklama-42-hata",
            e.Find("#si-cari-aciklama-42").GetAttribute("aria-describedby"));
    }

    // ═══ 3. DuzenlenebilirListe — şablonun kendisi ══════════════════════════

    private static RenderFragment<string> Hucre => k => y => y.AddContent(0, k);

    /// <summary>
    /// P06-10 (B-4) — "ne düzenleme yolu ne gerekçe" hâli ÇİZİLEMEZ.
    /// Şablonun tek işi budur; bu test kırılırsa mekanizma yok demektir.
    /// </summary>
    [Fact]
    public void P06_DuzenlenebilirListe_EYLEMSIZ_ve_GEREKCESIZ_cizilemez()
    {
        var hata = Assert.Throws<InvalidOperationException>(() =>
            Render<DuzenlenebilirListe<string>>(p => p
                .Add(x => x.Varlik, "deneme")
                .Add(x => x.Kayitlar, new[] { "a" })
                .Add(x => x.Satir, Hucre)));

        Assert.Contains("UX-S25", hata.Message, StringComparison.Ordinal);
    }

    /// <summary>P06-11 (B-4) — SatirEylemi verilince liste çizilir ve eylem her satırda vardır.</summary>
    [Fact]
    public void P06_DuzenlenebilirListe_SATIR_EYLEMI_ile_cizilir()
    {
        RenderFragment<string> eylem = k => y =>
        {
            y.OpenElement(0, "button");
            y.AddAttribute(1, "id", $"dl-deneme-duzenle-{k}");
            y.AddContent(2, "Düzenle");
            y.CloseElement();
        };

        var e = Render<DuzenlenebilirListe<string>>(p => p
            .Add(x => x.Varlik, "deneme")
            .Add(x => x.Kayitlar, new[] { "a", "b" })
            .Add(x => x.Satir, Hucre)
            .Add(x => x.SatirEylemi, eylem));

        Assert.Equal(2, e.FindAll(".dl-satir").Count);
        Assert.NotNull(e.Find("#dl-deneme-duzenle-a"));
        Assert.NotNull(e.Find("#dl-deneme-duzenle-b"));
    }

    /// <summary>P06-12 (B-54) — gerekçe verilince DegismezIpucu çizilir, DÜĞME çizilmez.</summary>
    [Fact]
    public void P06_DuzenlenebilirListe_GEREKCE_ile_DegismezIpucu_cizer()
    {
        var e = Render<DuzenlenebilirListe<string>>(p => p
            .Add(x => x.Varlik, "talep")
            .Add(x => x.Kayitlar, new[] { "a" })
            .Add(x => x.Satir, Hucre)
            .Add(x => x.Gerekce, "Bu kayıt kanıt kaydıdır.")
            .Add(x => x.AlternatifYol, "Yeni talep açın."));

        Assert.NotNull(e.Find("#degismez-ipucu-talep"));
        // "Bir yolu var ama sana kapalı" demeyen mesaj: devre dışı düğme YOK.
        Assert.Empty(e.FindAll("button"));
    }

    /// <summary>
    /// P06-13 (B-68 · UX-S28) — HATA boş duruma ÇEVRİLMEZ.
    /// Kayıt listesi boş VE hata varsa ekranda hata bandı çıkar, "kayıt yok" ÇIKMAZ.
    /// </summary>
    [Fact]
    public void P06_DuzenlenebilirListe_HATAYI_bos_duruma_CEVIRMEZ()
    {
        var e = Render<DuzenlenebilirListe<string>>(p => p
            .Add(x => x.Varlik, "deneme")
            .Add(x => x.Kayitlar, Array.Empty<string>())
            .Add(x => x.Satir, Hucre)
            .Add(x => x.Gerekce, "gerekçe")
            .Add(x => x.Hata, "Sunucuya ulaşılamadı."));

        var bant = e.Find("#dl-deneme-hata");
        Assert.Equal("alert", bant.GetAttribute("role"));
        Assert.Empty(e.FindAll("#dl-deneme-bos"));   // boş durum BASTIRILDI
    }

    /// <summary>P06-14 (B-68) — hata YOKKEN gerçekten boş liste boş durumu gösterir.</summary>
    [Fact]
    public void P06_DuzenlenebilirListe_GERCEKTEN_bos_listede_bos_durum_cizer()
    {
        var e = Render<DuzenlenebilirListe<string>>(p => p
            .Add(x => x.Varlik, "deneme")
            .Add(x => x.Kayitlar, Array.Empty<string>())
            .Add(x => x.Satir, Hucre)
            .Add(x => x.Gerekce, "gerekçe"));

        Assert.NotNull(e.Find("#dl-deneme-bos"));
        Assert.Empty(e.FindAll("#dl-deneme-hata"));
    }

    /// <summary>P06-15 (UX-S29) — sınıra değen liste "daha fazlası var"ı YAZAR.</summary>
    [Fact]
    public void P06_DuzenlenebilirListe_SINIRA_degince_uyari_yazar()
    {
        var e = Render<DuzenlenebilirListe<string>>(p => p
            .Add(x => x.Varlik, "deneme")
            .Add(x => x.Kayitlar, new[] { "a", "b" })
            .Add(x => x.Satir, Hucre)
            .Add(x => x.Gerekce, "gerekçe")
            .Add(x => x.Sinir, 2));

        var uyari = e.Find("#dl-deneme-sinir").TextContent;
        Assert.Contains("2", uyari, StringComparison.Ordinal);
    }

    // ═══ 4. B-54 — istisna kütüğü DÜRÜSTTÜR ════════════════════════════════

    /// <summary>Yazma yapan sayfa = POST/PUT/PATCH çağıran .razor.</summary>
    private static IEnumerable<(string Ad, string Kaynak)> YazanSayfalar()
    {
        var dizin = CssKaynak.Yol("Kuyumcu.Web", "Pages");
        foreach (var yol in Directory.GetFiles(dizin, "*.razor"))   // .mutorj artıkları HARİÇ
        {
            var kaynak = File.ReadAllText(yol);
            if (Regex.IsMatch(kaynak, @"\b(PostAsync|PutAsync|PatchAsync|DeleteAsync)\b"))
                yield return (Path.GetFileName(yol), kaynak);
        }
    }

    private static bool MuhurluKalipVar(string kaynak) =>
        kaynak.Contains("<SatirIciDuzenle", StringComparison.Ordinal)
        || kaynak.Contains("<Cekmece", StringComparison.Ordinal)
        || kaynak.Contains("<DuzenlenebilirListe", StringComparison.Ordinal)
        || kaynak.Contains("<DegismezIpucu", StringComparison.Ordinal);

    private static readonly Regex DuzenlemeYoluKalibi =
        new(@"data-duzenleme-yolu\s*=\s*""([A-Za-z0-9]+\.razor)""");

    /// <summary>
    /// PAKET P-06 — BEYAN EDİLMİŞ DÜZENLEME YOLU. Liste ekranı düzenlemeyi kendi
    /// üstünde yapmayıp DETAY ekranına yolluyorsa bu geçerli bir yoldur (UX-S25)
    /// — ama YALNIZ beyan makineyle doğrulanabiliyorsa: hedef dosya GERÇEKTEN
    /// var olmalı ve kendisi mühürlü bir kalıp taşımalıdır. Aksi hâlde beyan,
    /// borç saklamanın yeni bir yolu olurdu (Anayasa md.1).
    /// </summary>
    private static bool BeyanEdilmisDuzenlemeYoluVar(string kaynak)
    {
        var eslesmeler = DuzenlemeYoluKalibi.Matches(kaynak);
        if (eslesmeler.Count == 0) return false;

        foreach (Match m in eslesmeler)
        {
            var hedef = CssKaynak.Yol("Kuyumcu.Web", "Pages", m.Groups[1].Value);
            Assert.True(File.Exists(hedef),
                $"data-duzenleme-yolu var olmayan bir sayfayı gösteriyor: {m.Groups[1].Value}");
            Assert.True(MuhurluKalipVar(File.ReadAllText(hedef)),
                $"{m.Groups[1].Value}: düzenleme yolu olarak beyan edildi ama kendisinde mühürlü kalıp YOK.");
        }
        return true;
    }

    /// <summary>
    /// P06-16 (B-4 · B-54 · DUZENLEME_KALIBI_YOK_SAYFA = 0) — yazma yapan HER
    /// sayfa ya mühürlü kalıp taşır ya da KÜTÜKLÜ bir istisnadır. Üçüncü hâl yok.
    /// </summary>
    [Fact]
    public void P06_YAZAN_her_sayfa_KALIP_ya_da_KUTUKLU_istisnadir()
    {
        var kutuk = DuzeltmePolitikasi.EkranIstisnalari
            .Select(i => i.Sayfa).ToHashSet(StringComparer.Ordinal);

        var suclular = YazanSayfalar()
            .Where(s => !MuhurluKalipVar(s.Kaynak)
                        && !BeyanEdilmisDuzenlemeYoluVar(s.Kaynak)
                        && !kutuk.Contains(s.Ad))
            .Select(s => s.Ad)
            .ToList();

        Assert.True(suclular.Count == 0,
            "Yazma yapan ama ne düzenleme kalıbı ne kütüklü gerekçesi olan sayfa(lar) "
            + "(UX-S25 · B-4):\n  " + string.Join("\n  ", suclular));
    }

    /// <summary>
    /// P06-17 (B-54 · md.1 "gizleyerek sayaç düşürmek yasak") — kütükteki her
    /// istisna GERÇEKTEN kalıpsızdır. Kalıbı olan bir sayfa kütükte durursa
    /// kütük sayacı yapay olarak şişiyor demektir; ya da tersine, kütük
    /// "her ihtimale karşı" doldurulmuş olur. İkisi de kabul edilmez.
    /// </summary>
    [Fact]
    public void P06_KUTUKTEKI_her_istisna_GERCEKTEN_kalipsizdir()
    {
        foreach (var istisna in DuzeltmePolitikasi.EkranIstisnalari)
        {
            var kaynak = CssKaynak.Metin("Kuyumcu.Web", "Pages", istisna.Sayfa);
            Assert.False(MuhurluKalipVar(kaynak),
                $"{istisna.Sayfa}: mühürlü kalıbı VAR ama istisna kütüğünde duruyor — kütükten çıkarılmalı.");
        }
    }

    /// <summary>
    /// P06-18 (B-54 · UX-S25) — her istisna gerekçeyi VE kullanıcının düzeltmeyi
    /// nerede yaptığını SOMUT yazar. Boş/şablon metin geçmez.
    /// </summary>
    [Fact]
    public void P06_HER_istisna_GEREKCE_ve_SOMUT_duzeltme_yeri_yazar()
    {
        Assert.NotEmpty(DuzeltmePolitikasi.EkranIstisnalari);
        foreach (var i in DuzeltmePolitikasi.EkranIstisnalari)
        {
            Assert.EndsWith(".razor", i.Sayfa, StringComparison.Ordinal);
            Assert.True(i.Gerekce.Trim().Length >= 40, $"{i.Sayfa}: gerekçe fazla kısa.");
            Assert.True(i.DuzeltmeYeri.Trim().Length >= 25, $"{i.Sayfa}: düzeltme yeri somut değil.");
        }
    }

    /// <summary>
    /// P06-19 (B-54 cırcırı) — istisna sayısı TAVANI aşamaz. Tavan yalnız AZALIR;
    /// yeni bir sayfayı kütüğe yazarak kalıp borcunu saklamak bu testi kırar.
    /// TAVAN 2026-09-18'de TAZE ÖLÇÜLDÜ (PLAN § 1 md.2 — belgeden kopyalanmadı).
    /// </summary>
    [Fact]
    public void P06_ISTISNA_sayisi_TAVANI_asamaz()
    {
        const int tavan = 3;
        Assert.True(DuzeltmePolitikasi.EkranIstisnalari.Length <= tavan,
            $"İstisna kütüğü {DuzeltmePolitikasi.EkranIstisnalari.Length} satır — tavan {tavan}. "
            + "Tavan ARTIRILMAZ: yeni sayfaya mühürlü kalıp kurulur.");
    }

    /// <summary>
    /// P06-20 (Y-15 · UX-S31 · B-59) — ham politika kodu (SALT_OKUNUR / STORNO /
    /// DUZENLENEBILIR) sayfa METNİNE basılmaz; gerekçe sözlükten çözülür.
    /// </summary>
    [Fact]
    public void P06_HAM_politika_kodu_EKRAN_METNINE_basilmaz()
    {
        // Açık metin olarak basma kalıbı: >SALT_OKUNUR< ya da @("STORNO") gibi.
        var kalip = new Regex(@">\s*(SALT_OKUNUR|STORNO|DUZENLENEBILIR)\s*<");
        var suclular = new List<string>();

        foreach (var yol in Directory.GetFiles(CssKaynak.Yol("Kuyumcu.Web", "Pages"), "*.razor"))
        {
            var kaynak = File.ReadAllText(yol);
            foreach (Match m in kalip.Matches(kaynak))
                suclular.Add($"{Path.GetFileName(yol)}: {m.Value.Trim()}");
        }

        Assert.True(suclular.Count == 0,
            "Ham politika kodu ekrana basılıyor (UX-S31):\n  " + string.Join("\n  ", suclular));
    }

    /// <summary>
    /// P06-21 (Y-15) — politika haritasındaki HER gerekçe anahtarının TR ve EN
    /// karşılığı vardır; eksik anahtar boş metin döndürüp kullanıcıyı sessiz
    /// bırakırdı (Anayasa md.1).
    /// </summary>
    [Fact]
    public void P06_HER_gerekce_anahtarinin_TR_ve_EN_karsiligi_VARDIR()
    {
        foreach (var anahtar in GerekceAnahtarlari.Tumu)
        {
            Assert.False(string.IsNullOrWhiteSpace(Sozlukler.Tr.Duzeltme.GerekceMetni(anahtar)),
                $"TR gerekçe metni yok: {anahtar}");
            Assert.False(string.IsNullOrWhiteSpace(Sozlukler.En.Duzeltme.GerekceMetni(anahtar)),
                $"EN gerekçe metni yok: {anahtar}");
        }
    }

    /// <summary>
    /// P06-22 (BU_PAKETTE_EKLENEN_RECORD = 0) — bu paket Pages/ altına YENİ
    /// istemci record AÇMADI. Şablonun tipi jeneriktir; sözleşme tipi gerekirse
    /// Kuyumcu.Contracts tarafına konur (PLAN § 1 md.9).
    /// </summary>
    [Fact]
    public void P06_Bilesenler_altinda_YENI_istemci_record_ACILMADI()
    {
        var liste = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "DuzenlenebilirListe.razor");
        Assert.DoesNotContain("record ", liste, StringComparison.Ordinal);

        var satirIci = CssKaynak.Metin("Kuyumcu.Web", "Bilesenler", "SatirIciDuzenle.razor");
        Assert.DoesNotContain("record ", satirIci, StringComparison.Ordinal);
    }
}
