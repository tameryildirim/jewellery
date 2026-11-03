// 02.11 — GİRİŞ: AYNI E-POSTA BİRDEN FAZLA İŞLETMEDE → İŞLETME SEÇİMİ SÖZLEŞMESİ.
//
// Kural (PLAN § 1 md.9): yeni istemci record'u razor içine yazılmaz, buraya yazılır;
// ad VE tip sunucuyla aynıdır. Kuyumcu.Api (/auth/giris yanıtı) üretir, Kuyumcu.Web
// (Giris.razor "Hangi işletme?" seçimi) tüketir.
//
// Sunucu bu listeyi YALNIZ parola doğrulandıktan sonra ve yalnız parolası tutan
// işletmeler için döner (e-posta sayımı engeli korunur). ULID ekrana basılmaz:
// kullanıcı Ad (+ rol) görür, TenantId gövdede gider.

namespace Kuyumcu.Contracts.Giris;

/// <summary>Girişte seçilebilir işletme: kimlik gövdede, ad ekranda.</summary>
public sealed record IsletmeSecenegi(string TenantId, string Ad, string? Rol = null);
