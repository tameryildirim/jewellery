using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuyumcu.Api.Modules.Admin;

// ═════════════════════════════════════════════════════════════════════════════
// Kaynak: apps/api/src/modules/admin/*
// Tenant kendi bilgilerini görür; planını ve AI modülünü günceller.
// Tüm uç noktalar SAHIP rolü gerektirir.
// ═════════════════════════════════════════════════════════════════════════════

// ── DTO'lar ──────────────────────────────────────────────────────────────────

public sealed class PlanGuncelleDto
{
    /// <summary>
    /// ⚠ PAKET P-27: plan listesi buradan KALDIRILDI. Katalog artık
    /// <c>abonelik_planlari</c> tablosudur; kodda ikinci bir liste tutulsaydı
    /// yeni plan eklendiğinde uç onu sessizce 400 ile reddederdi (UX-S31 —
    /// aynı kavram tek kaynak). Doğrulama servistedir, katalogdan yapılır.
    /// </summary>
    [Required(ErrorMessage = "Plan kodu zorunludur"), MaxLength(20)]
    public string Plan { get; set; } = null!;
}

public sealed class AiToggleDto
{
    [Required] public bool? Etkin { get; set; }
}

/// <summary>
/// PATCH /admin/tenant gövdesi — paket UX-3 Faz A.
/// <para>
/// ⚠ AYRI DTO, TOPLU ATAMA YOK: bu uç kiracının yalnız VARSAYILAN arayüz
/// modunu değiştirir. Plan / AI / ad gibi alanlar buraya EKLENMEZ; her biri
/// kendi ucunda ve kendi doğrulamasında kalır (aşırı gönderim yüzeyi açılmaz).
/// </para>
/// <para>
/// Bu bir TERCİHTİR, yetki değildir: kullanıcıların arayuz_modu satırlarına
/// DOKUNULMAZ (toplu atama yok) — NULL olanlar çözümleyici üzerinden yeni
/// varsayılanı devralır, kendi tercihini yazmış olan kimse etkilenmez.
/// </para>
/// </summary>
public sealed class TenantArayuzModuDto
{
    [Required, RegularExpression("BASIT|PROFESYONEL", ErrorMessage = "Geçersiz arayüz modu")]
    public string VarsayilanArayuzModu { get; set; } = null!;
}


/// <summary>
/// PATCH /admin/tenant/kimlik gövdesi — paket UX-1 Faz B (Mehmet K2).
/// <para>
/// ⚠ AYRI DTO, TOPLU ATAMA YOK: bu uç YALNIZ işletme kimlik bilgilerini taşır.
/// Plan · AiEtkin · Aktif · KurKaynagi · MaliyetYontemi · VarsayilanArayuzModu
/// bu gövdeden ASLA atanmaz; her biri kendi ucunda kalır.
/// </para>
/// <para>
/// ⚠ NEDEN AYRI ROTA: PATCH /admin/tenant ucu paket UX-3 Faz A ile
/// <see cref="TenantArayuzModuDto"/>'ya ([Required] VarsayilanArayuzModu)
/// bağlandı. Kimlik alanlarını oraya koymak o ucun zorunlu alanını
/// gevşetmeyi gerektirirdi (UX-3'ün sözleşmesi bozulurdu), tek gövdede iki
/// ayrı sorumluluk toplanırdı. Bu yüzden kimlik AYRI ROTADADIR.
/// </para>
/// <para>
/// ÜÇ DURUMLU PATCH (UX-S26 · mevcut SubeIdGonderildi deseni, yeni desen
/// AÇILMAZ): alan gövdede YOK → korunur · null → silinir · dolu → yazılır.
/// </para>
/// </summary>
public sealed class TenantKimlikGuncelleDto
{
    private string? _ad;
    private string? _adres;
    private string? _vergiDairesi;
    private string? _vergiNo;
    private string? _telefon;

    [JsonIgnore] public bool AdGonderildi { get; private set; }
    [JsonIgnore] public bool AdresGonderildi { get; private set; }
    [JsonIgnore] public bool VergiDairesiGonderildi { get; private set; }
    [JsonIgnore] public bool VergiNoGonderildi { get; private set; }
    [JsonIgnore] public bool TelefonGonderildi { get; private set; }

    /// <summary>İşletme unvanı — gönderilirse BOŞ OLAMAZ (belgenin satıcı adı).</summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "İşletme adı 2-200 karakter olmalıdır")]
    public string? Ad { get => _ad; set { _ad = value; AdGonderildi = true; } }

    [MaxLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
    public string? Adres { get => _adres; set { _adres = value; AdresGonderildi = true; } }

    [MaxLength(100, ErrorMessage = "Vergi dairesi en fazla 100 karakter olabilir")]
    public string? VergiDairesi
    {
        get => _vergiDairesi;
        set { _vergiDairesi = value; VergiDairesiGonderildi = true; }
    }

    /// <summary>10 hane (vergi no) ya da 11 hane (TCKN). Q10-3: kural TEK kaynaktan (Core VergiKimlik; ASCII [0-9]).</summary>
    [VergiKimlikBicimi]
    public string? VergiNo { get => _vergiNo; set { _vergiNo = value; VergiNoGonderildi = true; } }

    /// <summary>
    /// İşletme telefonu — kural ŞUBE ile AYNI kaynaktan (Kuyumcu.Core Iletisim).
    /// QA turu 5 (YÜKSEK): "abc" 200 ile kabul ediliyordu; şube formunda aynı alan
    /// doğrulanıyordu. İki ekranda iki kural olmaz (UX-S31).
    /// </summary>
    [MaxLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
    [Kuyumcu.Api.Common.Dogrulama.Telefon]   // GIRDI-AILESI-2 tur 3: ekranla TEK kural (Core TelefonDogrulayici)
    public string? Telefon { get => _telefon; set { _telefon = value; TelefonGonderildi = true; } }
}


/// <summary>
/// Q10-3 — VKN/TCKN biçim özniteliği: controller yolunda da servisle AYNI mesaj
/// (harfli girdiye "hane" mesajı dönmesin). Kural Core VergiKimlik'tedir; burada tekrar yazılmaz.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VergiKimlikBicimiAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        Kuyumcu.Core.Domain.VergiKimlik.Mesaj(value as string) is { } mesaj
            ? new ValidationResult(mesaj, validationContext.MemberName is { } ad ? [ad] : null)
            : ValidationResult.Success;
}

// ── Servis ───────────────────────────────────────────────────────────────────

/// <summary>
/// GET/PATCH /admin/tenant yanıtı — AÇIK PROJEKSİYON (paket F-2-min, Bertan koşulu).
/// <para>
/// ⚠ Daha önce burada Tenant VARLIĞI doğrudan dönüyordu: eklenen her yeni kolon
/// (durum, deneme_bitis, davet_id…) istemciye kendiliğinden SIZIYORDU ve
/// kalanGun/saltOkunurMu gibi TÜRETİLMİŞ alanlar hesaplanamıyordu. Alan adları
/// DEĞİŞMEDİ — yalnız yeni alanlar eklendi.
/// </para>
/// </summary>
/// <param name="KalanGun">Deneme bitişine kalan gün (yukarı yuvarlanır). NULL = süre kısıtı yok.</param>
/// <param name="SaltOkunurMu">Sunucunun SON SÖZÜ: bu kiracı şu an yazabiliyor mu?</param>
public sealed record TenantDto(
    string Id, string Ad, string Plan, bool AiEtkin, bool Aktif,
    string VarsayilanArayuzModu,
    string? Adres, string? VergiDairesi, string? VergiNo, string? Telefon,
    string? KurKaynagi, string? MaliyetYontemi,
    string Durum, DateTime? DenemeBitis, int? KalanGun, bool SaltOkunurMu,
    DateTime Olusturuldu, DateTime Guncellendi);

/// <summary>PAKET P-13 (B-47) — PATCH /admin/tenant/maliyet-yontemi gövdesi.</summary>
public sealed class MaliyetYontemiDto
{
    [Required(ErrorMessage = "Maliyet yöntemi zorunludur"), MaxLength(20)]
    public string? Yontem { get; set; }
}

public sealed class AdminServisi(KuyumcuDbContext db, DenetimServisi? denetim = null)
{
    /// <summary>Tenant kendi bilgilerini görür + planını günceller</summary>
    public async Task<TenantDto> TenantGetir(AktifKullanici islem)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");
        return Yansit(tenant);
    }

    /// <summary>Varlık → DTO. Yeni kolon eklenince buraya yazılmadıkça SIZMAZ.</summary>
    public static TenantDto Yansit(Tenant t)
    {
        var now = DateTime.UtcNow;
        var ham = t.Durum ?? "";
        var durum = new Modules.Sistem.KiraciDurumu(ham, t.DenemeBitis);
        return new TenantDto(
            t.Id, t.Ad, t.Plan, t.AiEtkin, t.Aktif, t.VarsayilanArayuzModu,
            t.Adres, t.VergiDairesi, t.VergiNo, t.Telefon, t.KurKaynagi, t.MaliyetYontemi,
            ham, t.DenemeBitis,
            Modules.Sistem.SistemServisi.KalanGun(t.DenemeBitis, now),
            !durum.YazabilirMi(now),
            t.Olusturuldu, t.Guncellendi);
    }

    /// <summary>
    /// Plan kodları — PAKET P-09 · K3 diliminden kalan TARİHSEL sabit.
    /// <para>
    /// ⚠ P-27'DEN İTİBAREN TEK KAYNAK DEĞİLDİR: geçerli planlar artık
    /// <c>abonelik_planlari</c> tablosundadır ve doğrulamayı
    /// <see cref="Modules.Abonelik.AbonelikServisi"/> katalogdan yapar. Bu dizi
    /// YALNIZ migration tohumunun ve eski testlerin beklediği üç kodu belgeler;
    /// yeni kod buna BAKMAZ (ikinci bir plan listesi = ikinci gerçek).
    /// </para>
    /// </summary>
    public static readonly string[] GecerliPlanlar = ["BASLANGIC", "PRO", "KURUMSAL"];

    // ⚠ PAKET P-27 (F-3 PLAN KAPISI): `PlanGuncelle` BU SERVİSTEN KALDIRILDI.
    //   Gerekçe — `tenants.plan` alanına yazan İKİ yol olamaz: kim değiştirdi,
    //   ne zaman başladı, ne zaman bitiyor sorularının cevabı
    //   `kiraci_abonelikleri` tablosundadır ve oraya yazan tek çekirdek
    //   AbonelikServisi.DonemDegistir'dir. PATCH /api/v1/admin/plan ROTASI
    //   DURUYOR (istemci sözleşmesi kırılmadı) ama gövdesi AbonelikServisi'ne
    //   devreder ve yükseltmeyi 403 ile reddeder.
    //   Sayaç: dotnet/denetim.sh → YETKISIZ_PLAN_YAZAN_UC = 0.

    public async Task<TenantDto> AiToggle(bool etkin, AktifKullanici islem)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        tenant.AiEtkin = etkin;
        tenant.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await TenantGetir(islem);
    }

    /// <summary>
    /// VEZNE AYRIMI (paket 19 Faz A) — kiraci_ayarlari üzerinden okunur/yazılır.
    /// <para>
    /// ⚠ tenants tablosuna KOLON EKLENMEDİ (Bertan koşulu): Tenant IKiraciVarlik
    /// değildir, global filtre ve yazma denetçisi ona işlemez. Ayar IKiraciVarlik
    /// olan ayrı tabloda durur ve denetim izine kendiliğinden düşer.
    /// </para>
    /// </summary>
    public async Task<object> VezneAyarGetir(AktifKullanici islem) => new
    {
        zorunlu = await Vezne.VezneAyari.ZorunluMu(db, islem.TenantId),
    };

    /// <summary>Vezne ayrımını açar/kapatır (SAHIP). Varsayılan KAPALI.</summary>
    public async Task<object> VezneToggle(bool zorunlu, AktifKullanici islem)
    {
        var now = DateTime.UtcNow;
        var kayit = await db.KiraciAyarlari.FirstOrDefaultAsync(
            a => a.TenantId == islem.TenantId && a.Anahtar == KiraciAyarAnahtari.VezneZorunlu);

        if (kayit is null)
            db.KiraciAyarlari.Add(new KiraciAyari
            {
                Id = Kimlik.Ulid(),
                TenantId = islem.TenantId,
                Anahtar = KiraciAyarAnahtari.VezneZorunlu,
                Deger = zorunlu ? "true" : "false",
                Guncellendi = now,
            });
        else
        {
            kayit.Deger = zorunlu ? "true" : "false";
            kayit.Guncellendi = now;
        }

        denetim?.YazmaKaydet(islem, "KiraciAyari", KiraciAyarAnahtari.VezneZorunlu,
            $"vezne ayrımı {(zorunlu ? "AÇILDI" : "KAPATILDI")}");
        await db.SaveChangesAsync();
        return new { zorunlu };
    }

    /// <summary>Kiracının varsayılan arayüz modunu değiştirir (UX-3 Faz A).</summary>
    public async Task<TenantDto> VarsayilanArayuzModuGuncelle(string mod, AktifKullanici islem)
    {
        // DataAnnotations yalnız controller yolunda koşar — beyaz liste burada da.
        if (!Modules.Kullanici.ArayuzModu.Gecerli(mod))
            throw new BadRequestException("Geçersiz arayüz modu: BASIT veya PROFESYONEL olmalıdır");

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        tenant.VarsayilanArayuzModu = mod;
        tenant.Guncellendi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await TenantGetir(islem);
    }

    /// <summary>
    /// PAKET P-13 (B-47) — kiracının MALİYET YÖNTEMİ ayarı.
    /// <para>
    /// Eskiden yalnız OKUNUYORDU: <c>MaliyetMotoru.Yontem()</c> kolonu okuyor,
    /// hiçbir uç yazmıyordu. Ekranda da salt okunur satırdı ve "türetilir"
    /// gerekçesi yazıyordu — oysa türetilmiyor, hiç sorulmuyordu. Kolon boşken
    /// motor FIFO'ya düşer; bu bir VARSAYILAN'dır, bir KARAR değildir.
    /// </para>
    /// <para>⚠ Geçmişe dönük etki YOKTUR: yöntem, bundan sonraki maliyet
    /// düşümlerinde uygulanır; kapanmış partiler yeniden hesaplanmaz.</para>
    /// </summary>
    public async Task<TenantDto> MaliyetYontemiGuncelle(string? yontem, AktifKullanici islem)
    {
        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        if (!Data.MaliyetYontemi.Gecerli(yontem))
            throw new BadRequestException(
                $"Geçersiz maliyet yöntemi: '{yontem}'. Seçilebilir: {string.Join(", ", Data.MaliyetYontemi.Hepsi)}");

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        var eski = tenant.MaliyetYontemi;
        if (eski != yontem)
        {
            tenant.MaliyetYontemi = yontem;
            tenant.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, nameof(Data.Tenant), tenant.Id,
                $"maliyet-yontemi-degisti: {eski ?? "(belirlenmemiş)"} → {yontem}");
            await db.SaveChangesAsync();
        }
        return await TenantGetir(islem);
    }

    /// <summary>
    /// İşletme kimlik bilgilerini günceller (UX-1 Faz B).
    /// <para>
    /// Sözleşme: gövdede OLMAYAN alan KORUNUR, açıkça null gönderilen alan
    /// SİLİNİR. Boş/boşluk dizgesi de silme sayılır (kullanıcı alanı temizler).
    /// </para>
    /// <para>
    /// ⚠ Mehmet K3: vergi no / işletme adı belgenin SATICI kimliğidir; değişimi
    /// denetim izine aynı transaction içinde yazılır. Detaya DEĞER değil, hangi
    /// ALANLARIN değiştiği girer (DenetimIzi.cs kişisel veri uyarısı).
    /// </para>
    /// </summary>
    public async Task<TenantDto> KimlikGuncelle(TenantKimlikGuncelleDto dto, AktifKullanici islem)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == islem.TenantId)
            ?? throw new NotFoundException("Tenant bulunamadı");

        // DataAnnotations YALNIZ controller yolunda koşar → servis içi elle kontrol.
        if (dto.AdGonderildi && string.IsNullOrWhiteSpace(dto.Ad))
            throw new BadRequestException("İşletme adı boş bırakılamaz");
        // Q10-3 (doğrulayıcı bulgusu): controller'daki StringLength KIRPILMAMIŞ metni
        // ölçer → " x " (3 karakter) geçip "x" olarak yazılıyordu. Sınır KIRPILMIŞ
        // uzunlukta, tek kaynaktan (Core IsletmeAdi) — istemci de aynısını çağırır.
        if (dto.AdGonderildi && !Kuyumcu.Core.Domain.IsletmeAdi.Gecerli(dto.Ad))
            throw new BadRequestException(Kuyumcu.Core.Domain.IsletmeAdi.Mesaj);
        // Q10-3: biçim kuralı TEK kaynaktan (Core VergiKimlik). Eski "\d" Unicode rakamı da
        // eşliyordu; sağlama (kontrol hanesi) UYGULANMAZ — birincil kaynak yok (MEVZUAT KURALI).
        if (dto.VergiNoGonderildi && Kuyumcu.Core.Domain.VergiKimlik.Mesaj(dto.VergiNo) is { } vergiNoHatasi)
            throw new BadRequestException(vergiNoHatasi).AlanIle("vergiNo");   // TCKN-DOGRULAMA: alan altı
        // QA turu 5 (YÜKSEK): telefon hiç doğrulanmıyordu, "abc" 200 ile kaydediliyordu.
        // Kural ŞUBE ile TEK kaynaktan (Iletisim.TelefonGecerli — desen + hane sayısı).
        // GIRDI-AILESI-2 tur 3: ekranın Telefon bileşeniyle TEK kural (Core TelefonDogrulayici) ve AYNI metin.
        if (dto.TelefonGonderildi && Kuyumcu.Core.Domain.Girdi.TelefonDogrulayici.Denetle(dto.Telefon) is { } telefonHatasi)
            throw new BadRequestException(Kuyumcu.Core.I18n.GirdiSozlugu.Mesaj(telefonHatasi, Kuyumcu.Core.I18n.IstekDili.Gecerli)).AlanIle("telefon");

        var degisen = new List<string>();
        // Q10-3: denetim izi ESKİ → YENİ taşır. DenetimIzi.Detay "DEĞERLER YAZILMAZ"
        // kuralıyla uzlaşma: kimlik belirten alanlar (vergi_no, telefon) MASKELİ
        // (son 4 hane), adres KISALTILMIŞ; tam değer hiçbir koşulda detaya düşmez.
        var farklar = new List<string>();

        if (dto.AdGonderildi && dto.Ad!.Trim() != tenant.Ad)
        {
            farklar.Add($"ad: {DenetimDegeri("ad", tenant.Ad)} → {DenetimDegeri("ad", dto.Ad.Trim())}");
            tenant.Ad = dto.Ad.Trim();
            degisen.Add("ad");
        }

        Uygula(dto.AdresGonderildi, dto.Adres, tenant.Adres, v => tenant.Adres = v, "adres");
        Uygula(dto.VergiDairesiGonderildi, dto.VergiDairesi, tenant.VergiDairesi,
            v => tenant.VergiDairesi = v, "vergi_dairesi");
        Uygula(dto.VergiNoGonderildi, dto.VergiNo, tenant.VergiNo, v => tenant.VergiNo = v, "vergi_no");
        Uygula(dto.TelefonGonderildi, dto.Telefon, tenant.Telefon, v => tenant.Telefon = v, "telefon");

        if (degisen.Count > 0)
        {
            tenant.Guncellendi = DateTime.UtcNow;
            denetim?.YazmaKaydet(islem, "TenantKimlik", tenant.Id,
                $"işletme kimliği güncellendi · değişen alanlar: {string.Join(", ", degisen)}"
                + $" · eski → yeni: {string.Join("; ", farklar)}");
            await db.SaveChangesAsync();
        }

        return await TenantGetir(islem);

        void Uygula(bool gonderildi, string? yeni, string? eski, Action<string?> yaz, string alanAdi)
        {
            if (!gonderildi) return;                       // gövdede yok → KORUNUR
            var deger = string.IsNullOrWhiteSpace(yeni) ? null : yeni.Trim();  // null/boş → SİLİNİR
            if (deger == eski) return;
            farklar.Add($"{alanAdi}: {DenetimDegeri(alanAdi, eski)} → {DenetimDegeri(alanAdi, deger)}");
            yaz(deger);
            degisen.Add(alanAdi);
        }
    }

    /// <summary>
    /// Q10-3 — denetim detayına yazılacak GÖSTERİM değeri. vergi_no/telefon son 4
    /// hane dışında maskelenir (MASAK Maskele ile aynı kalıp), adres ilk 12 karakter,
    /// ad/vergi dairesi 60 karakterle sınırlanır (Detay kolonu 1000 karakter).
    /// </summary>
    public static string DenetimDegeri(string alanAdi, string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger)) return "(boş)";
        return alanAdi switch
        {
            "vergi_no" or "telefon" => Modules.Masak.MasakServisi.Maskele(deger),
            "adres" => deger.Length <= 12 ? $"\"{deger}\"" : $"\"{deger[..12]}…\"",
            _ => deger.Length <= 60 ? $"\"{deger}\"" : $"\"{deger[..60]}…\"",
        };
    }
}

// ── Controller ───────────────────────────────────────────────────────────────

[ApiController]
[Authorize]
[RolGerekli("SAHIP")]
[Route("api/v1/admin")]
public sealed class AdminController(
    AdminServisi servis, Modules.Abonelik.AbonelikServisi abonelik) : ControllerBase
{
    private AktifKullanici Islem => AktifKullanici.FromClaims(User);

    /// <summary>GET /api/v1/admin/tenant — Tenant bilgisi + plan</summary>
    [HttpGet("tenant")]
    public Task<TenantDto> TenantGetir() => servis.TenantGetir(Islem);

    /// <summary>PATCH /api/v1/admin/tenant — Kiracı varsayılan arayüz modu — SAHIP</summary>
    /// <remarks>Rol kapısı SINIF DÜZEYİNDEDİR ([RolGerekli("SAHIP")], :71).</remarks>
    [HttpPatch("tenant")]
    public Task<TenantDto> TenantGuncelle([FromBody] TenantArayuzModuDto dto)
        => servis.VarsayilanArayuzModuGuncelle(dto.VarsayilanArayuzModu, Islem);


    /// <summary>
    /// PATCH /api/v1/admin/tenant/kimlik — İşletme kimlik bilgileri — SAHIP.
    /// <para>Rol kapısı SINIF düzeyindedir ([RolGerekli("SAHIP")]).</para>
    /// </summary>
    [HttpPatch("tenant/kimlik")]
    public Task<TenantDto> TenantKimlikGuncelle([FromBody] TenantKimlikGuncelleDto dto)
        => servis.KimlikGuncelle(dto, Islem);

    /// <summary>
    /// PATCH /api/v1/admin/tenant/maliyet-yontemi — FIFO · LIFO · ORTALAMA — SAHIP
    /// <para>Rol kapısı SINIF düzeyindedir ([RolGerekli("SAHIP")]).</para>
    /// </summary>
    [HttpPatch("tenant/maliyet-yontemi")]
    public Task<TenantDto> MaliyetYontemiGuncelle([FromBody] MaliyetYontemiDto dto)
        => servis.MaliyetYontemiGuncelle(dto.Yontem, Islem);

    /// <summary>
    /// PATCH /api/v1/admin/plan — plan İNDİRME (PAKET P-27 · F-3 plan kapısı).
    /// <para>
    /// ⚠ DAVRANIŞ DEĞİŞTİ: yükseltme artık 403'tür — abonelik hakkını
    /// genişletmek yalnız sistem anahtarıyla korunan operatör ucundan olur
    /// (POST /api/v1/sistem/kiracilar/{id}/abonelik). Reddedilen girişim
    /// denetim izine REDDEDILDI eylemiyle yazılır.
    /// </para>
    /// <para>Rota KORUNDU (istemci sözleşmesi kırılmadı); gövde tek yazma
    /// çekirdeğine (AbonelikServisi) devreder.</para>
    /// </summary>
    [HttpPatch("plan")]
    public async Task<TenantDto> PlanGuncelle([FromBody] PlanGuncelleDto dto)
    {
        await abonelik.KiraciPlanDegistir(dto.Plan, Islem);
        // Yanıt ŞEKLİ değişmedi: ekran TenantVeri bekliyor (UX-1-C4 sözleşmesi).
        return await servis.TenantGetir(Islem);
    }

    /// <summary>PATCH /api/v1/admin/ai — AI modülünü aç/kapat</summary>
    [HttpPatch("ai")]
    public Task<TenantDto> AiToggle([FromBody] AiToggleDto dto)
        => servis.AiToggle(dto.Etkin!.Value, Islem);

    /// <summary>GET /api/v1/admin/vezne — Vezne ayrımı açık mı — SAHIP (sınıf düzeyi kapı)</summary>
    [HttpGet("vezne")]
    public Task<object> VezneAyarGetir() => servis.VezneAyarGetir(Islem);

    /// <summary>PATCH /api/v1/admin/vezne — Vezne ayrımını aç/kapat — SAHIP</summary>
    [HttpPatch("vezne")]
    public Task<object> VezneToggle([FromBody] Vezne.VezneAyarDto dto)
        => servis.VezneToggle(dto.Zorunlu ?? throw new BadRequestException("zorunlu alanı gerekli"), Islem);
}
