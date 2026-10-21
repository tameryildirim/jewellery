using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Http;

namespace Kuyumcu.Tests;

/// <summary>
/// Görsel denetim O-18 (21.10): /api/v1/denetim/ozet canlıda bir kez 500 döndü — "Mutabakat ihlali —
/// 'denetim.ozet.eylemler': özet 25908, listenin toplamı 25907". Kırılımlar ve toplam ayrı sorgulardı; denetim izi
/// her istekte büyüdüğü için arada yazılan satır sayıları ayırıyordu. Özet artık tek anlık görüntüden okur.
/// Test: özet okunurken başka bağlantı sürekli iz yazar; hiçbir özet mutabakat ihlali vermemeli ve kırılım
/// toplamları kendi toplamına eşit olmalı.
/// </summary>
[Collection("veritabani")]
public sealed class GorselDenetimO18Testleri(TestVeritabani vt)
{
    [Fact]
    public async Task O18_ozet_eszamanli_yazma_altinda_mutabik_kalir()
    {
        var k = TestKurulum.KiraciKur(vt.OlusturSistem(), "O18 denetim ozet");
        var yazanDb = vt.Olustur(k);
        var yazan = new DenetimServisi(yazanDb, new HttpContextAccessor());
        for (var i = 0; i < 20; i++) await yazan.ListeOkumaKaydet(k.Islem, "O18Tohum", i);

        using var dur = new CancellationTokenSource();
        var yazilan = 0;
        var yazma = Task.Run(async () =>
        {
            while (!dur.IsCancellationRequested)
            {
                await yazan.ListeOkumaKaydet(k.Islem, "O18Eszamanli", yazilan);
                yazilan++;
            }
        });

        try
        {
            for (var tur = 0; tur < 40; tur++)
            {
                var okuDb = vt.Olustur(k);
                var servis = new Kuyumcu.Api.Modules.Denetim.DenetimOkumaServisi(okuDb, new DenetimServisi(okuDb, new HttpContextAccessor()));
                var ozet = await servis.Ozet(k.Islem, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
                var json = System.Text.Json.JsonSerializer.SerializeToElement(ozet);
                var toplam = json.GetProperty("toplam").GetInt32();
                Assert.Equal(toplam, json.GetProperty("eylemler").EnumerateArray().Sum(x => x.GetProperty("adet").GetInt32()));
                Assert.Equal(toplam, json.GetProperty("turler").EnumerateArray().Sum(x => x.GetProperty("adet").GetInt32()));
            }
        }
        finally
        {
            dur.Cancel();
            await yazma;
        }
        Assert.True(yazilan > 40, $"eşzamanlı yazma yetersiz kaldı ({yazilan}) — test yarışı sınamıyor");
    }
}
