using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MHurda = Kuyumcu.Api.Modules.Hurda;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// PAKET UX-1-C3 — gider pusulası BELGESİNİN düzenleyen bloğu [DB, gerçek MySQL]
//
// UX-1-B ile tenants tablosuna adres / vergi dairesi / vergi no / telefon
// kolonları geldi. VUK md.234 belgenin DÜZENLEYEN tarafını bu bilgilerle ister;
// belge ise yalnız unvan + şube basıyordu.
//
// Anayasa md.1: boş alan UYDURULMAZ — kiracı kartı boşsa belge null döner ve
// ekran "işletme kartından doldurun" der (sahte adres yazılmaz).
// ═════════════════════════════════════════════════════════════════════════════

[Collection("veritabani")]
public sealed class UX1C3BelgeTestleri(TestVeritabani vt)
{
    private static DenetimServisi Denetim(KuyumcuDbContext db) => new(db, new HttpContextAccessor());

    private static MHurda.HurdaServisi Servis(KuyumcuDbContext db) =>
        new(db, Denetim(db), new MHurda.HurdaAyarlari());

    /// <summary>Belge ucunun okuyabileceği en yalın gider pusulası satırı.</summary>
    private static async Task<string> PusulaYaz(KuyumcuDbContext db, Kiraci k)
    {
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();
        db.GiderPusulalari.Add(new GiderPusulasi
        {
            Id = id,
            TenantId = k.Islem.TenantId,
            PusulaNo = "GP-2026-000001",
            HareketId = Kimlik.Ulid(),
            SubeId = k.Islem.SubeId!,
            SaticiTipi = SaticiTipi.NihaiTuketici,
            SaticiAdSoyad = "Ayşe Yılmaz",
            SaticiTckn = "v1:sifreli",
            SaticiTcknHash = new string('a', 64),
            SifreSurumu = 1,
            AyarId = "K22",
            GramAgirlik = 54.6m,
            HasGrami = 50m,
            BrutTutar = 74196m,
            StopajOran = 0m,
            StopajTutar = 0m,
            NetOdenen = 74196m,
            TeslimTarihi = now,
            DuzenlemeSonTarihi = now.AddDays(7),
            Olusturuldu = now,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static object? Alan(object kaynak, string ad) =>
        kaynak.GetType().GetProperty(ad)?.GetValue(kaynak);

    [Fact]
    public async Task Belgede_isletme_ADRESI_ve_VERGI_bilgisi_basilir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "C3 belge dolu");

        var sistem = vt.OlusturSistem();
        var tenant = await sistem.Tenants.SingleAsync(t => t.Id == k.Islem.TenantId);
        tenant.Adres = "Kuyumcular Çarşısı No:5 Fatih/İstanbul";
        tenant.VergiDairesi = "Fatih";
        tenant.VergiNo = "1234567890";
        tenant.Telefon = "02125550000";
        await sistem.SaveChangesAsync();

        var db = vt.Olustur(k);
        var id = await PusulaYaz(db, k);

        var belge = await Servis(vt.Olustur(k)).Belge(id, k.Islem);
        var duzenleyen = Alan(belge, "duzenleyen")!;

        Assert.Equal("Kuyumcular Çarşısı No:5 Fatih/İstanbul", Alan(duzenleyen, "adres"));
        Assert.Equal("Fatih", Alan(duzenleyen, "vergiDairesi"));
        Assert.Equal("1234567890", Alan(duzenleyen, "vergiNo"));
        Assert.Equal("02125550000", Alan(duzenleyen, "telefon"));
        // Eski alanlar AYNEN duruyor (sözleşme geriye dönük kırılmadı).
        Assert.Equal("C3 belge dolu", Alan(duzenleyen, "unvan"));
        Assert.Equal("Merkez", Alan(duzenleyen, "sube"));
    }

    [Fact]
    public async Task Isletme_karti_BOSSA_belge_adres_UYDURMAZ_null_doner()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "C3 belge bos");
        var db = vt.Olustur(k);
        var id = await PusulaYaz(db, k);

        var belge = await Servis(vt.Olustur(k)).Belge(id, k.Islem);
        var duzenleyen = Alan(belge, "duzenleyen")!;

        Assert.Null(Alan(duzenleyen, "adres"));
        Assert.Null(Alan(duzenleyen, "vergiDairesi"));
        Assert.Null(Alan(duzenleyen, "vergiNo"));
        Assert.Equal("C3 belge bos", Alan(duzenleyen, "unvan"));
    }
}
