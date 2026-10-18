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
        s.AddScoped<Auth.MfaServisi>();   // iki aşamalı giriş (paket 45)
        // PAKET AUTH-SIFIRLA — "Parolamı unuttum". Kanal yapılandırmadan seçilir; boş = KAPALI
        // (fail-closed, 503). Gerçek SMTP YOK (ADR-0016); DOSYA yalnız Development/Testing.
        s.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().GetSection("ParolaSifirlama")
            .Get<Auth.ParolaSifirlamaAyarlari>() ?? new Auth.ParolaSifirlamaAyarlari());
        s.AddSingleton<Auth.IParolaSifirlamaPostasi>(sp => Auth.ParolaSifirlamaPostasiFabrikasi.Kur(
            sp.GetRequiredService<Auth.ParolaSifirlamaAyarlari>(),
            sp.GetRequiredService<IHostEnvironment>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("ParolaSifirlama")));
        s.AddSingleton<Auth.ParolaSifirlamaHizSiniri>();
        s.AddScoped<Auth.ParolaSifirlamaServisi>();
        s.AddScoped<Kullanici.KullaniciServisi>();
        s.AddSingleton<Kur.KurKaynakDefteri>();   // kur kaynağı adaptör defteri
        s.AddScoped<Kur.KurServisi>();
        s.AddScoped<Kur.OzelMatrahKapanisServisi>();   // KUR KAYNAKLARI (17.10): has bedeli kapanışları + kaynak ayarları
        s.AddScoped<Cari.CariServisi>();
        s.AddScoped<Cari.CariIbanServisi>();   // M7a
        s.AddScoped<Urun.UrunServisi>();
        s.AddScoped<Urun.UrunKuraliServisi>();   // M4 — ürün kuralları
        s.AddScoped<Satis.SatisServisi>();
        s.AddScoped<Satis.StornoServisi>();   // storno motoru (paket 48 Faz A)
        s.AddScoped<Kasa.KasaServisi>();
        // PAKET P-19 (Y-11/Y-12): gider kategorileri + gider kaydı, ve virmanın
        // iki bacaklı atomik yazıcısı. Storno motoru AYRIDIR (Satis.StornoServisi).
        s.AddScoped<Gider.GiderServisi>();
        s.AddScoped<Gider.VirmanServisi>();
        // Paket 31 — POS alacak defteri (YAN DEFTER; kasa bakiyesine dokunmaz).
        s.AddScoped<Pos.PosServisi>();
        s.AddScoped<Emanet.EmanetServisi>();
        s.AddScoped<Banka.BankaServisi>();
        // Paket 32 Faz A — banka ekstresi içe aktarımı. Servis KENDİ Add'ini
        // YAPMAZ; BankaServisi'ni DI ile alır ve satır başına HareketEkleIc
        // çağırır (K4-OK-2 / denetim.sh "BankaHareketleri.Add" sayacı).
        s.AddScoped<Banka.EkstreServisi>();
        s.AddScoped<Banka.GelenOdemeServisi>();   // M7a
        s.AddScoped<Maliyet.AlisIscilikServisi>();   // M9
        s.AddScoped<Banka.BankaCekimServisi>();      // M7
        s.AddScoped<Banka.BankaSaglayiciServisi>();  // M7
        s.AddScoped<Fatura.FaturaTopluServisi>();    // 18.10 toplu fatura
        s.AddScoped<Satis.BekleyenFisServisi>();     // D1-03 işlem bekletme
        s.AddScoped<Kur.KurKilidiServisi>();         // D1-04 kur kilidi
        s.AddScoped<Satis.SatisOnayServisi>();       // D1-05 satış onayı
        s.AddScoped<Satis.PersonelUyariServisi>();   // D1-05 personel uyarıları
        s.AddScoped<Cari.OdemePlaniServisi>();       // D1-06 ödeme planı
        s.AddScoped<Cari.CariD1Servisi>();           // D1-07 altınla öde · kara liste
        s.AddScoped<Masak.SibHazirlikServisi>();     // D1-18 ŞİB hazırlık
        s.AddScoped<Cari.KvkkServisi>();             // D1-19 KVKK aydınlatma / rıza
        s.AddScoped<Cari.HesapDonusturServisi>();    // D1-09 hesap dönüştürme
        s.AddScoped<Urun.UrunSetiServisi>();         // D1-16 ürün setleri
        s.AddScoped<Fatura.FaturaServisi>();
        s.AddScoped<Rapor.RaporServisi>();
        // ── Paket 34 Faz A — rapor seti (salt okur; deftere YAZMAZ) ─────────
        s.AddScoped<Rapor.SatisRaporServisi>();
        s.AddScoped<Rapor.FinansRaporServisi>();
        // P-25 / Y-16 — R15 stok yaşlandırma + devir hızı.
        s.AddScoped<Rapor.StokYaslandirmaServisi>();
        s.AddScoped<Fiyat.FiyatServisi>();
        s.AddScoped<Sayim.SayimServisi>();
        s.AddScoped<Urun.BelgeServisi>();
        s.AddScoped<Sube.SubeServisi>();
        // PAKET P-15 (Y-29/Y-5/B-25/B-29) — kabuk araması / kayıt seçici kaynağı.
        // Kendi kiracı/şube süzgecini KURMAZ: kapsam P-04 global filtresidir;
        // yetki tür başına YetkiServisi.ErisebilirMi ile sorulur.
        s.AddScoped<Arama.AramaServisi>();
        s.AddScoped<Admin.AdminServisi>();
        s.AddScoped<Yetki.YetkiServisi>();
        // PAKET P-37 — kullanıcı bazlı arayüz tercihleri (menü favorisi/sırası, panel düzeni).
        s.AddScoped<Tercih.TercihServisi>();
        // Paket P-36b — kiracı görünümü (renk şeması + yazı tipi ölçeği).
        s.AddScoped<Gorunum.GorunumServisi>();
        s.AddScoped<Gorunum.KullaniciGorunumServisi>();   // B-1: kullanıcı görünüm tercihi
        // PAKET B-2 — altın pozisyonu (panelin üçüncü kartı; salt okunur, türetilmiş).
        s.AddScoped<Pozisyon.PozisyonServisi>();
        // PAKET P-37 — hava durumu (DIŞ KAYNAK, varsayılan KAPALI; 204 = veri yok).
        s.AddScoped<Hava.HavaServisi>();
        s.AddScoped<Masak.MasakServisi>();   // MASAK uçları (SIRADAKI-ADIM-KARAR §2/A)
        s.AddScoped<Masak.RiskServisi>();   // Paket 40-A — risk skorlama + PEP beyanı
        // Paket T-4a — sürüm/derleme/migration künyesi (salt okur; şema YOK).
        s.AddScoped<Meta.SurumServisi>();
        // PAKET P-31 (R-5 · T-4b) — maskeli tanılama paketi + istemci hata kodu.
        // Kanal kapısı `Destek:PaketKanaliAcik` varsayılan KAPALI (BK-17).
        s.AddScoped<Destek.DestekPaketiServisi>();
        s.AddScoped<Denetim.DenetimOkumaServisi>();   // denetim izi okuma (PLAN-2 Adım 0d)
        s.AddScoped<Disaaktarim.DisaAktarimServisi>();   // veri taşınabilirliği (PLAN-2 Adım 4)
        // Paket 47 Faz A — kapılı içe aktarım. Servis KENDİ Add'ini yapmaz;
        // CariServisi/UrunServisi'ni DI ile alır ve satır başına onları çağırır.
        s.AddScoped<Iceaktarim.IceAktarimServisi>();
        // Paket T-2 Faz A — kuruluş sihirbazı + açılış bakiyeleri. AcilisServisi
        // kendi defter yolunu AÇMAZ: cari bacağını CariServisi'nden, defter
        // bacağını HareketFabrikasi'ndan geçirir (denetim.sh sayacı).
        s.AddScoped<Kurulum.KurulumServisi>();
        s.AddScoped<Kurulum.AcilisServisi>();
        s.AddScoped<Maliyet.MaliyetMotoru>();            // stok maliyet partisi (lot)
        s.AddScoped<Maliyet.KarZararServisi>();          // kâr-zarar, iki bacaklı
        s.AddScoped<Urun.KategoriServisi>();             // çok seviyeli ürün ağacı
        s.AddScoped<Urun.ReyonServisi>();                // vitrin / kasa / depo
        s.AddScoped<Yerlesim.YerlesimServisi>();              // reyon bazlı stok kırılımı (paket 25)
        // Paket 29 Faz A — çek/senet portföyü. Kendi defter yolunu AÇMAZ:
        // deftere HareketFabrikasi, vadeye VadeKapisi, duruma EvrakDurumKapisi.
        s.AddScoped<Evrak.EvrakServisi>();

        // ── Paket 6 Faz A — bildirim + zamanlanmış iş çatısı ────────────────
        s.AddScoped<Isler.BildirimServisi>();
        s.AddScoped<Isler.IsCalistirici>();
        // PAKET P-11 (B-24) — zamanlanmış iş ayarları (kiraci_ayarlari üzerinden).
        s.AddScoped<Isler.IsAyarServisi>();
        // ⚠ DIŞ KANAL TEKELİ: IBildirimKanali uygulaması TAM 1'dir. İkincisi
        //   eklenirse IsCatisiSafTestleri ve denetim.sh KIRMIZI olur (ADR-0016).
        s.AddSingleton<Isler.IBildirimKanali, Isler.UygulamaIciKanal>();
        // İş sınıfları DURUMSUZDUR; kiracı bağlamı çağrıda gelir.
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.VadeHatirlatmaIsi>();
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.MasakSibSuresiIsi>();
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.SaklamaOnizlemeIsi>();
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.SaklamaUygulaIsi>();   // PAKET YASAL-3 (§ 4.3)
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.TevsikKurtarmaIsi>(); s.AddScoped<Tevsik.TevsikIhlalServisi>();   // PAKET YASAL-2 (TB-16)
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.StopajHatirlatmaIsi>(); s.AddScoped<Hurda.StopajServisi>();   // PAKET YASAL-4 (Y4-B)
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.StokEsikIsi>(); s.AddScoped<Hurda.HurdaAnalizServisi>();   // D1-15
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.TaksitHatirlatmaIsi>();   // D1-06
        s.AddSingleton<Isler.IZamanlanmisIs, Isler.Isler.KasaKapanisHatirlatmaIsi>();   // D1-11

        // ── Paket 3 Faz A — sipariş çatısı (deftere YAZMAZ) ────────────────
        s.AddScoped<Siparis.SiparisServisi>();

        // ── PAKET P-26 (Y-34 · Y-35) — tedarik zinciri ────────────────────
        // İkisi de deftere KENDİ yazmaz: mal kabul SatisServisi.Alis'i ÇAĞIRIR
        // (vezne deseninin aynısı), irsaliye hiç defter görmez.
        s.AddScoped<Satinalma.SatinalmaServisi>();
        s.AddScoped<Irsaliye.IrsaliyeServisi>();

        // ── Paket 19 Faz A — vezne modeli (deftere YAZMAZ, SatisFisi'yi ÇAĞIRIR) ──
        s.AddScoped<Vezne.VezneServisi>();

        // ── Paket T-3 Faz A — yedek üstverisi. Servis SÜREÇ BAŞLATMAZ:
        //    yalnız yedek_kayitlari/yedek_talepleri/yedek_tatbikatlari okur-yazar.
        s.AddScoped<Yedek.YedekServisi>();

        // ── PAKET P-17 (Y-6 / Y-7) — ortak A4 çıktı + vitrin panosu ────────
        // İkisi de SALT OKURDUR: deftere yazmaz, yalnız denetim izi düşer.
        // PanoAyarServisi kendi tablosunu AÇMAZ: mevcut kiraci_ayarlari.
        s.AddScoped<Cikti.CiktiServisi>();
        s.AddScoped<Pano.PanoAyarServisi>();

        // ── PAKET P-27 (F-1 + F-3 · R-1) — abonelik ve plan kapısı ─────────
        // Deftere YAZMAZ (para hareketi doğurmaz): yalnız abonelik dönemi ve
        // tenants.plan yansımasını yazar ve her değişimde denetim izi düşer.
        // `tenants.plan` alanına yazan TEK servis budur (YETKISIZ_PLAN_YAZAN_UC).
        s.AddScoped<Abonelik.AbonelikServisi>();
        // Sistem ucu ANONİMDİR: istek kapsamında kiracı bağlamı yoktur, bu yüzden
        // DbContext ENJEKTE EDİLMEZ — kapsam içinden çözülür (SistemServisi kalıbı).
        s.AddScoped<Abonelik.AbonelikSistemServisi>();

        // ── PAKET P-28 (F-4 · R-2) — kiracı etiketli telemetri + destek dakikası ──
        // Toplayıcı SINGLETON'dır (bellek içi birikim, istekler arası yaşar);
        // yazıcı SCOPED'tur (kapsam içinden SistemKapsami ile kiracı gezer).
        // Ölçüm salt sayaçtır: kişisel veri taşımaz (TELEMETRIDE_KISISEL_VERI).
        s.AddSingleton<Telemetri.TelemetriToplayici>();
        s.AddScoped<Telemetri.TelemetriYazici>();
        s.AddScoped<Telemetri.TelemetriServisi>();
        // Sistem ucu ANONİMDİR: DbContext enjekte EDİLMEZ (P-27 kalıbı).
        s.AddScoped<Telemetri.TelemetriSistemServisi>();
        return s;
    }
}
