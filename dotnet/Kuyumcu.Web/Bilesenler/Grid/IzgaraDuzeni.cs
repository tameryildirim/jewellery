using Kuyumcu.Core.Domain;

namespace Kuyumcu.Web.Bilesenler.Grid;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GRID-1 — IZGARA DÜZENİ (saf mantık; bileşenden ayrı ki birim sınansın)
//
// Girdi: ekranın kolon TANIMI (varsayılan sıra) + kullanıcının kayıtlı tercihi.
// Çıktı: kolon sırası, gizli kolonlar, sıralama.
//
// DAYANIKLILIK (envanter § G1 "Sürüm ve dayanıklılık"):
//   • tercih yok / bozuk / sürümü uyuşmuyor → VARSAYILAN düzen (hata yok, tercih SİLİNMEZ);
//   • tercihte olmayan YENİ kolon varsayılan yerine (kendinden önceki kolonun
//     arkasına) ve varsayılan görünürlüğüyle eklenir;
//   • artık var olmayan kolon anahtarı YOK SAYILIR;
//   • zorunlu kolon tercihte gizli olsa bile GÖRÜNÜR (kural tercihten üstündür);
//   • taşınamaz kolon (eylemler) hep en sonda.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bir kolonun düzen açısından tanımı (bileşenden bağımsız).</summary>
public sealed record IzgaraKolonTanimi(string Anahtar, bool Zorunlu, bool VarsayilanGorunur, bool Tasinamaz, bool Siralanabilir);

public sealed class IzgaraDuzeni
{
    private readonly IReadOnlyList<IzgaraKolonTanimi> tanim;

    /// <summary>Tüm kolonlar, çizim sırasıyla (taşınamazlar sonda).</summary>
    public IReadOnlyList<string> Sira { get; }

    /// <summary>Gizli kolonlar (zorunlular asla burada değildir).</summary>
    public IReadOnlySet<string> Gizli { get; }

    /// <summary>Etkin sıralama (kolon anahtarı + yön) ya da null.</summary>
    public IzgaraSiralamaTercihi? Siralama { get; }

    private IzgaraDuzeni(IReadOnlyList<IzgaraKolonTanimi> tanim, IReadOnlyList<string> sira,
        IReadOnlySet<string> gizli, IzgaraSiralamaTercihi? siralama)
    {
        this.tanim = tanim;
        Sira = sira;
        Gizli = gizli;
        Siralama = siralama;
    }

    /// <summary>Görünen kolonlar (çizim sırasıyla).</summary>
    public IReadOnlyList<string> Gorunen => [.. Sira.Where(a => !Gizli.Contains(a))];

    /// <summary>Varsayılan düzen (tercih yokken).</summary>
    public static IzgaraDuzeni Varsayilan(IReadOnlyList<IzgaraKolonTanimi> tanim) => Kur(tanim, null);

    /// <summary>
    /// Tercihi tanıma uygular. <paramref name="tercih"/> null ya da sürümü
    /// <paramref name="surum"/>'dan farklıysa varsayılan düzen döner.
    /// </summary>
    public static IzgaraDuzeni Kur(IReadOnlyList<IzgaraKolonTanimi> tanim, IzgaraTercihi? tercih, int surum = 1)
    {
        if (tercih is not null && tercih.S != surum) tercih = null;   // eski/yeni sürüm → varsayılan

        var tasinir = tanim.Where(t => !t.Tasinamaz).Select(t => t.Anahtar).ToList();
        var bilinen = new HashSet<string>(tasinir, StringComparer.Ordinal);

        List<string> sira;
        if (tercih is null || tercih.K.Count == 0)
        {
            sira = [.. tasinir];
        }
        else
        {
            sira = [.. tercih.K.Where(bilinen.Contains).Distinct()];
            // Tercihte olmayan (yeni) kolon: varsayılan sırada kendinden ÖNCE gelen
            // kolonun arkasına; öncülü yoksa başa.
            for (var i = 0; i < tasinir.Count; i++)
            {
                var a = tasinir[i];
                if (sira.Contains(a)) continue;
                var konum = 0;
                for (var j = i - 1; j >= 0; j--)
                {
                    var onceki = sira.IndexOf(tasinir[j]);
                    if (onceki >= 0) { konum = onceki + 1; break; }
                }
                sira.Insert(konum, a);
            }
        }
        sira.AddRange(tanim.Where(t => t.Tasinamaz).Select(t => t.Anahtar));

        HashSet<string> gizli;
        if (tercih is null)
        {
            gizli = tanim.Where(t => !t.VarsayilanGorunur && !t.Zorunlu).Select(t => t.Anahtar).ToHashSet(StringComparer.Ordinal);
        }
        else
        {
            var tercihteVar = new HashSet<string>(tercih.K.Concat(tercih.G), StringComparer.Ordinal);
            gizli = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in tanim)
            {
                if (t.Zorunlu) continue;
                if (tercih.G.Contains(t.Anahtar)) gizli.Add(t.Anahtar);
                else if (!tercihteVar.Contains(t.Anahtar) && !t.VarsayilanGorunur) gizli.Add(t.Anahtar);
            }
        }

        IzgaraSiralamaTercihi? siralama = null;
        if (tercih?.O is { } o && tanim.Any(t => t.Anahtar == o.A && t.Siralanabilir))
            siralama = o;

        return new IzgaraDuzeni(tanim, sira, gizli, siralama);
    }

    /// <summary>Bu düzenin tercih karşılığı (taşınamazlar sıraya yazılmaz).</summary>
    public IzgaraTercihi Tercih(int surum = 1)
    {
        var tasinamaz = tanim.Where(t => t.Tasinamaz).Select(t => t.Anahtar).ToHashSet(StringComparer.Ordinal);
        return new IzgaraTercihi(surum,
            [.. Sira.Where(a => !tasinamaz.Contains(a))],
            [.. Sira.Where(Gizli.Contains)],
            Siralama);
    }

    private IzgaraKolonTanimi? Bul(string a) => tanim.FirstOrDefault(t => t.Anahtar == a);

    /// <summary>Kolon taşınabilir mi (taşınamaz değil ve tanımda var).</summary>
    public bool Tasinabilir(string a) => Bul(a) is { Tasinamaz: false };

    /// <summary>
    /// Bir adım taşınabilecek hedef konum; yoksa -1. <paramref name="yalnizGorunen"/>
    /// = başlıkta klavyeyle taşıma (gizli kolonların üzerinden atlar).
    /// </summary>
    public int Hedef(string a, int yon, bool yalnizGorunen)
    {
        if (!Tasinabilir(a)) return -1;
        var i = Sira.ToList().IndexOf(a);
        if (i < 0) return -1;
        for (var j = i + yon; j >= 0 && j < Sira.Count; j += yon)
        {
            var b = Sira[j];
            if (!Tasinabilir(b)) return -1;            // taşınamaz kolonun ötesine geçilmez
            if (yalnizGorunen && Gizli.Contains(b)) continue;
            return j;
        }
        return -1;
    }

    /// <summary>Kolonu bir adım sola (-1) ya da sağa (+1) taşır; sınırdaysa null.</summary>
    public IzgaraDuzeni? Tasi(string a, int yon, bool yalnizGorunen = false)
    {
        var j = Hedef(a, yon, yalnizGorunen);
        if (j < 0) return null;
        var liste = Sira.ToList();
        liste.Remove(a);
        liste.Insert(j, a);
        return new IzgaraDuzeni(tanim, liste, Gizli, Siralama);
    }

    /// <summary>Sürükle-bırak: kaynağı hedefin yerine koyar. Geçersizse null.</summary>
    public IzgaraDuzeni? Birak(string kaynak, string hedef)
    {
        if (kaynak == hedef || !Tasinabilir(kaynak) || !Tasinabilir(hedef)) return null;
        var liste = Sira.ToList();
        var j = liste.IndexOf(hedef);
        liste.Remove(kaynak);
        liste.Insert(j, kaynak);
        return new IzgaraDuzeni(tanim, liste, Gizli, Siralama);
    }

    /// <summary>Göster/gizle. Zorunlu kolon gizlenemez → null.</summary>
    public IzgaraDuzeni? GizleDegistir(string a)
    {
        if (Bul(a) is not { } k || k.Zorunlu) return null;
        var g = new HashSet<string>(Gizli, StringComparer.Ordinal);
        if (!g.Remove(a)) g.Add(a);
        // Gizlenen kolonla sıralanıyorsa sıralama kalkar (görünmeyen ölçütle sıralı liste yanıltır).
        var s = Siralama is not null && g.Contains(Siralama.A) ? null : Siralama;
        return new IzgaraDuzeni(tanim, Sira, g, s);
    }

    /// <summary>Başlık tıklaması: artan → azalan → varsayılan (null).</summary>
    public IzgaraDuzeni? SiralamaDongusu(string a)
    {
        if (Bul(a) is not { Siralanabilir: true }) return null;
        IzgaraSiralamaTercihi? yeni = Siralama?.A != a
            ? new(a, IzgaraTercihKurali.YonArtan)
            : Siralama.Y == IzgaraTercihKurali.YonArtan ? new(a, IzgaraTercihKurali.YonAzalan) : null;
        return new IzgaraDuzeni(tanim, Sira, Gizli, yeni);
    }

    /// <summary>Düzen varsayılandan farklı mı (yalnız bilgi — "Varsayılana dön" kayıtlı tercihe bakar).</summary>
    public bool VarsayilanMi
    {
        get
        {
            var v = Varsayilan(tanim);
            return v.Sira.SequenceEqual(Sira) && v.Gizli.SetEquals(Gizli) && Siralama is null;
        }
    }
}
