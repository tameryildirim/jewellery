// P-03 SÖZLEŞME-1 — Kuyumcu.Contracts'a SAF YER DEĞİŞTİRME (alan eklenmedi, tip değişmedi).
// Kaynak: Kuyumcu.Web/Pages/VeriDisaAktarim.razor @code bloğu.
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır.
using System.Text.Json;

namespace Kuyumcu.Contracts.Sayfalar.VeriDisaAktarim;
public sealed record KaynakVeri(string Ad, bool KisiselVeri, string[] Alanlar);
public sealed record GecmisVeri(
    string Id, string Gerekce, string Kapsam, bool Maskeli,
    DateTime Olusturuldu, DateTime? Kullanildi, int? SatirSayisi);
public sealed record GecmisYanit(int Toplam, List<GecmisVeri> Kayitlar);
