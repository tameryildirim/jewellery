using Kuyumcu.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kuyumcu.Api.Modules.Kur;

/// <summary>
/// Canlı kur push kanalı (60 sn'lik yoklama yerine).
/// İstemci bağlanınca kendi tenant'ının grubuna eklenir; yeni bir kur snapshot'ı
/// yazıldığında (zamanlayıcı / elle çekim / manuel giriş) yalnız o tenant'ın
/// bağlı istemcilerine "KurGuncellendi" olayı gönderilir.
///
/// Kimlik JWT ile; WebSocket taşımasında token Authorization başlığı yerine
/// access_token query'sinden okunur (bkz. Program.cs JwtBearerEvents.OnMessageReceived).
/// </summary>
[Authorize]
public sealed class KurHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Tenant grubu: yayınlar tenant bazında izole edilir (çok kiracılı güvenlik)
        var tenantId = Context.User is { } u ? AktifKullanici.FromClaims(u).TenantId : null;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        await base.OnConnectedAsync();
    }
}
