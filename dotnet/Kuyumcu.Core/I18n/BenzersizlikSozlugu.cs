namespace Kuyumcu.Core.I18n;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET P-05 (Y-2) — BENZERSİZLİK İHLALİNİN TÜRKÇE, ALAN ALTI KARŞILIĞI
//
// MySQL benzersizlik ihlalinde (hata 1062) şu metni döndürür:
//   Duplicate entry 'ABC-01' for key 'urunler.UQ_urunler_tenant_kod'
// Bu metin kullanıcıya GÖSTERİLEMEZ: İngilizcedir, tablo/indeks adı sızdırır ve
// hangi FORM ALANININ hatalı olduğunu söylemez.
//
// Bu sözlük indeks adını → (alan anahtarı, Türkçe mesaj) çiftine çevirir.
// Alan anahtarı istemcinin `alanlar` sözlüğüne girer; ekran onu alanın ALTINDA
// gösterir (UX-4d madde 8 boru hattı, ApiIstemci.HataFirlat).
//
// ⚠ Sözlükte OLMAYAN bir indeks için genel bir mesaj döner ve indeks adı
//   KULLANICIYA YAZILMAZ (iç kod sızıntısı yasağı, Mehmet koşulu). Yeni bir
//   benzersiz indeks eklenince buraya satır eklenmesi denetim.sh sayacıyla
//   (DOGAL_ANAHTAR_KISITSIZ) zorunlu tutulur.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Benzersizlik ihlalinin kullanıcıya görünen karşılığı.</summary>
/// <param name="Alan">İstemcinin `alanlar` sözlüğündeki anahtar (camelCase).</param>
/// <param name="Mesaj">Alanın altında gösterilecek Türkçe cümle.</param>
public readonly record struct BenzersizlikIhlali(string Alan, string Mesaj);

/// <summary>Veritabanı benzersiz indeks adı → Türkçe alan altı mesaj.</summary>
public static class BenzersizlikSozlugu
{
    /// <summary>
    /// Sözlük. Anahtar: <c>UQ_…</c> indeks adı (şema öneki OLMADAN).
    /// TR karşılıklar <see cref="DogrulamaSozlugu.AlanAdlari"/> ile tutarlıdır.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, BenzersizlikIhlali> Kayitlar =
        new Dictionary<string, BenzersizlikIhlali>(StringComparer.OrdinalIgnoreCase)
        {
            ["UQ_urunler_tenant_kod"] =
                new("kod", "Bu stok kodu bu işletmede zaten kullanılıyor — başka bir kod girin."),
            ["UQ_urunler_tenant_barkod"] =
                new("barkod", "Bu barkod başka bir üründe kayıtlı — barkodu boş bırakabilir ya da değiştirebilirsiniz."),
            ["UQ_cariler_tenant_vergi_no"] =
                new("vergiNo", "Bu vergi/TC numarası başka bir AKTİF cari kartta kayıtlı — mevcut kartı düzenleyin."),
            ["UQ_stoklar_tenant_sube_urun"] =
                new("urunId", "Bu ürünün stok satırı aynı anda başka bir işlem tarafından açıldı — "
                    + "işlemi tekrarlayın; stok bozulmadı, hiçbir hareket yazılmadı."),
            // ⚠ Bu indeks P-05'ten ÖNCE vardı ve adı EF varsayılanıdır. YENİDEN
            //   ADLANDIRILMADI: ad değişimi tek başına bir migration ister ve
            //   kazanç yoktur (PLAN: "zaten var → yalnız doğrulanır").
            ["IX_cari_ibanlari_tenant_id_iban_hash"] =
                new("iban", "Bu IBAN bu işletmede zaten kayıtlı."),
        };

    /// <summary>Genel (sözlükte bulunmayan indeks) mesajı — iç kod SIZDIRMAZ.</summary>
    public const string GenelMesaj =
        "Bu kayıt zaten var — aynı bilgiyle ikinci bir kayıt açılamaz.";

    /// <summary>Genel ihlalde `alanlar` sözlüğünde kullanılan anahtar.</summary>
    public const string GenelAlan = "genel";

    /// <summary>
    /// Veritabanının verdiği indeks adını çözer. Ad <c>tablo.indeks</c> biçiminde
    /// gelebilir; yalnız son parça dikkate alınır. Bilinmeyen ad genel mesaja düşer.
    /// </summary>
    public static BenzersizlikIhlali Coz(string? indeksAdi)
    {
        if (string.IsNullOrWhiteSpace(indeksAdi)) return new(GenelAlan, GenelMesaj);
        var son = indeksAdi.Split('.')[^1].Trim().Trim('\'', '`', '"');
        return Kayitlar.TryGetValue(son, out var kayit) ? kayit : new(GenelAlan, GenelMesaj);
    }

    /// <summary>
    /// MySQL'in 1062 metninden indeks adını ayıklar:
    /// <c>Duplicate entry 'x' for key 'urunler.UQ_urunler_tenant_kod'</c>.
    /// Bulamazsa <c>null</c> döner (çağıran genel mesaja düşer).
    /// </summary>
    public static string? IndeksAdiniAyikla(string? hataMetni)
    {
        if (string.IsNullOrWhiteSpace(hataMetni)) return null;
        const string isaret = "for key ";
        var yer = hataMetni.IndexOf(isaret, StringComparison.OrdinalIgnoreCase);
        if (yer < 0) return null;
        var kalan = hataMetni[(yer + isaret.Length)..].Trim();
        if (kalan.Length == 0) return null;
        var tirnak = kalan[0];
        if (tirnak is '\'' or '`' or '"')
        {
            var kapanis = kalan.IndexOf(tirnak, 1);
            if (kapanis > 1) return kalan[1..kapanis];
            return null;
        }
        var bosluk = kalan.IndexOf(' ');
        return bosluk > 0 ? kalan[..bosluk] : kalan;
    }
}
