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
builder.Services.AddScoped<ApiIstemci>();
builder.Services.AddScoped<CanliKurServisi>();   // canlı kur push (SignalR)
builder.Services.AddScoped<YetkiServisi>();      // rol → modül erişimi (menü gizleme)

var host = builder.Build();

// Kalıcı durumları açılışta yükle
var oturum = host.Services.GetRequiredService<OturumServisi>();
var dil = host.Services.GetRequiredService<DilServisi>();
await oturum.YukleAsync();
await dil.YukleAsync();

await host.RunAsync();
