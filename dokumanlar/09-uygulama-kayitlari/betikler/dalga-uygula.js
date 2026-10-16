export const meta = {
  name: 'kuyumcu-dalga-uygula',
  description: 'Bir uygulama dalgasinin paketlerini SERI olarak Opus uygulayici + hasim dogrulayici + duzeltme donguleriyle uygular; kapi gecmeyen paket dalgayi durdurur',
  phases: [
    { title: 'Uygulama', detail: 'Opus uygulayici: spesifikasyon + kosullar + UX sozlesmesi; test, mutasyon, denetim, canli', model: 'opus' },
    { title: 'Dogrulama', detail: 'Hasim dogrulayici (farkli model): build/test/denetim/git/DB kanitlarini yeniden uretir' },
  ],
}

const KOK = '/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery'
const SCRATCH = '/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad'
const RAPOR_DIZINI = `${KOK}/dokumanlar/09-uygulama-kayitlari`
const paketler = args.paketler
const dalga = args.dalga
const anaBelge = args.ana_belge || `${KOK}/dokumanlar/02-mimari/MIMARI-KALAN-PAKETLER-2026-09-15.md`
const uxSozlesme = args.ux_sozlesme || ''

const KURALLAR = `
BAĞLAYICI KURALLAR (ihlal = başarısızlık):
- Proje kökü ${KOK}; derleme/test için ${KOK}/dotnet. Dil Türkçe. Anayasa md.1: başarısız komut gizlenmez, gerçek çıktısıyla raporlanır; 'bitti' ancak kanıtla; bilinmeyen tahmin edilmez.
- COMMIT YOK (git add/commit/push yasak). git checkout/stash/reset/rm/mv/find -delete ÇALIŞTIRMA (hook bloklar, .git bozulabilir). Geçici dosyalar yalnız ${SCRATCH} altında; proje ağacına .mutorj/.bak/.tmp bırakma; boşaltman gereken dosyayı 'printf "" > dosya' ile boşalt.
- Sır/parola/bağlantı dizesi/token değerini ASLA çıktına yazma. MySQL parolası: 'P=$(sed -n "s/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p" ${KOK}/docker-compose.yml | head -1 | tr -d "\\r")' ile değişkene al (⚠ ESKİ REÇETE 'grep … | head -1 | awk' YASAK: docker-compose.yml'in 3. SATIRI bir YORUMDUR ve içinde parola geçer; eski reçete o satırı seçip parolayı çıktıya düşürüyordu — 2026-09-21/22'de üç ajanda gerçekleşti); komutlarda $P kullan; echo etme. DB erişimi: 'docker exec kuyumcu-mysql mysql -uroot -p"$P" -N kuyumcu_net -e "..."' (yerel mysql istemcisi yok).
- 5199 PORTUNA DOKUNMA (kurucunun inceleme sunucusu). Kendi canlı doğrulaman için: 'cd ${KOK}/dotnet && ASPNETCORE_ENVIRONMENT=Development nohup dotnet run --project Kuyumcu.Api --no-build --no-launch-profile --urls http://localhost:5198 > ${SCRATCH}/api-5198.log 2>&1 &' → PID'i sakla → iş bitince yalnız o PID'i 'kill' ile durdur ('lsof -nP -iTCP:5198 -sTCP:LISTEN' boş olmalı).
- ADIM EKONOMİSİ: her araç çağrısını KISA tut (3 dakikadan uzun süren tek bir düşünme/komut adımı 'durdu' sayılır ve iş İPTAL olur): büyük dosyaları bütünüyle cat'leme, grep/sed -n aralıklarıyla oku; testleri filtreli koş, tam süiti yalnız sonda; uzun komutlara 'timeout' ver; API'yi nohup ile arka planda başlat.
- ÇIKTI EKONOMİSİ: 'dotnet build' ve 'dotnet test' çıktısını dosyaya yaz ve yalnız süz ('grep -E "error|Hata|Uyarı|Başarısız|Passed!|Failed!|Toplam|Geçti|Kaldı"'); tam çıktıyı bağlama okuma.
- MİGRATION PROSEDÜRÜ (yalnız spesifikasyon şema değişikliği istiyorsa): (1) 'dotnet build Kuyumcu.sln' YEŞİL olmadan 'dotnet ef' ÇALIŞTIRMA; '--no-build' ASLA. (2) 'cd ${KOK}/dotnet && dotnet ef migrations add <YYYYMMDDHHMMSS'siz Ad> --project Kuyumcu.Api' (build'li). (3) Üretilen .cs ve .Designer.cs dosyalarının 0 bayttan büyük ve Up() gövdesinin beklenen değişikliği içerdiğini OKU. (4) 'dotnet ef database update --project Kuyumcu.Api --connection "server=localhost;port=3306;database=kuyumcu_net;user=root;password=$P"' (parola değişkenden). (5) kuyumcu_test'i testler kendisi kurar (süreç başına EnsureDeleted+Migrate). Yeni her varlık IKiraciVarlik + (tenant_id, …) indeks; HasQueryFilter kapsama denetimi uygulamayı açmaz sa bunu düzelt.
- DEĞİŞMEZLER: deftere yazan tek yol HareketFabrikasi.Ekle (Hareketler.Add yasak); IgnoreQueryFilters/Find/FindAsync yasak; ExecuteUpdate/Delete beyaz listesi 4; [RolGerekli] TAM eşleşme; DTO DataAnnotations yalnız controller yolunda → servis içi elle kontrol; PATCH sözleşmesi 'gövdede yoksa koru, null ise sil' + testi; Sozlukler.cs TR+EN (çıplak metin yok); her yeni form için düzenleme yolu (UX-S1); mevcut element id'leri DEĞİŞMEZ; yeni id'ler '<önek>-<alan>'; Razor öznitelikte '{ }' lambda yok; 'disabled="false"' dizgedir → '@(false)'; bUnit sahte API '{}' varsayılanı → yeni uçlara gerçekçi yanıt; SVG/CSV sayıları InvariantCulture; Türkçe JSON gövdesi ayrıştırılarak sınanır; yeni CSS'te sabit renk yok, belirteç var(); satır içi style yok; boş durum + hata/başarı bandı zorunlu; ateşle-unut yan kanallar beklenmez.
- MUTASYON YÖNTEMİ (mut.sh projede yok): kaynağı ${SCRATCH}/mut-yedek/<paket>/ altına kopyala → 'perl -0777 -pi -e' ile TEK eşleşmeli değişiklik → 'dotnet test --filter <ilgili> -v n' → yedeği geri kopyala → 'cmp' ile birebir aynı olduğunu KANITLA. Hiçbir testi kırmayan mutasyon = TEST AÇIĞI: testi düzelt, mutasyonu tekrarla. Son mutasyondan sonra TEMİZ 'dotnet build'.
- Kabuk zsh: 'for t in \${=LISTE}'. API yanıtları camelCase; boş kimlikle devam etme; yeni kiracıda /kur/guncel ilk 10-20 sn boş dönebilir → bekle. Test kiracısı adı 'PAKET<no>-TEST' ile başlasın ve iş sonunda tüm tablolardan silinsin (information_schema'dan tenant_id kolonu olan tablolar), tenants sayısı başlangıçtaki değere dönsün.
- SÖZLEŞME-DoD (Bertan koşulu, 2026-09-16 12:12 mührü, dalga 2b'den itibaren her pakette bağlayıcı): her YENİ varlık/kayıt türü PATCH (gönderilmeyen alan = koru, null = sil) + DELETE ya da pasife alma ucu + [ModulYetkisi]/[RolGerekli] + denetim kaydı (DenetimServisi.YazmaKaydet ÇAĞRILIR, yeniden yazılmaz) + ekranda Düzenle/Pasife al eylemi ya da DegismezIpucu gerekçesi ile DOĞAR. Finansal varlıklarda (hareketler, kasa_hareketleri, banka_hareketleri, faturalar) hard DELETE YASAK — yalnız pasifleştirme/storno. Raporda "DoD sözleşmesi" bölümü zorunlu: hangi varlık hangi uçla düzenlenebilir, hangisi neden salt okunur. Eksikse hasım doğrulayıcı paketi GEÇİRMEZ.

- DoD EK (kurul tur 1, 2026-09-16 13:35, bağlayıcı): (1) HER DÜĞME ya gerçek istek atar ya da disabled + DegismezIpucu/açıklama taşır — "tıklanınca hiçbir şey olmayan" düğme = paket geçmez (bUnit: her button için tıklama → istek ya da disabled); (2) TEK DÜZENLEME KALIBI: çok alanlı düzenleme Cekmece.razor (2c sonrası), tek alanlı SatirIciDuzenle; ModalPencere yalnız yıkıcı onay; ikinci "duzenlenenId" kalıbı açılmaz — dotnet/denetim.sh sayaçları (Pages/ altında yeni ModalPencere düzenleme kullanımı = 0 artış); (3) her form: boş/geçersiz gönderimde alan altı Türkçe mesaj, sessiz etkisizlik YASAK; (4) yıkıcı eylem (pasife al, iptal, kes, storno) ortak onay modalı + geri dönüş yolu (aktife al).

- DoD EK 2 (mühür 2, 13:38): TEK DOKUNUŞ — bir razor sayfası bu dalgada açıldıysa raporun "Dokunulan sayfalar" listesine yazılır; aynı sayfa başka bir dalgada tekrar açılırsa kapı kaydına yazılı gerekçe zorunlu (gerekçesiz ikinci dokunuş = engel). Storno taklidi ("ters iade") YASAK; storno yalnız 48-A motoruyla. Ürün, dalga 2b+2c ve QA turu 2 bitene dek yalnız ÜCRETSİZ PİLOT: fiyat/teklif/tahsilat içeren hiçbir metin üretilmez.

- Belge güncelleme: ${anaBelge} ve paket spesifikasyonuna DOKUNMA; raporunu ${RAPOR_DIZINI}/PAKET-<no>-uygulama-raporu.md dosyasına yaz (dizini gerekirse oluştur). SONRAKI-OTURUM-PROMPT.md'ye dokunma (orkestratör yazacak).
`

const UYGULAMA = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    durum: { type: 'string', enum: ['BITTI', 'YARIM', 'BASARISIZ'], description: 'BITTI = paketin TAMAMI yapıldı ve DoD karşılandı · YARIM = bir kısmı yapılmadı '+ '(neyin yapılmadığını ozet içinde tek tek say) · BASARISIZ = yapılamadı. ⚠ BU BEYAN BAĞLAYICIDIR: BITTI demezsen doğrulayıcı GECTI dese bile paket GEÇMEZ.' },
    ozet: { type: 'string' },
    derleme: { type: 'string' },
    migration: { type: 'string', description: 'Adı ve kuyumcu_net uygulama kanıtı; yoksa "gerekmedi"' },
    test_kosumlari: { type: 'array', items: { type: 'object', properties: { kapsam: { type: 'string' }, toplam: { type: 'integer' }, basarili: { type: 'integer' }, basarisiz: { type: 'integer' }, atlanan: { type: 'integer' }, kirmizilar: { type: 'array', items: { type: 'string' } } }, required: ['kapsam', 'toplam', 'basarili', 'basarisiz'] } },
    yeni_test_sayisi: { type: 'integer' },
    mutasyonlar: { type: 'array', items: { type: 'object', properties: { kod: { type: 'string' }, degisiklik: { type: 'string' }, kirmizi_testler: { type: 'array', items: { type: 'string' } }, geri_alindi_cmp: { type: 'string' } }, required: ['kod', 'degisiklik', 'kirmizi_testler', 'geri_alindi_cmp'] } },
    denetim_sh: { type: 'string' },
    canli: { type: 'array', items: { type: 'object', properties: { adim: { type: 'string' }, beklenen: { type: 'string' }, gercek: { type: 'string' }, kanit: { type: 'string' } }, required: ['adim', 'beklenen', 'gercek'] } },
    kiraci_sayisi_son: { type: 'integer' },
    degisen_dosyalar: { type: 'array', items: { type: 'string' } },
    kosul_karsilama: { type: 'array', items: { type: 'object', properties: { kosul: { type: 'string' }, durum: { type: 'string' }, kanit: { type: 'string' } }, required: ['kosul', 'durum'] } },
    acik_kalanlar: { type: 'array', items: { type: 'string' } },
    rapor_dosyasi: { type: 'string' },
  },
  required: ['paket_no', 'durum', 'ozet', 'derleme', 'migration', 'test_kosumlari', 'yeni_test_sayisi', 'mutasyonlar', 'denetim_sh', 'canli', 'kiraci_sayisi_son', 'degisen_dosyalar', 'kosul_karsilama', 'acik_kalanlar', 'rapor_dosyasi'],
}
const DOGRULAMA = {
  type: 'object',
  properties: {
    paket_no: { type: 'string' },
    karar: { type: 'string', description: 'GECTI | KALDI' },
    kontroller: { type: 'array', items: { type: 'object', properties: { kontrol: { type: 'string' }, sonuc: { type: 'string', description: 'OK | HATA | YAPILAMADI' }, kanit: { type: 'string' } }, required: ['kontrol', 'sonuc', 'kanit'] } },
    bulgular: { type: 'array', items: { type: 'object', properties: { baslik: { type: 'string' }, ciddiyet: { type: 'string', description: 'KRITIK | ORTA | DUSUK' }, kanit: { type: 'string' }, oneri: { type: 'string' } }, required: ['baslik', 'ciddiyet', 'kanit'] } },
    test_toplam: { type: 'integer' },
  },
  required: ['paket_no', 'karar', 'kontroller', 'bulgular', 'test_toplam'],
}

const uygulamaPrompt = (p, ekBulgular) => `${KURALLAR}
GÖREV — PAKET ${p.no} "${p.ad}" UYGULAMASI (Taşkın backend + Ertuğ frontend rolü; Opus). Dalga ${dalga}.
ÖNCE OKU (sırayla): (1) ${anaBelge} — özellikle 'Kesişen kararlar', 'Dosya çakışma haritası', Katman 8 UX sözleşmesi; (2) paket spesifikasyonu ${p.belge} — 'Birleşik koşullar' bölümü BAĞLAYICIDIR; (3) spesifikasyonun 'dosya ayak izi'ndeki mevcut dosyalar (gerçek satır numaraları spesifikasyondakinden kaymış olabilir — koda bak); (4) ${KOK}/dotnet/Kuyumcu.Tests/TestOrtami.cs, HttpOrtami.cs ve bir ekran testi (desen); (5) ${KOK}/SONRAKI-OTURUM-PROMPT.md §6 Tuzaklar.
${uxSozlesme ? 'UX SÖZLEŞMESİ (Katman 8, bağlayıcı):\n' + uxSozlesme + '\n' : ''}
${p.kosullar && p.kosullar.length ? 'EK KOŞULLAR (oylamadan):\n' + p.kosullar.map(k => '- ' + k).join('\n') + '\n' : ''}
${ekBulgular ? 'ÖNCEKİ DOĞRULAMADA KALAN BULGULAR (önce bunları kapat, sonra kalan adımları yeniden koş):\n' + ekBulgular + '\n' : ''}
ADIMLAR:
1. Spesifikasyonu koda karşı doğrula: 'mevcut durum' iddiaları hâlâ doğru mu; sapma varsa spesifikasyonun HEDEFİNE sadık kalarak uyarlama yap ve raporda gerekçele. Spesifikasyon bir yasal/mevzuat hükmünü 'DOGRULANMALI/VARSAYIM' işaretlemişse o hükmü koda sabit yazma: yapılandırmadan okunur, varsayılan KAPALI/tutucu; raporda 'kurucuya/mali müşavire' notu düş.
2. Kodu yaz: veri modeli (+ migration prosedürü), servis/kapılar, uçlar (+[RolGerekli]), Sozlukler TR+EN, ekran (+ düzenleme yolu, ProAlan/boş durum/bandlar), sahte API güncellemesi.
3. 'dotnet build Kuyumcu.sln --nologo' → 0 hata (0 uyarı hedefi). Kırmızıysa düzelt; düzelemiyorsan DUR ve raporla.
4. Testler: spesifikasyondaki test listesi (DB/HTTP/bUnit/saf) → filtreli koşumlar yeşil.
5. Mutasyon kanıtları: spesifikasyondaki listenin HER maddesi (kod: M<no>-<sıra>) → kırmızıya dönen testin ADI. Açık varsa testi güçlendir.
6. Tam süit: 'dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --logger trx --results-directory ${SCRATCH}/trx-p${p.no}' → 0 başarısız; kırmızı varsa adını trx'ten al, çöz ya da kararsızlık kanıtıyla raporla. ${KOK}/dotnet/denetim.sh → DENETİM TEMİZ.
7. Canlı doğrulama (5198): spesifikasyonun 'canlı doğrulama' listesi birebir: beklenen HTTP kodları + SQL kanıtı; yanıtları OKU (-o /dev/null yasak). Sonra API'yi durdur, test kiracısını sil (yalnız KENDİ açtığın test kiracısını; 'QA-TEST-' önekli kiracılar kalıcı QA fikstürüdür — ASLA silme/değiştirme, verisine dokunma), tenants sayısını doğrula.
8. Raporu ${RAPOR_DIZINI}/PAKET-${p.no}-uygulama-raporu.md dosyasına yaz: yapılanlar, sapmalar ve gerekçeleri, migration, test sayıları (önce/sonra), mutasyon tablosu, denetim çıktısı, canlı tablo + SQL, değişen dosyalar ('git status --porcelain | grep -v -E "/(bin|obj)/"'), koşul karşılama tablosu, açık kalanlar, kurucuya/mali müşavire notlar.
Yapılandırılmış çıktıyı gerçek değerlerle doldur.`

const dogrulamaPrompt = (p, uyg) => `${KURALLAR}
GÖREV — HASIM DOĞRULAYICI (Aşkın + Mehmet; uygulayıcıdan farklı model). Paket ${p.no} "${p.ad}". Uygulayıcının raporuna GÜVENME; kanıtları yeniden üret. Uygulayıcı çıktısı:
${JSON.stringify(uyg, null, 1)}
Spesifikasyon: ${p.belge} (Birleşik koşullar bağlayıcı). Rapor: ${uyg.rapor_dosyasi}
KONTROLLER (komutu koş, gerçek çıktıyı 'kanit'e yaz; çıktı ekonomisi kuralına uy):
1. 'cd ${KOK}/dotnet && dotnet build Kuyumcu.sln --nologo' → 0 hata; uyarı sayısı.
2. Spesifikasyon uyumu: her uç (grep [Http…] + rol), her yeni tablo/kolon (Entities/DbContext/Migration Up gövdesi), her ekran/rota (+UstBar rota başlığı, KenarCubugu yetki), her test dosyası VAR mı; 'Birleşik koşullar'ın her maddesi için kanıt; UX sözleşmesi: yeni form için düzenleme yolu, Sozlukler TR+EN, mevcut id'ler değişmemiş ('git diff' ile kaldırılan id="..." satırı var mı), sabit renk/inline style yok.
3. Güvenlik: yeni varlıklar IKiraciVarlik + tenant indeksi; para el değiştiren yeni akış tevsik+MASAK+gün kilidi+kasa para birimi kapılarından geçiyor mu; 'grep -rn "Hareketler\\.Add(" Kuyumcu.Api --include=*.cs' yalnız HareketFabrikasi.cs; IgnoreQueryFilters/Find 0; ExecuteUpdate/Delete 4; yeni yazma uçlarında [RolGerekli].
4. '${KOK}/dotnet/denetim.sh' → DENETİM TEMİZ.
5. Tam süit BİR koşu (trx → ${SCRATCH}/trx-dogrulama-p${p.no}) → 0 başarısız; toplam sayı; kırmızı varsa adı.
6. Mutasyon örneklemesi: uygulayıcının tablosundan EN KRİTİK 2 mutasyonu KENDİN yeniden uygula (yedek ${SCRATCH}/dogrulama-yedek/p${p.no}/, perl, filtreli test, geri al, cmp) → aynı testler kırmızıya dönmeli; sonra TEMİZ dotnet build.
7. Artık/ayak izi: 'find ${KOK}/dotnet -name "*.mutorj" -newer ${KOK}/dotnet/Kuyumcu.sln', '*.bak', '*.tmp' boş; 'git status --porcelain | grep -v -E "/(bin|obj)/"' beklenmeyen dosya yok; sıfır baytlık yeni migration yok; Data/Migrations/KuyumcuDbContextModelSnapshot.cs son migration ile tutarlı.
8. DB: 'SELECT COUNT(*) FROM tenants' = uygulayıcının beyan ettiği başlangıç sayısı; 'PAKET${p.no}-TEST' kiracısı yok; migration varsa __EFMigrationsHistory'de kayıtlı; 5199 dinliyor, 5198 kapalı.
9. Rapor dosyası var ve canlı tablo + SQL kanıtı içeriyor; yasal hüküm 'DOGRULANMALI' ise koda sabit yazılmamış.
Karar: KRITIK bulgu yoksa GECTI; aksi hâlde KALDI (bulgular dosya:satır / komut çıktısıyla).`

const sonuclar = []
for (const p of paketler) {
  phase('Uygulama')
  log(`Dalga ${dalga} · Paket ${p.no} ${p.ad} — uygulama başlıyor`)
  let uyg = await agent(uygulamaPrompt(p, null), { label: `uygula:p${p.no}`, phase: 'Uygulama', model: 'opus', effort: 'medium', schema: UYGULAMA })
  let dogrulama = null
  let tur = 0
  while (uyg && tur < 3) {
    tur++
    phase('Dogrulama')
    dogrulama = await agent(dogrulamaPrompt(p, uyg), { label: `dogrula:p${p.no}:t${tur}`, phase: 'Dogrulama', effort: 'medium', schema: DOGRULAMA })
    if (!dogrulama) { log(`Paket ${p.no}: doğrulayıcı dönmedi`); break }
    log(`Paket ${p.no} doğrulama turu ${tur}: ${dogrulama.karar} · bulgu ${dogrulama.bulgular.length} · test toplam ${dogrulama.test_toplam}`)
    if (dogrulama.karar === 'GECTI' || tur === 3) break
    const kritik = dogrulama.bulgular.map(b => `- [${b.ciddiyet}] ${b.baslik}: ${b.kanit}${b.oneri ? ' → ' + b.oneri : ''}`).join('\n')
    phase('Uygulama')
    uyg = await agent(uygulamaPrompt(p, kritik), { label: `duzelt:p${p.no}:t${tur}`, phase: 'Uygulama', model: 'opus', effort: 'medium', schema: UYGULAMA })
  }
  // ═══ UYGULAYICININ KENDİ BEYANI DOĞRULAYICIYI BAĞLAR (2026-09-21) ═══════
  // ARIZA 13: P-36'da uygulayıcı `durum: "YARIM"` dedi, yapmadıklarını tek tek
  // saydı ("176 select'in 174'ü göç etmedi, girdi ailesi yazılmadı, kabul eşiği
  // ÖLÇÜLMEDİ → karşılanmadı sayılır, paket KAPANMADI") — ve hasım doğrulayıcı
  // buna rağmen GECTI dedi. Kurucu ekranda arayıp bulamadı; haklıydı.
  // Kural: uygulayıcı "BITTI" demediyse paket GEÇEMEZ. Doğrulayıcı bir paketi
  // yalnız DAHA SIKI yapabilir, daha gevşek YAPAMAZ.
  const beyan = (uyg && uyg.durum ? String(uyg.durum) : '').toUpperCase()
  const uygulayiciBitti = beyan === 'BITTI' || beyan === 'BİTTİ'
  if (dogrulama && dogrulama.karar === 'GECTI' && !uygulayiciBitti) {
    log(`Paket ${p.no}: doğrulayıcı GECTI dedi ama UYGULAYICI "${beyan || 'beyan yok'}" dedi — ` +
        `BEYAN BAĞLAYICIDIR, paket GEÇMEDİ sayılıyor (orkestratör kuralı).`)
  }
  const gecti = !!(dogrulama && dogrulama.karar === 'GECTI' && uygulayiciBitti)
  sonuclar.push({ paket: p.no, ad: p.ad, gecti, tur, uygulama: uyg, dogrulama })
  if (!gecti) { log(`Paket ${p.no} KAPIDAN GEÇMEDİ — dalga durduruldu (orkestratör denetimi gerekli)`); break }
}
return { dalga, sonuclar, tamamlanan: sonuclar.filter(s => s.gecti).map(s => s.paket), duran: sonuclar.find(s => !s.gecti)?.paket || null }
