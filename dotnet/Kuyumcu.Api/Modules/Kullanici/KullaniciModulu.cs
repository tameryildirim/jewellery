using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Kullanici;

// ═════════════════════════════════════════════════════════════════════════════
// Kullanıcı modülü — NestJS kaynağı: apps/api/src/modules/kullanici/*
//   • Rota: api/v1/kullanicilar (Nest ile birebir)
//   • Rol hiyerarşisi: SAHIP(4) > MUDUR(3) > MUHASEBE(2) > TEZGAHTAR(1)
//   • Şifreler bcrypt (work factor 12) ile hash'lenir — Nest'teki bcryptjs eşleniği
//   • sifreHash hiçbir response'ta dönmez (Temizle)
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class KullaniciOlusturDto
{
    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Ad 2-100 karakter olmalıdır")]
    public string Ad { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad 2-100 karakter olmalıdır")]
    public string Soyad { get; set; } = null!;

    // PAKET GOC-D (K3): [EmailAddress] yalnız "@" arıyordu; ekranın <Eposta>'sıyla AYNI Core kuralı + metin.
    [Required, Kuyumcu.Api.Common.Dogrulama.Eposta]
    public string Email { get; set; } = null!;

    [Required, StringLength(72, MinimumLength = 8, ErrorMessage = "Şifre 8-72 karakter olmalıdır")]
    [RegularExpression("^(?=.*[0-9])(?=.*[a-zA-Z]).+$", ErrorMessage = "Şifre en az bir harf ve bir rakam içermelidir")]
    public string Sifre { get; set; } = null!;

    [Required, RegularExpression("SAHIP|MUDUR|TEZGAHTAR|MUHASEBE", ErrorMessage = "Geçersiz rol")]
    public string Rol { get; set; } = null!;

    public string? SubeId { get; set; }

    /// <summary>
    /// Yeni kullanıcının arayüz modu: BASIT | PROFESYONEL. Boş/null = kiracı
    /// varsayılanını devral. (Bertan KOŞUL 8: oluşturma yolunda da olmalı;
    /// yoksa sahip her yeni tezgâhtarı ikinci bir adımda düzenlemek zorunda kalır.)
    /// </summary>
    [RegularExpression("BASIT|PROFESYONEL", ErrorMessage = "Geçersiz arayüz modu")]
    public string? ArayuzModu { get; set; }

    /// <summary>D1-14 — salt okunur (mali müşavir) hesap; yalnız MUHASEBE rolüyle verilebilir.</summary>
    public bool SaltOkunur { get; set; }
    /// <summary>D1-05 — kişisel iskonto sınırı (yüzde); boş = sınır yok.</summary>
    [Range(0, 100, ErrorMessage = "İskonto sınırı 0 ile 100 arasında olmalıdır")] public decimal? IskontoSiniriYuzde { get; set; }
    /// <summary>D1-20 — prim yüzdesi ve aylık hedef; boş = yok.</summary>
    [Range(0, 100, ErrorMessage = "Prim yüzdesi 0 ile 100 arasında olmalıdır")] public decimal? PrimYuzde { get; set; }
    [Range(0, 1_000_000_000, ErrorMessage = "Aylık hedef sıfır ya da daha büyük olmalıdır")] public decimal? AylikHedefTl { get; set; }
}

public sealed class KullaniciGuncelleDto
{
    private string? _subeId;
    private string? _arayuzModu;

    [StringLength(100, MinimumLength = 2)] public string? Ad { get; set; }
    [StringLength(100, MinimumLength = 2)] public string? Soyad { get; set; }

    // PAKET GOC-D (K3): Core EpostaDogrulayici (uzunluk 254 kuralın içinde).
    [Kuyumcu.Api.Common.Dogrulama.Eposta]
    public string? Email { get; set; }

    /// <summary>
    /// JSON gövdesinde subeId alanı geldi mi (açık null dahil)?
    /// Nest'te undefined/null ayrımı var: gönderilen null şubeyi temizler,
    /// hiç gönderilmeyen alan ise dokunulmadan bırakılır.
    /// </summary>
    [JsonIgnore] public bool SubeIdGonderildi { get; private set; }

    public string? SubeId
    {
        get => _subeId;
        set { _subeId = value; SubeIdGonderildi = true; }
    }

    public bool? Aktif { get; set; }

    /// <summary>
    /// JSON gövdesinde arayuzModu alanı geldi mi (açık null dahil)?
    /// SubeId ile AYNI desen (Mehmet K4): JsonElement/Optional&lt;T&gt; gibi
    /// YENİ bir üçlü-durum deseni açılmaz — üç durum tek biçimde çözülür:
    ///   alan YOK      → mevcut değer KORUNUR
    ///   alan null     → kiracı varsayılanına DEVRET
    ///   alan dolu     → BASIT | PROFESYONEL (beyaz liste)
    /// </summary>
    [JsonIgnore] public bool ArayuzModuGonderildi { get; private set; }

    [RegularExpression("BASIT|PROFESYONEL", ErrorMessage = "Geçersiz arayüz modu")]
    public string? ArayuzModu
    {
        get => _arayuzModu;
        set { _arayuzModu = value; ArayuzModuGonderildi = true; }
    }
}

public sealed class SifreDegistirDto
{
    /// <summary>Kendi şifresini değiştiren kullanıcı için gerekli; admin reset'te opsiyonel.</summary>
    public string? MevcutSifre { get; set; }

    [Required, StringLength(72, MinimumLength = 8, ErrorMessage = "Yeni şifre 8-72 karakter olmalıdır")]
    [RegularExpression("^(?=.*[0-9])(?=.*[a-zA-Z]).+$", ErrorMessage = "Şifre en az bir harf ve bir rakam içermelidir")]
    public string YeniSifre { get; set; } = null!;

    [Required]
    public string YeniSifreTekrar { get; set; } = null!;
}

/// <summary>
/// PAKET P-09 (B-36) — "Yeni SAHİP ata ve devret".
/// <para>
/// ⚠ NEDEN GEREKTİ: <c>RolOlusturmayaIzinVarMi</c> yalnız KENDİNDEN DÜŞÜK rol
/// atamaya izin verir; SAHİP'ten yüksek rol olmadığı için hiçbir SAHİP bir
/// başkasını SAHİP yapamıyordu. Tek sahipli bir işletmede sahip ayrıldığında
/// kiracı kilitleniyordu (B-36 "tek SAHİP çıkmazı").
/// </para>
/// <para>
/// ⚠ Devir TEK İŞLEMDİR: hedef SAHİP olur, devreden aynı anda düşer. Araya
/// "iki SAHİP" ya da "sıfır SAHİP" hâli GİRMEZ (tek SaveChanges).
/// </para>
/// </summary>
public sealed class SahiplikDevretDto
{
    /// <summary>Devredenin KENDİ parolası — çalınmış oturum sahipliği devredemesin.</summary>
    [Required(ErrorMessage = "Şifrenizi girmeniz zorunludur")]
    public string Sifre { get; set; } = null!;

    /// <summary>Devredenin devirden sonraki rolü. SAHIP olamaz; boşsa MUDUR.</summary>
    [RegularExpression("MUDUR|MUHASEBE|TEZGAHTAR", ErrorMessage = "Devirden sonraki rolünüz MUDUR, MUHASEBE veya TEZGAHTAR olabilir")]
    public string? YeniRolum { get; set; }
}

/// <summary>P-09 (B-37) — kullanıcıyı pasife alma / geri aktifleştirme.</summary>
public sealed class AktiflikDto
{
    [Required(ErrorMessage = "aktif alanı zorunludur")]
    public bool? Aktif { get; set; }
}

public sealed class RolDegistirDto
{
    [Required, RegularExpression("SAHIP|MUDUR|TEZGAHTAR|MUHASEBE", ErrorMessage = "Geçersiz rol")]
    public string Rol { get; set; } = null!;
    /// <summary>D1-14 — salt okunur (mali müşavir); yalnız MUHASEBE rolüyle. Gönderilmezse kapanır.</summary>
    public bool SaltOkunur { get; set; }
    /// <summary>D1-05 — kişisel iskonto sınırı (yüzde); boş = sınır yok. Her istekte gönderilir.</summary>
    [Range(0, 100, ErrorMessage = "İskonto sınırı 0 ile 100 arasında olmalıdır")] public decimal? IskontoSiniriYuzde { get; set; }
    /// <summary>D1-20 — prim yüzdesi ve aylık hedef; boş = yok.</summary>
    [Range(0, 100, ErrorMessage = "Prim yüzdesi 0 ile 100 arasında olmalıdır")] public decimal? PrimYuzde { get; set; }
    [Range(0, 1_000_000_000, ErrorMessage = "Aylık hedef sıfır ya da daha büyük olmalıdır")] public decimal? AylikHedefTl { get; set; }
}

// ── Servis ───────────────────────────────────────────────────────────────────

public sealed class KullaniciServisi(KuyumcuDbContext db, DenetimServisi denetim)
{
    private const int BcryptRounds = 12;

    // ─── Kullanıcı oluştur ────────────────────────────────────────────────────

    public async Task<object> Olustur(KullaniciOlusturDto dto, AktifKullanici islem)
    {
        RolOlusturmayaIzinVarMi(islem.Rol, dto.Rol);
        // PAKET GOC-D: DataAnnotations yalnız controller yolunda — servis yolunda AYNI Core kuralı.
        Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.EpostaDogrulayici.Denetle(dto.Email), "email");

        var mevcutmu = await db.Kullanicilar.AnyAsync(k =>
            k.Email == dto.Email && k.TenantId == islem.TenantId);
        if (mevcutmu) throw new ConflictException("Bu e-posta adresi zaten kayıtlı");
        await EpostaBaskaIsletmedeAktifOlmasin(dto.Email, islem.TenantId);   // 02.11 kurucu: aynı e-posta ile ikinci kayıt YOK

        // K4 denetimi: kullanıcının şubesi doğrulanmıyordu. Bu, kasa kusurundan
        // AĞIRDIR — subeId JWT'ye girer, sonra satış/alış `islem.SubeId`'yi
        // hareketlere yazar. Yabancı bir şube kimliği bu yolla defterlere sızardı.
        if (!string.IsNullOrEmpty(dto.SubeId))
            await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);

        var sifreHash = BCrypt.Net.BCrypt.HashPassword(dto.Sifre, BcryptRounds);
        var now = DateTime.UtcNow;
        var id = Kimlik.Ulid();

        db.Kullanicilar.Add(new Data.Kullanici
        {
            Id = id,
            TenantId = islem.TenantId,
            SubeId = dto.SubeId,
            Ad = dto.Ad,
            Soyad = dto.Soyad,
            Email = dto.Email,
            SifreHash = sifreHash,
            Rol = dto.Rol,
            SaltOkunur = SaltOkunurDogrula(dto.Rol, dto.SaltOkunur),   // D1-14
            IskontoSiniriYuzde = dto.IskontoSiniriYuzde,   // D1-05
            PrimYuzde = dto.PrimYuzde, AylikHedefTl = dto.AylikHedefTl,   // D1-20
            ArayuzModu = OlusturmaModu(dto.ArayuzModu),
            Aktif = true,
            Olusturuldu = now,
            Guncellendi = now,
        });
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Liste ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Kiracının kullanıcıları. Liste SAHIP/MUDUR'e kapalıdır (controller kapısı).
    /// <para>
    /// 45-UI: her satırda iki aşamalı girişin AÇIK/KAPALI olduğu gösterilir.
    /// ⚠ Yalnız BAYRAK döner — giz, kurtarma kodu sayısı, kilit bilgisi DEĞİL;
    /// yönetici "kim korumasız" sorusunu görebilsin diye vardır, başkasının
    /// MFA kaydına erişim yolu AÇILMAZ (Mehmet).
    /// </para>
    /// </summary>
    public async Task<List<object>> Listele(AktifKullanici islem)
    {
        var liste = await db.Kullanicilar.AsNoTracking()
            .Where(k => k.TenantId == islem.TenantId)
            .ToListAsync();

        var mfaAcik = await db.KullaniciMfalari.AsNoTracking()
            .Where(m => m.Durum == MfaDurumu.Etkin)
            .Select(m => m.KullaniciId)
            .ToListAsync();

        return liste.Select(k => Temizle(k, mfaAcik.Contains(k.Id))).ToList();
    }

    // ─── Tekil kullanıcı ─────────────────────────────────────────────────────

    public async Task<object> Getir(string id, AktifKullanici islem)
    {
        var kullanici = await TenantKullanicisiBul(id, islem.TenantId);
        return Temizle(kullanici);
    }

    // ─── Güncelle ─────────────────────────────────────────────────────────────

    public async Task<object> Guncelle(string id, KullaniciGuncelleDto dto, AktifKullanici islem)
    {
        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        // B-36 SIRA KURALI: tek SAHİP kapısı rütbe kapısından ÖNCE sorulur.
        // Ters sırada rütbe kapısı ("eşit rol") her zaman önce patlıyor ve
        // kullanıcı ÇIKIŞ YOLUNU (devir) hiç öğrenemiyordu — kilit buydu.
        if (dto.Aktif is not null) await SonSahipKapisi(hedef, hedef.Rol, dto.Aktif.Value);
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);

        // Yalnızca gönderilen alanlar yazılır (Nest: v !== undefined)
        // subeId açık null gönderilirse şube temizlenir — Nest ile birebir
        if (dto.Ad is not null) hedef.Ad = dto.Ad;
        if (dto.Soyad is not null) hedef.Soyad = dto.Soyad;
        await EpostaAyarla(hedef, dto.Email, islem.TenantId);
        // K4: açık null şubeyi temizler (bilinçli); dolu değer DOĞRULANIR.
        if (dto.SubeIdGonderildi)
        {
            if (!string.IsNullOrEmpty(dto.SubeId))
                await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);
            hedef.SubeId = dto.SubeId;
        }
        if (dto.Aktif is not null)
        {
            // Kapı yukarıda, rütbe kapısından ÖNCE çağrıldı (B-36 sıra kuralı).
            if (dto.Aktif.Value && !hedef.Aktif)
                await EpostaBaskaIsletmedeAktifOlmasin(hedef.Email, hedef.TenantId);   // 02.11: aktife alınca da tek işletme
            if (dto.Aktif.Value != hedef.Aktif)
                denetim.YazmaKaydet(islem, nameof(Data.Kullanici), hedef.Id,
                    dto.Aktif.Value ? "kullanici-aktiflestirildi" : "kullanici-pasife-alindi");
            hedef.Aktif = dto.Aktif.Value;
        }
        // Mehmet K5: bu satır HedefUzerindekiYetkiKontrol çağrısının ARKASINDADIR;
        // MUDUR bir SAHIP.in modunu değiştiremez. Rol değişimi AYRI uçtadır.
        ArayuzModuAyarla(hedef, dto);
        hedef.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Kendi profilini güncelle ─────────────────────────────────────────────

    public async Task<object> KendinGuncelle(KullaniciGuncelleDto dto, AktifKullanici islem, bool yalnizMod = false)
    {
        // PAKET ISLEM-BANT — ALAN BEYAZ LİSTESİ: "kullanici" modülü olmayan rol (varsayılan matriste
        // TEZGAHTAR, MUHASEBE) bu uçta YALNIZ kendi arayuzModu'nu yazar. Başka alan (ad, soyad, e-posta,
        // şube, aktif) gönderilirse hiçbir şey yazılmadan 403 — yetki GENİŞLEMEZ.
        if (yalnizMod && (dto.Ad is not null || dto.Soyad is not null || dto.Email is not null
                          || dto.SubeIdGonderildi || dto.Aktif is not null))
            throw new ForbiddenException(Kuyumcu.Core.I18n.IslemBantSozlugu.Al(Kuyumcu.Core.I18n.IstekDili.Gecerli).YalnizModDegisir);
        var kullanici = await TenantKullanicisiBul(islem.Id, islem.TenantId);

        // Kendi aktifliğini değiştiremez — aktif alanı yok sayılır
        // subeId açık null gönderilirse şube temizlenir — Nest ile birebir
        if (dto.Ad is not null) kullanici.Ad = dto.Ad;
        if (dto.Soyad is not null) kullanici.Soyad = dto.Soyad;
        await EpostaAyarla(kullanici, dto.Email, islem.TenantId);
        if (dto.SubeIdGonderildi)
        {
            if (!string.IsNullOrEmpty(dto.SubeId))
                await IslemDogrulayici.SubeDogrula(db, dto.SubeId, islem.TenantId);
            kullanici.SubeId = dto.SubeId;
        }
        ArayuzModuAyarla(kullanici, dto);
        if (dto.ArayuzModuGonderildi)   // ISLEM-BANT: dar uçla aynı iz (mod bir tercihtir; hassas değil)
            denetim.YazmaKaydet(islem, nameof(Data.Kullanici), kullanici.Id,
                $"arayuz-modu: {dto.ArayuzModu ?? "devral"}", hassas: false);
        kullanici.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Getir(islem.Id, islem);
    }

    // ─── PAKET S1-f — KENDİ arayüz modunu yaz (her rol) ──────────────────────
    //
    // Tuval Ayarlar.dc.html notu n4: "her kullanıcı ... basit/profesyonel modu
    // seçer". PATCH /kullanicilar/ben "herkes" diye belgeli ama sınıf düzeyindeki
    // [ModulYetkisi("kullanici")] TEZGAHTAR ve MUHASEBE'yi 403 ile durduruyor
    // (varsayılan matriste "kullanici" modülü yok — ölçüldü). O ucu açmak
    // YANLIŞ olurdu: aynı gövde şube (subeId) ve e-posta da değiştirir.
    // Bu uç DAR'dır: YALNIZ arayuz_modu, YALNIZ jetondaki kişinin satırı.
    // Mod bir TERCİHTİR; hiçbir yasal kapı ona bakmaz (Mehmet K1).

    public async Task<object> KendiArayuzModunuYaz(KendiArayuzModuDto dto, AktifKullanici islem)
    {
        // DataAnnotations yalnız controller yolunda koşar — kontrol burada, elle.
        if (!dto.ArayuzModuGonderildi)
            throw new BadRequestException("arayuzModu alanı gönderilmedi — değiştirilecek bir şey yok");
        if (dto.ArayuzModu is not null && !ArayuzModu.Gecerli(dto.ArayuzModu))
            throw new BadRequestException("Geçersiz arayüz modu: BASIT veya PROFESYONEL olmalıdır");

        // ⚠ Hedef kimlik GÖVDEDEN değil JETONDAN gelir; Find/FindAsync YASAK.
        var kullanici = await TenantKullanicisiBul(islem.Id, islem.TenantId);
        kullanici.ArayuzModu = dto.ArayuzModu;          // null = kiracı varsayılanını DEVRAL
        kullanici.Guncellendi = DateTime.UtcNow;

        denetim.YazmaKaydet(islem, nameof(Data.Kullanici), kullanici.Id,
            $"arayuz-modu: {dto.ArayuzModu ?? "devral"}", hassas: false);
        await db.SaveChangesAsync();

        var kiraciModu = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == islem.TenantId)
            .Select(t => t.VarsayilanArayuzModu)
            .FirstOrDefaultAsync();
        return new
        {
            arayuzModu = kullanici.ArayuzModu,
            etkinArayuzModu = ArayuzModu.Coz(kullanici.ArayuzModu, kiraciModu),
        };
    }

    /// <summary>D1-14 — salt okunur yalnız MUHASEBE rolünde (mali müşavir); başka rolde 400.</summary>
    private static bool SaltOkunurDogrula(string rol, bool saltOkunur)
    {
        if (saltOkunur && rol != "MUHASEBE")
            throw new BadRequestException("Salt okunur hesap yalnız muhasebe rolünde olabilir").AlanIle("saltOkunur");
        return saltOkunur;
    }

    // ─── Rol değiştir ─────────────────────────────────────────────────────────

    public async Task<object> RolDegistir(string id, RolDegistirDto dto, AktifKullanici islem)
    {
        if (id == islem.Id)
            throw new ForbiddenException("Kendi rolünüzü değiştiremezsiniz");

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        await SonSahipKapisi(hedef, dto.Rol, hedef.Aktif);   // B-36 sıra kuralı: ÖNCE
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);
        RolOlusturmayaIzinVarMi(islem.Rol, dto.Rol); // atanacak rol de kontrol edilmeli

        var eskiRol = hedef.Rol;
        hedef.Rol = dto.Rol;
        hedef.SaltOkunur = SaltOkunurDogrula(dto.Rol, dto.SaltOkunur);   // D1-14
        hedef.IskontoSiniriYuzde = dto.IskontoSiniriYuzde;   // D1-05 (boş = sınır yok)
        hedef.PrimYuzde = dto.PrimYuzde; hedef.AylikHedefTl = dto.AylikHedefTl;   // D1-20
        hedef.Guncellendi = DateTime.UtcNow;
        denetim.YazmaKaydet(islem, nameof(Data.Kullanici), hedef.Id,
            $"kullanici-rol-degisti: {eskiRol} → {dto.Rol}{(hedef.SaltOkunur ? " (salt okunur)" : "")}");
        await db.SaveChangesAsync();

        return await Getir(id, islem);
    }

    // ─── Kendi şifresini değiştir ─────────────────────────────────────────────

    public async Task<object> KendinSifreDegistir(SifreDegistirDto dto, AktifKullanici islem)
    {
        if (string.IsNullOrEmpty(dto.MevcutSifre))
            throw new BadRequestException("Mevcut şifre gereklidir");
        YeniSifreTekrarKontrol(dto.YeniSifre, dto.YeniSifreTekrar);

        var kullanici = await TenantKullanicisiBul(islem.Id, islem.TenantId);
        var eslesti = BCrypt.Net.BCrypt.Verify(dto.MevcutSifre, kullanici.SifreHash);
        if (!eslesti) throw new BadRequestException("Mevcut şifre hatalı");

        await SifreGuncelle(kullanici, dto.YeniSifre);
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Şifre başarıyla güncellendi") };
    }

    // ─── Admin şifre sıfırlama ────────────────────────────────────────────────

    public async Task<object> AdminSifreSifirla(string id, SifreDegistirDto dto, AktifKullanici islem)
    {
        if (id == islem.Id)
        {
            // Kendi şifresini admin olarak sıfırlamak — kendi endpoint'ini kullansın
            throw new BadRequestException("Kendi şifrenizi /ben/sifre endpoint'inden değiştiriniz");
        }
        YeniSifreTekrarKontrol(dto.YeniSifre, dto.YeniSifreTekrar);

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);

        await SifreGuncelle(hedef, dto.YeniSifre);
        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Şifre başarıyla sıfırlandı") };
    }

    // ─── Deaktif et ───────────────────────────────────────────────────────────

    public async Task<object> DeaktifEt(string id, AktifKullanici islem)
    {
        if (id == islem.Id)
            throw new ForbiddenException("Kendi hesabınızı deaktif edemezsiniz");

        return await AktiflikDegistir(id, false, islem);
    }

    // ─── Pasife al / geri aktifleştir (P-09 · B-37) ──────────────────────────

    /// <summary>
    /// B-37 — kullanıcı kaydı SİLİNMEZ, pasife alınır ve GERİ alınabilir.
    /// <para>⚠ Hard DELETE yoktur: kullanıcı satırı denetim izinin ve geçmiş
    /// hareketlerin sahibidir; silinirse iz yetim kalır.</para>
    /// </summary>
    public async Task<object> AktiflikDegistir(string id, bool aktif, AktifKullanici islem)
    {
        if (id == islem.Id)
            throw new ForbiddenException("Kendi hesabınızın durumunu değiştiremezsiniz");

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        await SonSahipKapisi(hedef, hedef.Rol, aktif);       // B-36 sıra kuralı: ÖNCE
        HedefUzerindekiYetkiKontrol(islem, hedef.Rol);

        if (hedef.Aktif != aktif)
        {
            if (aktif) await EpostaBaskaIsletmedeAktifOlmasin(hedef.Email, hedef.TenantId);   // 02.11: aktife alınca da tek işletme
            hedef.Aktif = aktif;
            hedef.Guncellendi = DateTime.UtcNow;
            denetim.YazmaKaydet(islem, nameof(Data.Kullanici), hedef.Id,
                aktif ? "kullanici-aktiflestirildi" : "kullanici-pasife-alindi");
            await db.SaveChangesAsync();
        }

        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir(aktif ? "Kullanıcı yeniden aktif edildi" : "Kullanıcı pasife alındı"), aktif };
    }

    // ─── Sahipliği devret (P-09 · B-36) ──────────────────────────────────────

    /// <summary>
    /// Hedefi SAHİP yapar, devredeni aynı SaveChanges içinde düşürür.
    /// Kiracı hiçbir an sahipsiz ya da çift sahipli kalmaz.
    /// </summary>
    public async Task<object> SahiplikDevret(string id, SahiplikDevretDto dto, AktifKullanici islem)
    {
        if (string.IsNullOrWhiteSpace(dto.Sifre))
            throw new BadRequestException("Şifrenizi girmeniz zorunludur");
        if (id == islem.Id)
            throw new BadRequestException("Sahipliği kendinize devredemezsiniz");

        var yeniRolum = string.IsNullOrWhiteSpace(dto.YeniRolum) ? "MUDUR" : dto.YeniRolum!;
        if (yeniRolum == "SAHIP")
            throw new BadRequestException("Devirden sonraki rolünüz SAHİP olamaz");
        if (!Moduller_Roller.Contains(yeniRolum))
            throw new BadRequestException("Devirden sonraki rolünüz MUDUR, MUHASEBE veya TEZGAHTAR olabilir");

        var ben = await TenantKullanicisiBul(islem.Id, islem.TenantId);
        if (ben.Rol != "SAHIP")
            throw new ForbiddenException("Sahipliği yalnız SAHİP devredebilir");
        if (!BCrypt.Net.BCrypt.Verify(dto.Sifre, ben.SifreHash))
            throw new BadRequestException("Şifre hatalı");

        var hedef = await TenantKullanicisiBul(id, islem.TenantId);
        if (hedef.Rol == "SAHIP")
            throw new ConflictException("Bu kullanıcı zaten SAHİP");
        if (!hedef.Aktif)
            throw new BadRequestException("Pasif kullanıcıya sahiplik devredilemez, önce aktifleştirin");

        hedef.Rol = "SAHIP";
        hedef.Guncellendi = DateTime.UtcNow;
        ben.Rol = yeniRolum;
        ben.Guncellendi = DateTime.UtcNow;

        denetim.YazmaKaydet(islem, nameof(Data.Kullanici), hedef.Id,
            $"sahiplik-devredildi: yeni SAHİP {hedef.Email}, devreden {ben.Email} → {yeniRolum}");
        await db.SaveChangesAsync();

        return new { mesaj = Kuyumcu.Core.I18n.SunucuIletiSozlugu.Cevir("Sahiplik devredildi"), yeniSahipId = hedef.Id, yeniRolunuz = yeniRolum };
    }

    // ─── Yardımcı metodlar ───────────────────────────────────────────────────

    internal async Task<Data.Kullanici> TenantKullanicisiBul(string id, string tenantId) =>
        await db.Kullanicilar.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId)
        ?? throw new NotFoundException("Kullanıcı bulunamadı");

    /// <summary>E-posta değişiyorsa tenant içinde benzersizlik doğrular ve atar.</summary>
    private async Task EpostaAyarla(Data.Kullanici hedef, string? yeniEmail, string tenantId)
    {
        if (yeniEmail is null || yeniEmail == hedef.Email) return;
        Kuyumcu.Api.Common.Dogrulama.GirdiKapisi.Denetle(Kuyumcu.Core.Domain.Girdi.EpostaDogrulayici.Denetle(yeniEmail), "email");   // PAKET GOC-D
        var cakisan = await db.Kullanicilar.AnyAsync(k =>
            k.Email == yeniEmail && k.TenantId == tenantId && k.Id != hedef.Id);
        if (cakisan) throw new ConflictException("Bu e-posta adresi zaten kayıtlı");
        if (hedef.Aktif) await EpostaBaskaIsletmedeAktifOlmasin(yeniEmail, tenantId);   // 02.11
        hedef.Email = yeniEmail;
    }

    // ─── 02.11 — kurucu kararı: "aynı e-posta ile kayıt yapılamasın" ─────────────
    // Bir e-posta aynı anda YALNIZ BİR işletmede aktif olabilir. Aksi hâlde giriş ekranı
    // işletme sormak zorunda kalır (kurucu canlı sunucuda yaşadı). Kapılar: kullanıcı
    // ekleme, e-posta değiştirme, pasiften aktife alma (burada) + işletme kaydı (AuthServisi.Kayit).
    // Pasif kayıt engel değildir: ayrılan çalışan başka işletmede çalışabilir.
    // Kiracılar arası okuma SistemKapsami madde 11 ile (IgnoreQueryFilters YOK, yeni bağlam
    // kurulmaz); yalnız VAR/YOK sorulur, başka işletmenin satırı okunmaz.
    public const string EpostaBaskaIsletmedeMesaji = "Bu e-posta başka bir işletmede kullanılıyor; her e-posta tek işletmede olabilir";

    private async Task EpostaBaskaIsletmedeAktifOlmasin(string email, string tenantId)
    {
        if (await SistemKapsami.EpostaBaskaKiracidaAktifMi(db, email, tenantId))
            throw new ConflictException(EpostaBaskaIsletmedeMesaji).AlanIle("email");
    }

    /// <summary>
    /// Üçlü durum (yok / null / değer) TEK YERDE çözülür — Bertan KOŞUL 5.
    /// ⚠ DataAnnotations yalnız controller yolunda çalışır; servis doğrudan
    /// çağrıldığında beyaz liste burada uygulanır (proje değişmezi).
    /// </summary>
    private static void ArayuzModuAyarla(Data.Kullanici hedef, KullaniciGuncelleDto dto)
    {
        if (!dto.ArayuzModuGonderildi) return;              // gövdede YOK → KORUNUR
        if (dto.ArayuzModu is null) { hedef.ArayuzModu = null; return; }   // açık null → DEVRAL
        hedef.ArayuzModu = ModDogrula(dto.ArayuzModu);
    }

    /// <summary>Oluşturma yolunda boş dizge de "devral" sayılır (form boş seçeneği).</summary>
    private static string? OlusturmaModu(string? mod) =>
        string.IsNullOrEmpty(mod) ? null : ModDogrula(mod);

    private static string ModDogrula(string mod) =>
        ArayuzModu.Gecerli(mod) ? mod
            : throw new BadRequestException("Geçersiz arayüz modu: BASIT veya PROFESYONEL olmalıdır");

    /// <summary>Devir hedefi olabilecek roller — SAHİP hariç.</summary>
    private static readonly string[] Moduller_Roller = ["MUDUR", "MUHASEBE", "TEZGAHTAR"];

    /// <summary>
    /// B-36 — TEK SAHİP KİLİT KAPISI. Bir kiracının SON aktif SAHİBİ
    /// ne pasife alınabilir ne de rolü düşürülebilir.
    /// <para>
    /// ⚠ Bu kapı SAHİP.in <c>Rol</c> ya da <c>Aktif</c> alanını değiştiren HER
    /// yazma yolundan çağrılır (<c>TEK_SAHIP_KILIT_YOLU = 0</c> sayacı bunu
    /// denetler). Doğru çıkış yolu kilit değil DEVİR'dir:
    /// <c>POST /kullanicilar/{id}/sahiplik-devret</c>.
    /// </para>
    /// </summary>
    private async Task SonSahipKapisi(Data.Kullanici hedef, string yeniRol, bool yeniAktif)
    {
        if (hedef.Rol != "SAHIP") return;                       // hedef sahip değil
        if (yeniRol == "SAHIP" && yeniAktif) return;            // sahiplik korunuyor
        if (!hedef.Aktif) return;                               // zaten pasif, sayımda yok

        var aktifSahip = await db.Kullanicilar
            .CountAsync(k => k.TenantId == hedef.TenantId && k.Rol == "SAHIP" && k.Aktif);
        if (aktifSahip <= 1)
            throw new ForbiddenException(
                "Bu işletmenin tek SAHİBİ pasife alınamaz ya da rolü düşürülemez. "
                + "Önce 'Sahipliği devret' ile yeni bir SAHİP atayın.");
    }

    private static void HedefUzerindekiYetkiKontrol(AktifKullanici islem, string hedefRol)
    {
        if (AktifKullanici.Seviye(islem.Rol) <= AktifKullanici.Seviye(hedefRol))
            throw new ForbiddenException(
                "Kendi rolünüzden yüksek veya eşit roldeki kullanıcılar üzerinde işlem yapamazsınız");
    }

    private static void RolOlusturmayaIzinVarMi(string islemYapanRol, string hedefRol)
    {
        if (AktifKullanici.Seviye(islemYapanRol) <= AktifKullanici.Seviye(hedefRol))
            throw new ForbiddenException(
                $"{hedefRol} rolü atamak için daha yüksek yetkiye sahip olmanız gerekmektedir");
    }

    private static void YeniSifreTekrarKontrol(string yeni, string tekrar)
    {
        if (yeni != tekrar)
            throw new BadRequestException("Yeni şifre ve tekrarı eşleşmiyor");
    }

    private async Task SifreGuncelle(Data.Kullanici kullanici, string yeniSifre)
    {
        kullanici.SifreHash = BCrypt.Net.BCrypt.HashPassword(yeniSifre, BcryptRounds);
        kullanici.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>sifreHash alanını response'tan düşürür (Nest'teki temizle).</summary>
    /// <param name="mfaEtkin">45-UI liste sütunu; tekil uçlarda null (bilinmiyor).</param>
    private static object Temizle(Data.Kullanici k, bool? mfaEtkin = null) => new
    {
        mfaEtkin,
        id = k.Id,
        tenantId = k.TenantId,
        subeId = k.SubeId,
        ad = k.Ad,
        soyad = k.Soyad,
        email = k.Email,
        rol = k.Rol,
        saltOkunur = k.SaltOkunur,   // D1-14
        iskontoSiniriYuzde = k.IskontoSiniriYuzde,   // D1-05
        primYuzde = k.PrimYuzde, aylikHedefTl = k.AylikHedefTl,   // D1-20
        arayuzModu = k.ArayuzModu,
        aktif = k.Aktif,
        sonGiris = k.SonGiris,
        olusturuldu = k.Olusturuldu,
        guncellendi = k.Guncellendi,
    };
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[Route("api/v1/kullanicilar")]
[Yetki.ModulYetkisi("kullanici")]
public sealed class KullaniciController(KullaniciServisi servis) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>POST /api/v1/kullanicilar — Yeni kullanıcı oluştur — SAHIP, MUDUR (201)</summary>
    [HttpPost]
    [RolGerekli("SAHIP", "MUDUR")]
    public async Task<IActionResult> Olustur([FromBody] KullaniciOlusturDto dto) =>
        StatusCode(StatusCodes.Status201Created, await servis.Olustur(dto, Islem));

    /// <summary>GET /api/v1/kullanicilar — Tenant'taki tüm kullanıcılar — SAHIP, MUDUR</summary>
    [HttpGet]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<List<object>> Listele() => servis.Listele(Islem);

    // PAKET ISLEM-BANT-2: GET /ben de KendiProfilController'a taşındı — sınıf düzeyi [ModulYetkisi("kullanici")]
    // "herkes" diye belgelenen ucu TEZGAHTAR/MUHASEBE için 403 yapıyordu (doğrulayıcı DÜŞÜK).

    // PAKET ISLEM-BANT: PATCH /ben buradan KendiProfilController'a taşındı (sınıf düzeyi
    // [ModulYetkisi("kullanici")] tezgâhtarın kendi modunu değiştirmesini 403 ile kesiyordu).

    /// <summary>PATCH /api/v1/kullanicilar/ben/sifre — Kendi şifresini değiştir — herkes</summary>
    [HttpPatch("ben/sifre")]
    public Task<object> KendinSifreDegistir([FromBody] SifreDegistirDto dto)
        => servis.KendinSifreDegistir(dto, Islem);

    /// <summary>GET /api/v1/kullanicilar/{id} — Kullanıcı detayı — SAHIP, MUDUR</summary>
    [HttpGet("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Getir(string id) => servis.Getir(id, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/{id} — Kullanıcı güncelle — SAHIP, MUDUR</summary>
    [HttpPatch("{id}")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> Guncelle(string id, [FromBody] KullaniciGuncelleDto dto)
        => servis.Guncelle(id, dto, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/{id}/rol — Rol değiştir — SAHIP, MUDUR</summary>
    [HttpPatch("{id}/rol")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> RolDegistir(string id, [FromBody] RolDegistirDto dto)
        => servis.RolDegistir(id, dto, Islem);

    /// <summary>PATCH /api/v1/kullanicilar/{id}/sifre — Admin şifre sıfırlama — SAHIP, MUDUR</summary>
    [HttpPatch("{id}/sifre")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> AdminSifreSifirla(string id, [FromBody] SifreDegistirDto dto)
        => servis.AdminSifreSifirla(id, dto, Islem);

    /// <summary>DELETE /api/v1/kullanicilar/{id} — Kullanıcıyı pasife al (hard DELETE YOK) — SAHIP</summary>
    [HttpDelete("{id}")]
    [RolGerekli("SAHIP")]
    public Task<object> DeaktifEt(string id) => servis.DeaktifEt(id, Islem);

    /// <summary>
    /// PATCH /api/v1/kullanicilar/{id}/aktif — pasife al / geri aktifleştir — SAHIP, MUDUR.
    /// <para>B-37: DELETE'in geri dönüş yolu. Kayıt hiçbir yolda silinmez.</para>
    /// </summary>
    [HttpPatch("{id}/aktif")]
    [RolGerekli("SAHIP", "MUDUR")]
    public Task<object> AktiflikDegistir(string id, [FromBody] AktiflikDto dto)
        => servis.AktiflikDegistir(
            id, dto.Aktif ?? throw new BadRequestException("aktif alanı zorunludur"), Islem);

    /// <summary>
    /// POST /api/v1/kullanicilar/{id}/sahiplik-devret — yeni SAHİP ata ve devret — SAHIP.
    /// <para>B-36: tek sahipli kiracının kilitlenmeden el değiştirmesinin TEK yolu.</para>
    /// </summary>
    [HttpPost("{id}/sahiplik-devret")]
    [RolGerekli("SAHIP")]
    public Task<object> SahiplikDevret(string id, [FromBody] SahiplikDevretDto dto)
        => servis.SahiplikDevret(id, dto, Islem);
}

// ── PAKET ISLEM-BANT — PATCH /kullanicilar/ben (herkes; alan beyaz listesi) ─────

/// <summary>
/// PATCH /api/v1/kullanicilar/ben — kendi profilini güncelle. Sınıf [ModulYetkisi] TAŞIMAZ:
/// "kullanici" modülü olan rol (SAHİP daima) eskisi gibi ad/soyad/e-posta/şube/mod yazar;
/// modülü olmayan rol YALNIZ arayuzModu yazar (başka alan → 403, servis içinde). Hedef jetondan.
/// Üst çubuktaki BASİT/PRO anahtarı (ArayuzModuServisi.DegistirAsync) bu ucu çağırır.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/kullanicilar/ben")]
public sealed class KendiProfilController(KullaniciServisi servis, Yetki.YetkiServisi yetki) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/kullanicilar/ben — kendi profili — HER rol (PAKET ISLEM-BANT-2). Hedef JETONDAN (yol parametresi yok):
    /// başkasının satırına yol yok; yanıt KullaniciController.Getir ile aynı temizlenmiş biçim (parola özeti yok).
    /// </summary>
    [HttpGet]
    [HerkeseAcik("Yalnız jetondaki kişinin kendi satırı (salt okuma); başka kullanıcıya yol yok")]
    public Task<object> BenimProfilim()
    {
        var islem = AktifKullanici.FromClaims(User);
        return servis.Getir(islem.Id, islem);
    }

    [HttpPatch]
    [HerkeseAcik("Kendi satırı (jetondan); 'kullanici' modülü olmayan rol yalnız arayuzModu yazar — diğer alanlar servis içinde 403")]
    public async Task<object> KendinGuncelle([FromBody] KullaniciGuncelleDto dto)
    {
        var islem = AktifKullanici.FromClaims(User);
        var tamYetki = islem.Rol == "SAHIP" || await yetki.ErisebilirMi(islem.TenantId, islem.Rol, "kullanici");
        return await servis.KendinGuncelle(dto, islem, yalnizMod: !tamYetki);
    }
}

// ── PAKET S1-f — dar "kendi modum" ucu ───────────────────────────────────────

/// <summary>
/// Kendi arayüz modu yaması. "Alan yok" ile "alan null" AYRI anlamlıdır:
/// gövdede yoksa 400 (değiştirilecek şey yok), null ise kiracı varsayılanını devral.
/// </summary>
public sealed class KendiArayuzModuDto
{
    private string? _arayuzModu;

    public string? ArayuzModu
    {
        get => _arayuzModu;
        set { _arayuzModu = value; ArayuzModuGonderildi = true; }
    }

    [JsonIgnore] public bool ArayuzModuGonderildi { get; private set; }
}

/// <summary>
/// PATCH /api/v1/kullanicilar/ben/arayuz-modu — HER rol KENDİ modunu yazar.
/// ⚠ Sınıf [ModulYetkisi] TAŞIMAZ (bilinçli): "kullanici" modülü olmayan
///   tezgâhtar da kendi ekran tercihini seçebilmeli (tuval n4). Uç yalnız
///   arayuz_modu yazar ve hedefi jetondan alır — başkasının satırına yol yok.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/kullanicilar/ben/arayuz-modu")]
public sealed class KendiArayuzModuController(KullaniciServisi servis) : ControllerBase
{
    [HttpPatch]
    [HerkeseAcik("Kişisel EKRAN tercihi (basit/profesyonel); yalnız jetondaki kişinin satırı, yalnız arayuz_modu — hiçbir para/MASAK/gün kilidi kapısı bu değere bakmaz")]
    public Task<object> Yaz([FromBody] KendiArayuzModuDto dto)
        => servis.KendiArayuzModunuYaz(dto, AktifKullanici.FromClaims(User));
}
