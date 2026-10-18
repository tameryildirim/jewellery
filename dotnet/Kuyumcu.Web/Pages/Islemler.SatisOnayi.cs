using System.Text.Json;
using Kuyumcu.Contracts.Sayfalar.Islemler;
using Kuyumcu.Core.I18n;
using Kuyumcu.Web.Servisler;
using Microsoft.AspNetCore.Components;

namespace Kuyumcu.Web.Pages;

// ═════════════════════════════════════════════════════════════════════════════
// D1-05 (18.10) — SATIŞ ONAYI (tezgâh). Sunucu eşik aşan satışı 409 + ONAY_GEREKLI ile reddeder (hiçbir şey yazılmaz).
// Bant "Onay iste" der; gönderilen satış gövdesinin AYNISI POST /satis-onayi/talepler'e gider (sunucu ölçer),
// patrona bildirim düşer, tezgâh 4 sn'de bir talebin durumunu sorar. Onaylanınca Kaydet onayTalebiId ile gider.
// Fiş değişirse sunucu onayı reddeder ve bant yeniden "Onay iste" der.
// ═════════════════════════════════════════════════════════════════════════════
public partial class Islemler
{
    private bool onayGerekli;
    private OnayTalebiOzet? onayTalebi;
    private string? onayHata;
    private bool onayIsteniyor;
    private Dictionary<string, object?>? sonSatisGovdesi;
    private CancellationTokenSource? onayIptal;

    /// <summary>Satış gövdesi gönderilmeden önce: içerik saklanır (talep aynı içerikle açılsın), onay varsa eklenir.</summary>
    private void OnayGovdeye(Dictionary<string, object?> d)
    {
        if (tur != "SATIS") return;
        sonSatisGovdesi = new(d);
        if (onayTalebi is not { Durum: "ONAYLANDI" } t) return;
        var onayTalebiId = t.Id;   // SatisFisiDto.OnayTalebiId / SatisOlusturDto.OnayTalebiId
        d[nameof(onayTalebiId)] = onayTalebiId;
    }

    private void OnayYakala(ApiHata ex)
    {
        if (ex.Kod != "ONAY_GEREKLI") return;
        onayGerekli = true; hata = null;
        if (onayTalebi is { Durum: not "BEKLIYOR" }) { onayTalebi = null; onayHata = D1.OnayGecersiz; }
    }

    private async Task OnayIste()
    {
        if (onayIsteniyor || sonSatisGovdesi is null) return;
        onayIsteniyor = true; onayHata = null;
        try
        {
            var g = JsonSerializer.SerializeToElement(sonSatisGovdesi);
            object kalemler = g.TryGetProperty("kalemler", out var kl) && kl.ValueKind == JsonValueKind.Array
                ? kl
                : new[] { new { urunId = g.GetProperty("urunId").GetString(), adet = g.GetProperty("adet").GetDecimal(), birimFiyatTl = g.GetProperty("birimFiyatTl").GetDecimal() } };
            object? fisIsk = g.TryGetProperty("fisIskontoYuzde", out var fi) ? (fi.ValueKind == JsonValueKind.Number ? fi.GetDecimal() : null) : 0m;
            onayTalebi = await Api.PostAsync<OnayTalebiOzet>("/satis-onayi/talepler", new
            {
                cariId = g.TryGetProperty("cariId", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null,
                kurSnapshotId = g.GetProperty("kurSnapshotId").GetString(),
                fisIskontoYuzde = fisIsk,
                kalemler,
            });
            OnayBekle();
        }
        catch (ApiHata ex) { onayHata = ex.Message; }
        finally { onayIsteniyor = false; }
    }

    /// <summary>Talep karar bekledikçe 4 sn'de bir durum sorulur (onay süresi kadar; sayfadan çıkınca durur).</summary>
    private void OnayBekle()
    {
        onayIptal?.Cancel();
        var iptal = onayIptal = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        _ = Task.Run(async () =>
        {
            while (!iptal.IsCancellationRequested && onayTalebi is { Durum: "BEKLIYOR" } t)
            {
                try { await Task.Delay(4000, iptal.Token); } catch (TaskCanceledException) { return; }
                try
                {
                    var y = await Api.GetAsync<OnayTalebiOzet>($"/satis-onayi/talepler/{Uri.EscapeDataString(t.Id)}");
                    if (iptal.IsCancellationRequested || onayTalebi?.Id != t.Id) return;
                    onayTalebi = y;
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex) when (ex is ApiHata or JsonException) { onayHata = ex.Message; await InvokeAsync(StateHasChanged); return; }
            }
        });
    }

    private void OnayBirak()
    {
        onayIptal?.Cancel(); onayIptal = null;
        onayGerekli = false; onayTalebi = null; onayHata = null;
    }

    private string OnaySebepMetni(OnayTalebiOzet t) => string.Join(" · ", t.Sebepler.Select(s => s switch
    {
        "ISKONTO" => string.Format(D1.OnaySebepIskonto, Bicim.Oran(t.IndirimYuzde)),
        "KISISEL" => string.Format(D1.OnaySebepKisisel, Bicim.Oran(t.IndirimYuzde)),
        "TUTAR" => string.Format(D1.OnaySebepTutar, Bicim.TlKurus(t.ToplamTl)),
        "ZARARINA" => D1.OnaySebepZararina,
        _ => s,
    }));

    private RenderFragment? OnayBandi => !onayGerekli && onayTalebi is null ? null : b =>
    {
        b.OpenComponent<Kuyumcu.Web.Bilesenler.Fis.SatisOnayBandi>(0);
        b.AddAttribute(1, "Talep", onayTalebi);
        b.AddAttribute(2, "SebepMetni", onayTalebi is null ? null : OnaySebepMetni(onayTalebi));
        b.AddAttribute(3, "Hata", onayHata);
        b.AddAttribute(4, "Calisiyor", onayIsteniyor);
        b.AddAttribute(5, "Iste", EventCallback.Factory.Create(this, OnayIste));
        b.CloseComponent();
    };
}
