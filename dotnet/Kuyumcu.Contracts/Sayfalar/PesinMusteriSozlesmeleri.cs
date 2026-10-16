// ═════════════════════════════════════════════════════════════════════════════
// PAKET B-3 (karar D2) — "PEŞİN MÜŞTERİ" SİSTEM CARİSİ: SUNUCU ⇄ İSTEMCİ SÖZLEŞMESİ
//
// GET /api/v1/cari/sistem-carisi bu tipi DÖNDÜRÜR (Task<object> DEĞİL) — tip
// sunucu imzasında durduğu için ad/tip kayması DERLEME hatasıdır
// (SozlesmeKapsamTestleri: kilitsiz tip cırcırı artmaz).
// Kişisel veri TAŞIMAZ: kart kimliksizdir.
// ═════════════════════════════════════════════════════════════════════════════
namespace Kuyumcu.Contracts.Sayfalar.PesinMusteri;

/// <summary>Kiracının "Peşin Müşteri" kartı — satış ekranı ön seçim için okur.</summary>
/// <param name="Id">Cari kimliği (sunucuda tohumlanır; istemci üretmez).</param>
/// <param name="Ad">Kartın görünen adı (düzenlenebilir).</param>
/// <param name="Tur">Cari türü (MUSTERI).</param>
/// <param name="Aktif">Her zaman true — kart pasife alınamaz.</param>
/// <param name="SistemCarisi">Her zaman true.</param>
public sealed record PesinMusteriKarti(string Id, string Ad, string Tur, bool Aktif, bool SistemCarisi);
