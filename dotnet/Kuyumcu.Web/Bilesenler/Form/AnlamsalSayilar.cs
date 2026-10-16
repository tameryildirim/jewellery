using Kuyumcu.Core.Domain.Girdi;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Bilesenler.Form;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET GIRDI-AILESI-2 — ANLAMSAL SAYI BİLEŞENLERİ (envanter F2).
// Hepsi <Sayi>'nın görünümünü ve SayisalAlan beynini (tr-TR çözücü, kapı, biçim)
// AYNEN kullanır; yalnız Core'daki kendi kuralını ekler. Harf ve (kural izin
// vermiyorsa) "-" tuşta engellenir (girdi-maske.js 'sayi'), kesir tam sayı
// alanında yazılamaz.
//   <Adet>      K14 tam sayı ≥ 1               <Milyem>   K12 1–1000, 3 ondalık
//   <Yuzde>     K15 0–100 (Ondalik, Marj)      <KurAlani> K16 > 0, 6 ondalık
//   <GunSayisi> K17 tam sayı, EnAz–EnCok       <Has>      K11 > 0, 6 ondalık
// ⚠ Ad "KurAlani": Pages/Kur.razor sayfa sınıfıyla çakışmasın; "BarkodAlani" de
//   Kuyumcu.Core.Domain.Barkod ile çakışmasın diye.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>K14 — adet: tam sayı ≥ 1.</summary>
public class Adet : Sayi
{
    public Adet() { TamSayi = true; }
    protected override bool EksiYazilabilir => false;
    /// <summary>PAKET GOC-C: kesir iletisi Core AdetKurali'ndan (sunucuyla aynı sözlük anahtarı).</summary>
    protected override bool TamSayiCoreKuralinda => true;
    protected override GirdiHatasi? KuralDenetle(decimal d) => AdetKurali.Denetle(d) ?? base.KuralDenetle(d);
}

/// <summary>K12 — milyem: 1–1000, en çok 3 ondalık (ayar sapma uyarısı Core'da: MilyemKurali.SapmaVar).</summary>
public class Milyem : Sayi
{
    protected override bool EksiYazilabilir => false;
    protected override GirdiHatasi? KuralDenetle(decimal d) => MilyemKurali.Denetle(d) ?? base.KuralDenetle(d);
}

/// <summary>K15 — yüzde: 0–100 (Marj=true: 0–500), en çok <see cref="OndalikHane"/> ondalık.</summary>
public class Yuzde : Sayi
{
    [Parameter] public int OndalikHane { get; set; } = YuzdeKurali.OndalikHane;
    /// <summary>Kâr/marj oranı: 100'ü aşabilir (en çok 500).</summary>
    [Parameter] public bool Marj { get; set; }
    protected override bool EksiYazilabilir => false;
    protected override GirdiHatasi? KuralDenetle(decimal d) =>
        (Marj ? YuzdeKurali.Marj(d, OndalikHane) : YuzdeKurali.Denetle(d, OndalikHane)) ?? base.KuralDenetle(d);
}

/// <summary>K16 — kur: &gt; 0, en çok 6 ondalık.</summary>
public class KurAlani : Sayi
{
    protected override bool EksiYazilabilir => false;
    protected override GirdiHatasi? KuralDenetle(decimal d) => KurKurali.Denetle(d) ?? base.KuralDenetle(d);
}

/// <summary>K17 — gün sayısı: tam sayı, [EnAz, EnCok] (verilmezse 0–3650).</summary>
public class GunSayisi : Sayi
{
    public GunSayisi() { TamSayi = true; }
    protected override bool EksiYazilabilir => false;
    protected override GirdiHatasi? KuralDenetle(decimal d) =>
        GunSayisiKurali.Denetle(d, (int)(EnAz ?? 0), (int)(EnCok ?? 3650));
}

/// <summary>K11 — has gram: &gt; 0, en çok 6 ondalık.</summary>
public class Has : Sayi
{
    protected override bool EksiYazilabilir => false;
    protected override GirdiHatasi? KuralDenetle(decimal d) => AgirlikKurali.Has(d) ?? base.KuralDenetle(d);
}
