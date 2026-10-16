namespace Kuyumcu.Api.Auth;

/// <summary>
/// PAKET P-09 — "bu uç kimliği olan HERKESE bilerek açıktır" beyanı.
///
/// <para>
/// ⚠ NEDEN VAR: <c>OZNITELIKSIZ_UC = 0</c> sayacı fail-closed bir kapıdır —
/// ne <see cref="RolGerekliAttribute"/> ne <c>ModulYetkisi</c> ne de
/// <c>AllowAnonymous</c> taşıyan bir uç, "unutulmuş" sayılır ve denetimi kırar.
/// Gerçekten herkese açık olması GEREKEN uçlar (kendi hesabın, kimlik öncesi
/// yol, kiracı verisi taşımayan meta) bu öznitelikle GEREKÇESİYLE beyan edilir.
/// </para>
/// <para>
/// ⚠ DAVRANIŞI YOKTUR: hiçbir filtre çalıştırmaz, hiçbir kapı açmaz. Yalnız
/// bir beyandır; gerçek koruma <c>[Authorize]</c> + kiracı filtresidir.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class HerkeseAcikAttribute(string gerekce) : Attribute
{
    /// <summary>Neden modül/rol kapısı YOK — boş bırakılamaz.</summary>
    public string Gerekce { get; } = string.IsNullOrWhiteSpace(gerekce)
        ? throw new ArgumentException("HerkeseAcik gerekçesiz kullanılamaz", nameof(gerekce))
        : gerekce;
}
