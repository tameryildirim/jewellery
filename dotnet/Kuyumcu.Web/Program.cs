using Kuyumcu.Web;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API aynı origin'den sunulur (Kuyumcu.Api hem WASM'ı hem /api/v1'i barındırır)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<OturumServisi>();
builder.Services.AddScoped<DilServisi>();
builder.Services.AddScoped<TemaServisi>();     // aydınlık/karanlık tema (UX-2)
builder.Services.AddScoped<GorunumServisi>();  // P-36b: kiracı renk şeması + yazı ölçeği (talimat 16)
builder.Services.AddScoped<KullaniciGorunumServisi>();  // B-1: kullanıcının KENDİ görünümü (tuval n4)
builder.Services.AddScoped<ArayuzModuServisi>();  // basit/profesyonel arayüz modu (UX-3)
builder.Services.AddScoped<ApiIstemci>();
builder.Services.AddScoped<CanliKurServisi>();   // canlı kur push (SignalR)
builder.Services.AddScoped<YetkiServisi>();      // rol → modül erişimi (menü gizleme)
builder.Services.AddScoped<MenuTercihServisi>();  // P-37: kullanıcıya özel favori/sıra/panel düzeni
builder.Services.AddScoped<HavaServisi>();       // P-37b: karşılama hava kutucuğu (DIŞ KAYNAK, varsayılan KAPALI)
builder.Services.AddScoped<UyumServisi>();       // MASAK + gider pusulası (PLAN-2 Adım 1)
builder.Services.AddScoped<PosServisi>();     // POS alacak defteri istemcisi (paket 31)
builder.Services.AddScoped<DuzeltmePolitikaServisi>();  // düzeltme politikası haritası (UX-1 Faz A) — oturumda tek çekim
builder.Services.AddScoped<RaporServisi>();      // rapor seti tiplenmiş istemcisi (paket 34 Faz A)
builder.Services.AddScoped<SurumServisi>();    // sistem künyesi — oturumda tek çekim (paket T-4a)

var host = builder.Build();

// Kalıcı durumları açılışta yükle
var oturum = host.Services.GetRequiredService<OturumServisi>();
var dil = host.Services.GetRequiredService<DilServisi>();
// PAKET KAT-1 (K8): basit modda sözlük D10 katmanına geçer; mod ya da oturum
// değişince dil olayı (yalnız mod GERÇEKTEN değiştiyse) ekranları yeniden çizer.
var arayuzModu = host.Services.GetRequiredService<ArayuzModuServisi>();
dil.BasitModuBagla(() => arayuzModu.BasitMi);
arayuzModu.Degisti += dil.ModuDenetle;
// PAKET ISTEK-1: ApiIstemci hata metinleri (yoğunluk) etkin dilde yazılır.
host.Services.GetRequiredService<ApiIstemci>().DilKaynagi = () => dil.Dil;
oturum.Degisti += dil.ModuDenetle;
var tema = host.Services.GetRequiredService<TemaServisi>();
await oturum.YukleAsync();
await dil.YukleAsync();
await tema.YukleAsync();

// P-36b — kiracı görünümü. ⚠ Oturum YOKKEN istenmez: /gorunum kimlik ister ve
// giriş ekranında 401 alıp gereksiz hata üretirdi. Oturum açıldığında (ya da
// yenilendiğinde) yeniden okunur; olay aboneliği tek yerdedir.
var gorunum = host.Services.GetRequiredService<GorunumServisi>();
// KESIF-P1 · KIYAS TEMA-TEK: aydınlık/karanlık (ve kişisel şema/punto) için TEK
// doğruluk kaynağı KULLANICI SATIRIDIR (/kullanici/gorunum). Eskiden yalnız
// /ayarlar açılınca okunuyordu → /ayarlar'a girmeden önceki sayfalar cihaz
// önbelleğiyle, sonrakiler sunucu değeriyle boyanıyor, tema sayfadan sayfaya
// değişiyordu. Artık açılışta ve oturum her değiştiğinde okunur ve uygulanır;
// SIRA: önce dükkân (kiracı) görünümü, SONRA kullanıcının çözülmüş görünümü
// (kullanıcı seçimi dükkânı ezer — ters sıra kişisel şemayı silerdi).
// tema.js'in localStorage'ı yalnız İLK BOYAMA önbelleğidir (FOUC): değeri
// kuyumcuTema.uygula yalnız sunucu yanıtıyla yazar.
var benimGorunum = host.Services.GetRequiredService<KullaniciGorunumServisi>();
async Task GorunumleriYukle()
{
    await gorunum.YukleAsync();
    await benimGorunum.YukleAsync();
}
if (oturum.GirisYapildi) await GorunumleriYukle();
oturum.Degisti += () => { if (oturum.GirisYapildi) _ = GorunumleriYukle(); };

await host.RunAsync();
