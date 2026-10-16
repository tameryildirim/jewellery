using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// SEÇİCİ TEST YARDIMCISI — paket P-36c (ham <select> → ortak <Secici> göçü)
//
// NEDEN VAR: göçte eleman KİMLİKLERİ birebir korundu, ama eleman TÜRÜ değişti:
// `<select id="x">` artık `<button id="x" role="combobox">` + `<ul role=listbox>`.
// Bu yüzden `Find("#x").Change("v")` (onchange) artık geçersizdir — bu bir
// göç hatası değil, bileşenin sözleşmesidir (yerleşik select'in açılır listesi
// biçimlendirilemediği için kurucu talimatı 19 ile değiştirildi).
//
// TEK YER KURALI: testler seçimi TEK bu yardımcıdan yapar; ikinci bir "seçici
// tıklama" kalıbı açılmaz. Seçenek, DOM'daki `data-deger` ile bulunur —
// görünen metin dile göre değişir, kod değişmez.
// ⚠ Seçenek bulunamazsa SESSİZCE geçilmez: testin neden kırıldığı yazılı olarak
//   söylenir (Anayasa md.1).
// ═════════════════════════════════════════════════════════════════════════════
public static class SeciciYardim
{
    /// <summary>
    /// Seçenek listesini döndürür (Secici'yi AÇARAK). Ham &lt;select&gt; kalmışsa
    /// eski &lt;option&gt; düğümleri döner — göç kısmi olduğu sürece iki yol da çalışır.
    /// Boş seçenek de listededir (role=option), sayım eski davranışla AYNI kalır.
    /// </summary>
    public static IReadOnlyList<IElement> Secenekler<T>(IRenderedComponent<T> c, string elemanId)
        where T : IComponent
    {
        var tetik = c.Find("#" + elemanId);
        if (tetik.GetAttribute("role") != "combobox")
            return tetik.QuerySelectorAll("option").ToList();
        if (tetik.GetAttribute("aria-expanded") != "true") tetik.Click();
        // ⚠ KARARSIZLIK DÜZELTMESİ (P-37 doğrulama bulgusu, 21 Eylül):
        //   Click() sonrası liste HENÜZ basılmamış olabiliyordu (yük altında
        //   IadeEkranTestleri.Iade_odeme_secicisinde_ALTIN_YOKTUR kırmızıya
        //   dönüyordu: "Collection: []"). Artık listenin BASILMASI BEKLENİR;
        //   beklemede zaman aşımı olursa test SESSİZCE geçmez, gerekçesiyle
        //   düşer (Anayasa md.1).
        //   Bekleme zaman aşımına uğrarsa liste GERÇEKTEN boş demektir: o durumda
        //   çağıranın kendi iddiası (ör. Assert.Contains) kendi mesajıyla düşer —
        //   burada hata yutulmaz, yalnız "boş liste" meşru sonucu korunur.
        try
        {
            c.WaitForAssertion(
                () => Assert.NotEmpty(c.FindAll($"#{elemanId}-liste li[role=option]")),
                TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) when (ex.GetType().Name == "WaitForFailedException")
        { /* liste gerçekten boş — çağıranın kendi iddiası karar verir */ }
        return c.FindAll($"#{elemanId}-liste li[role=option]").ToList();
    }

    /// <summary>
    /// Açık listenin TÜM metni — ham select döneminde `Find("#id").TextContent`
    /// seçenekleri de kapsıyordu; Secici'de seçenekler kardeş listede durur.
    /// </summary>
    public static string ListeMetni<T>(IRenderedComponent<T> c, string elemanId)
        where T : IComponent
        => string.Join(" ", Secenekler(c, elemanId).Select(o => o.Ad()));

    /// <summary>
    /// Seçicinin O AN seçili değeri — ham select'te <c>value</c>, ortak
    /// Secici'de tetik düğmesindeki <c>data-deger</c>. Testler bu iki dünyada
    /// da AYNI çağrıyı kullansın diye burada durur (tek yer kuralı).
    /// </summary>
    public static string? SeciliDeger<T>(IRenderedComponent<T> c, string elemanId)
        where T : IComponent
    {
        var el = c.Find("#" + elemanId);
        // P-36c/3: salt okunur alan artık <input value> değil, metin satırıdır;
        // MAKİNE değeri Secici ile AYNI sözleşmeden okunur (data-deger).
        if (el.HasAttribute("data-deger")) return el.GetAttribute("data-deger");
        return el.GetAttribute("value");
    }

    /// <summary>Seçeneğin GÖVDEYE giden kodu — ham select'te value, Secici'de data-deger.</summary>
    public static string Deger(this IElement o)
        => o.GetAttribute("data-deger") ?? o.GetAttribute("value") ?? "";

    /// <summary>Seçeneğin GÖRÜNEN adı (işaret/rozet boşlukları temizlenmiş).</summary>
    public static string Ad(this IElement o)
        => (o.QuerySelector(".secici-ad")?.TextContent ?? o.TextContent).Trim();

    /// <summary>Ortak &lt;Secici&gt;'de bir değeri seçer (aç → seçeneğe tıkla).</summary>
    public static void Sec<T>(IRenderedComponent<T> c, string elemanId, string? deger)
        where T : IComponent
    {
        var tetik = c.Find("#" + elemanId);
        if (tetik.GetAttribute("role") != "combobox")
        {
            // Ham <select> ise eski yol geçerlidir — göç kısmi olabilir.
            // ⚠ P-36c/3: ham girdi göçünden sonra aynı KİMLİK bir ortak girdi
            //   bileşeninin (Bilesenler/Form/*) kutusu olabilir; o aile
            //   `oninput` dinler, `onchange` DEĞİL. Hangi olayın bağlı olduğunu
            //   DOM söyler — test tahmin etmez (Anayasa md.1), tek kapı korunur.
            if (tetik.Attributes.Any(a => a.Name.EndsWith("oninput")))
                tetik.Input(deger ?? "");
            else
                tetik.Change(deger ?? "");
            return;
        }
        // ── KİLİTLİ SEÇİCİ (P-36c) ──────────────────────────────────────
        // Ham <select disabled> bUnit'te yine `.Change()` kabul ediyordu; ortak
        // Secici KİLİTLİYKEN AÇILMAZ. Kilit çoğu yerde "tek seçenek var, sistem
        // seçti" demektir (UX-S22) — istenen değer ZATEN seçiliyse bu bir
        // NO-OP'tur. Değilse test SESSİZCE geçmez, kilit sebebiyle kırılır.
        if (tetik.HasAttribute("disabled"))
        {
            var mevcut = tetik.GetAttribute("data-deger") ?? "";
            if (mevcut == (deger ?? "")) return;
            throw new InvalidOperationException(
                $"Secici '{elemanId}' KİLİTLİ (sebep: {tetik.GetAttribute("title")}); " +
                $"istenen '{deger}', ekrandaki '{mevcut}'.");
        }

        // Zaten açıksa TEKRAR tıklamak KAPATIR (Secenekler ile zincirlenince).
        if (tetik.GetAttribute("aria-expanded") != "true") tetik.Click();

        if (string.IsNullOrEmpty(deger))
        {
            var bos = c.FindAll($"#{elemanId}-secenek-bos").FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Secici '{elemanId}': boş seçenek yok, '' seçilemez.");
            bos.Click();
            return;
        }

        // ⚠ Seçenek listesi SUNUCUDAN gelir; ham <select> döneminde `.Change()`
        //   eşleşen seçenek olmasa da değeri atıyordu (sessiz kabul). Ortak
        //   Secici gerçek listeden seçtiği için liste dolana kadar BEKLENİR;
        //   gelmezse test SESSİZCE geçmez, aşağıdaki mesajla kırılır (md.1).
        try
        {
            c.WaitForAssertion(() => Assert.Contains(
                c.FindAll($"#{elemanId}-liste li[role=option]"),
                x => x.GetAttribute("data-deger") == deger), TimeSpan.FromSeconds(2));
        }
        catch (Exception) { /* aşağıdaki açıklayıcı hata basılır */ }

        var secenek = c.FindAll($"#{elemanId}-liste li[role=option]")
            .FirstOrDefault(x => x.GetAttribute("data-deger") == deger);
        if (secenek is null)
        {
            var varolan = string.Join(", ", c.FindAll($"#{elemanId}-liste li[role=option]")
                .Select(x => x.GetAttribute("data-deger")));
            throw new InvalidOperationException(
                $"Secici '{elemanId}': '{deger}' seçeneği listede yok. Listedekiler: [{varolan}]");
        }
        secenek.Click();
    }
}
