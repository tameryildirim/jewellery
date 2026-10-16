// PAKET KESIF-P5 · SATIS-4 (c) — POS ürün arama satırında fiyat.
// Sunucu: Kuyumcu.Api/Modules/Fiyat/FiyatTopluTeklifModulu.cs (GET /api/v1/fiyat/teklifler).
// İstemci: Kuyumcu.Web/Pages/Islemler.Arama.cs. Tek tanım (SÖZLEŞME-1): iki taraf aynı türü kullanır.
namespace Kuyumcu.Contracts.Fiyat;

/// <summary>Bir ürünün POS teklifi özeti — <c>TeklifVar=false</c> iken <c>SatisFiyati</c> null ve sebep yazılıdır.</summary>
public sealed record TopluTeklifSatiri(string UrunId, bool TeklifVar, decimal? SatisFiyati, string? Sebep);

/// <summary>GET /fiyat/teklifler yanıtı — istenen sıra korunur; bulunamayan kimlik satır üretmez.</summary>
public sealed record TopluTeklifYaniti(List<TopluTeklifSatiri> Satirlar);
