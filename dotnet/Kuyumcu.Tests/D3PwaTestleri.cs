using System.Text.Json;

namespace Kuyumcu.Tests;

// D3-01 (19.10) — ana ekrana ekle: manifest geçerli, ikonlar ByGatech amblemi boyutlarında, index.html bağları var.
public sealed class D3PwaTestleri
{
    [Fact]
    public void Manifest_ikonlar_ve_bas_etiketleri_tutarli()
    {
        var m = JsonDocument.Parse(CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "manifest.json")).RootElement;
        Assert.Equal("standalone", m.GetProperty("display").GetString());
        Assert.Equal("/", m.GetProperty("start_url").GetString());
        foreach (var i in m.GetProperty("icons").EnumerateArray())
        {
            var yol = CssKaynak.Yol("Kuyumcu.Web", "wwwroot", i.GetProperty("src").GetString()!);
            Assert.True(File.Exists(yol), yol);
            var boyut = i.GetProperty("sizes").GetString()!.Split('x').Select(int.Parse).ToArray();
            var b = File.ReadAllBytes(yol);
            Assert.Equal(boyut[0], (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19]);   // PNG IHDR genişlik
        }
        var html = CssKaynak.Metin("Kuyumcu.Web", "wwwroot", "index.html");
        Assert.Contains("rel=\"manifest\" href=\"manifest.json\"", html);
        Assert.Contains("rel=\"apple-touch-icon\"", html);
        Assert.Contains("apple-mobile-web-app-capable", html);
    }
}
