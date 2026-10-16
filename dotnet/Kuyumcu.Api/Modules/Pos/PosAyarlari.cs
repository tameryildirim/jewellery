namespace Kuyumcu.Api.Modules.Pos;

/// <summary>
/// POS davranış eşikleri — ADR-0005: sayı koda gömülmez, yapılandırmadan okunur.
/// <para>
/// ⚠ HİÇBİRİ MEVZUAT DEĞİLDİR. Komisyon oranı ve valör günü bankayla yapılan
/// ÜYE İŞYERİ SÖZLEŞMESİNDEN gelir ve kuyumcudan kuyumcuya değişir; bu yüzden
/// oran/valör sayısı burada da yoktur, cihaz tanımında kullanıcıdan alınır.
/// Buradaki ayarlar yalnız DAVRANIŞ anahtarlarıdır ve varsayılanları TUTUCUdur
/// (mevcut davranışı değiştirmeyen yön).
/// </para>
/// </summary>
public sealed class PosAyarlari
{
    /// <summary>
    /// true → kart bacağı için cihaz çözülemezse istek REDDEDİLİR (fail-closed).
    /// Varsayılan false: paket 31 öncesi davranış aynen sürer, POS satırı yazılmaz.
    /// </summary>
    public bool CihazZorunlu { get; set; }

    /// <summary>
    /// true → valör günü sayılırken Cumartesi/Pazar atlanır.
    /// ⚠ RESMÎ TATİL TAKVİMİ YOKTUR ve UYDURULMAZ: bayram günleri yanlış
    /// hesaplanır. Bu yüzden varsayılan KAPALIdır (takvim günü).
    /// </summary>
    public bool ValorIsGunu { get; set; }

    /// <summary>
    /// İadede komisyon da geri alınır mı? Varsayılan false — bankalar genelde
    /// kesilen komisyonu iade etmez. DOĞRULANMAMIŞ bir ticari teamüldür;
    /// sözleşmesi farklı olan kuyumcu bunu açar.
    /// </summary>
    public bool IadeKomisyonIadeEdilir { get; set; }

    /// <summary>Kabul edilen azami valör günü — saçma girişe karşı üst sınır.</summary>
    public int AzamiValorGun { get; set; } = 90;

    /// <summary>Oto eşleştirme önerisinde kabul edilen kuruş toleransı.</summary>
    public int OtoEslestirToleransKurus { get; set; }
}
