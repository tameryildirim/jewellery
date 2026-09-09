namespace Kuyumcu.Api.Modules;

/// <summary>
/// Modül servislerinin DI kaydı — her modülün servisi {Modul}Servisi adlanır.
/// Program.cs bu tek noktayı çağırır; modül ajanları bu dosyaya DOKUNMAZ.
/// </summary>
public static class ModulKayit
{
    public static IServiceCollection AddKuyumcuModulleri(this IServiceCollection s)
    {
        s.AddScoped<Auth.AuthServisi>();
        s.AddScoped<Kullanici.KullaniciServisi>();
        s.AddScoped<Kur.KurServisi>();
        s.AddScoped<Cari.CariServisi>();
        s.AddScoped<Urun.UrunServisi>();
        s.AddScoped<Satis.SatisServisi>();
        s.AddScoped<Kasa.KasaServisi>();
        s.AddScoped<Emanet.EmanetServisi>();
        s.AddScoped<Banka.BankaServisi>();
        s.AddScoped<Fatura.FaturaServisi>();
        s.AddScoped<Rapor.RaporServisi>();
        s.AddScoped<Sube.SubeServisi>();
        s.AddScoped<Admin.AdminServisi>();
        s.AddScoped<Yetki.YetkiServisi>();
        return s;
    }
}
