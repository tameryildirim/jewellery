// ═════════════════════════════════════════════════════════════════════════════
// PAKET S2-5b (PLAN 2.8) — SATICI PERSONEL: SUNUCU ⇄ İSTEMCİ SÖZLEŞMESİ
//
// GET /api/v1/islemler/personeller bu tipi DÖNDÜRÜR. Satış ekranı "Satan"
// seçicisini bununla kurar; kimin başkasını seçebileceğine SUNUCU karar verir
// (SecimSerbest) — istemci rol listesi gömmez (yetki ≠ mod).
// Kişisel veri en az: yalnız kimlik + ad + soyad (e-posta/rol TAŞINMAZ).
// Personel bilgisi YASAL BELGE ALANI DEĞİLDİR; yalnız iç kayıttır.
// ═════════════════════════════════════════════════════════════════════════════
namespace Kuyumcu.Contracts.Sayfalar.SaticiPersonel;

/// <summary>Satışı yapan olarak seçilebilecek bir kullanıcı.</summary>
/// <param name="Id">Kullanıcı kimliği.</param>
/// <param name="Ad">Ad.</param>
/// <param name="Soyad">Soyad.</param>
public sealed record SaticiPersonelSatiri(string Id, string Ad, string Soyad);

/// <summary>Satıcı seçici verisi.</summary>
/// <param name="Personeller">Seçilebilir AKTİF kullanıcılar (kiracının). Seçim serbest değilse yalnız oturum kullanıcısı.</param>
/// <param name="OturumId">Oturum kullanıcısının kimliği — varsayılan satıcı.</param>
/// <param name="SecimSerbest">Oturum kullanıcısı başka personeli "satan" olarak yazabilir mi (sunucu kararı).</param>
/// <param name="DahaFazlasiVar">Liste tavana değdi (UX-S29: sessiz kırpma yok, ekran söyler).</param>
public sealed record SaticiPersonelListesi(
    List<SaticiPersonelSatiri> Personeller, string OturumId, bool SecimSerbest, bool DahaFazlasiVar = false);
