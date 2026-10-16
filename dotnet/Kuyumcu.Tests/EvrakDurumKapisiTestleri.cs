using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules.Evrak;

namespace Kuyumcu.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// EVRAK DURUM MAKİNESİ — SAF testler (veritabanı yok).
//
// Tablo güdümlüdür: izinli geçişlerin TAMAMI ve izinsiz geçişlerin TAMAMI
// tek kaynaktan (EvrakGecisleri.Izinli) üretilir. Elle yazılmış bir liste,
// yeni bir durum eklendiğinde sessizce eksik kalırdı.
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EvrakDurumKapisiTestleri
{
    public static TheoryData<string, string> IzinliGecisler()
    {
        var d = new TheoryData<string, string>();
        foreach (var (mevcut, hedefler) in EvrakGecisleri.Izinli)
            foreach (var hedef in hedefler)
                d.Add(mevcut, hedef);
        return d;
    }

    public static TheoryData<string, string> IzinsizGecisler()
    {
        var d = new TheoryData<string, string>();
        foreach (var mevcut in EvrakDurumu.Tumu)
            foreach (var hedef in EvrakDurumu.Tumu)
                if (!EvrakGecisleri.Izinli[mevcut].Contains(hedef))
                    d.Add(mevcut, hedef);
        return d;
    }

    [Theory]
    [MemberData(nameof(IzinliGecisler))]
    public void Izinli_gecislerin_tamami_gecer(string mevcut, string hedef)
    {
        // Atmaması GEREKİR; atarsa xUnit testi kırar.
        EvrakDurumKapisi.Dogrula(mevcut, hedef);
    }

    [Theory]
    [MemberData(nameof(IzinsizGecisler))]
    public void Izinsiz_gecislerin_tamami_reddedilir(string mevcut, string hedef)
    {
        var ex = Assert.Throws<BadRequestException>(() => EvrakDurumKapisi.Dogrula(mevcut, hedef));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void Tahsil_edildi_SON_durumdur_hicbir_gecisi_yoktur()
    {
        Assert.Empty(EvrakGecisleri.Izinli[EvrakDurumu.TahsilEdildi]);
        Assert.Empty(EvrakDurumKapisi.IzinliGecisler(EvrakDurumu.TahsilEdildi));

        foreach (var hedef in EvrakDurumu.Tumu)
            Assert.Throws<BadRequestException>(
                () => EvrakDurumKapisi.Dogrula(EvrakDurumu.TahsilEdildi, hedef));
    }

    [Fact]
    public void Bilinmeyen_durum_FAIL_CLOSED_reddedilir()
    {
        // "Tanımadığım durumdan her yere gidilebilir" varsayımı YAPILMAZ.
        Assert.Throws<BadRequestException>(
            () => EvrakDurumKapisi.Dogrula("UYDURMA", EvrakDurumu.TahsilEdildi));
        Assert.Throws<BadRequestException>(
            () => EvrakDurumKapisi.Dogrula(EvrakDurumu.Portfoyde, "UYDURMA"));
    }

    [Fact]
    public void Her_durum_gecis_tablosunda_TANIMLIDIR()
    {
        // Yeni bir durum eklenip tabloya yazılmazsa kapı onu bilmez ve o
        // durumdaki her evrak kilitlenirdi — bu test onu derleme sonrası yakalar.
        foreach (var durum in EvrakDurumu.Tumu)
            Assert.True(EvrakGecisleri.Izinli.ContainsKey(durum), $"Geçiş tablosunda yok: {durum}");
    }
}
