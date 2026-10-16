using Kuyumcu.Api.Data;

namespace Kuyumcu.Api.Modules.Isler;

// ═════════════════════════════════════════════════════════════════════════════
// DIŞ KANAL TEKELİ — ADR-0016 gerekçesi (paket 6, Bertan + Mehmet koşulu).
//
// E-posta / SMS / WhatsApp GÖNDERİMİ bu üründe YOKTUR ve sözleşme (operatör,
// KVKK aydınlatma, gönderen kimliği) olmadan AÇILMAZ. Arayüz yine de burada
// durur ki ileride kanal eklendiğinde iş kodları değişmesin.
//
// ⚠ DI'da IBildirimKanali uygulaması TAM 1'dir (UygulamaIciKanal). İkinci bir
//   uygulama kaydedilirse IsCatisiSafTestleri KIRMIZI olur ve denetim.sh
//   "IBildirimKanali DI kaydı = 1" sayacı BAŞARISIZ verir. Bu, "sessizce
//   dışarıya veri gönderen ikinci bir yol" açılmasını kod düzeyinde engeller.
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Bildirimin kullanıcıya ULAŞTIRILDIĞI kanal.</summary>
public interface IBildirimKanali
{
    string Kod { get; }

    Task Gonder(Bildirim bildirim, CancellationToken iptal);
}

/// <summary>
/// TEK uygulama: bildirim veritabanına yazıldığı anda "teslim edilmiş" sayılır;
/// kullanıcı onu /bildirimler ekranında ve üst bardaki rozette görür. Dışarıya
/// hiçbir istek gitmez.
/// </summary>
public sealed class UygulamaIciKanal : IBildirimKanali
{
    public string Kod => "UYGULAMA_ICI";

    public Task Gonder(Bildirim bildirim, CancellationToken iptal) => Task.CompletedTask;
}
