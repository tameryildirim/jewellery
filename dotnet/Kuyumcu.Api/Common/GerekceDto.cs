using System.ComponentModel.DataAnnotations;

namespace Kuyumcu.Api.Common;

/// <summary>
/// PAKET S-3b — YALNIZ GEREKÇE isteyen düzeltme uçlarının ortak gövdesi.
///
/// NEDEN AYRI BİR DTO: <c>StornoDto</c> kur anlık görüntüsünü ZORUNLU kılar
/// (defter hareketinin ters kaydı bugünün kuruyla yazılır). Elle girilmiş
/// kasa hareketinin ve manuel kur kaydının kur bacağı YOKTUR; StornoDto
/// kullanılsaydı, kur verisi hiç olmayan bir kiracıda bu iki düzeltme yolu
/// da kapanırdı (kilidin sebebi de anlaşılmazdı).
///
/// ⚠ DataAnnotations YALNIZ controller yolunda çalışır; servisler gerekçeyi
/// <c>StornoServisi.GerekceDogrula</c> ile ELLE de doğrular.
/// </summary>
public sealed class GerekceDto
{
    [Required, MinLength(20), MaxLength(1000)] public string Gerekce { get; set; } = null!;
}
