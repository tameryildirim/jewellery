using System.Diagnostics;

namespace Kuyumcu.Api.Modules.Telemetri;

// ═════════════════════════════════════════════════════════════════════════════
// TELEMETRİ ARACI (middleware) — PAKET P-28 · KİRACI ETİKETLİ ÖLÇÜM NOKTASI.
//
// ⚠ MEHMET KOŞULU — BU DOSYA İSTEK İÇERİĞİNE BAKMAZ:
//   okunan TEK şey `tenantId` claim'i (etiket), süre (Stopwatch), yazılan
//   bayt (SayanAkis) ve durum kodudur. Request.Path / QueryString / Query /
//   Headers / Body ve diğer claim'ler bu dosyada GEÇMEZ; denetim.sh
//   TELEMETRIDE_KISISEL_VERI sayacı bunu dosya üzerinde ölçer.
//
// ⚠ BORU HATTI YERİ: UseAuthentication'dan SONRA (kiracı claim'i çözülmüş
//   olmalı) ve MapControllers'tan ÖNCE. Statik dosyalar (UseStaticFiles daha
//   önce) ölçülmez — onlar kiracıya ait değildir, Blazor varlığıdır.
//
// ⚠ WEBSOCKET MUAF: /hubs yükseltme isteğinde gövde akışı devralınır;
//   sarmalamak bağlantıyı bozardı. Ölçülmez ve bu SINIR yazılıdır.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Yanıt gövdesine yazılan baytı SAYAN, içeriğe BAKMAYAN akış sarmalı.</summary>
internal sealed class SayanAkis(Stream ic) : Stream
{
    public long Sayac { get; private set; }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => Sayac; set => throw new NotSupportedException(); }

    public override void Flush() => ic.Flush();
    public override Task FlushAsync(CancellationToken t) => ic.FlushAsync(t);

    public override void Write(byte[] buffer, int offset, int count)
    {
        Sayac += count;
        ic.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Sayac += buffer.Length;
        ic.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken t)
    {
        Sayac += count;
        return ic.WriteAsync(buffer, offset, count, t);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken t = default)
    {
        Sayac += buffer.Length;
        return ic.WriteAsync(buffer, t);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}

/// <summary>Kiracı etiketli istek/süre/bayt sayacını besleyen ara katman.</summary>
public sealed class TelemetriAraci(RequestDelegate ileri, TelemetriToplayici toplayici)
{
    /// <summary>Kiracı ETİKETİNİN okunduğu TEK claim adı.</summary>
    public const string KiraciClaim = "tenantId";

    public async Task Invoke(HttpContext baglam)
    {
        if (baglam.WebSockets.IsWebSocketRequest)
        {
            await ileri(baglam);
            return;
        }

        var kronometre = Stopwatch.StartNew();
        var asilAkis = baglam.Response.Body;
        var sayan = new SayanAkis(asilAkis);
        baglam.Response.Body = sayan;
        try
        {
            await ileri(baglam);
        }
        finally
        {
            baglam.Response.Body = asilAkis;
            kronometre.Stop();
            // ⚠ Etiket dışında HİÇBİR kimlik okunmaz.
            var etiket = baglam.User.FindFirst(KiraciClaim)?.Value;
            toplayici.Kaydet(
                etiket,
                DateTime.UtcNow,
                kronometre.ElapsedMilliseconds,
                sayan.Sayac,
                baglam.Response.StatusCode >= 500);
        }
    }
}
