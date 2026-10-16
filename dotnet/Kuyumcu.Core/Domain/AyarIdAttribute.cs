using System.ComponentModel.DataAnnotations;

namespace Kuyumcu.Core.Domain;

/// <summary>
/// Ayar kimliği doğrulaması — <b>TEK KAYNAK</b> <see cref="Ayarlar.Tumu"/> (K20-8).
///
/// <para>
/// ⚠ NEDEN VAR: ayar listesi DÖRT DTO'da elle yazılmış bir düzenli ifadeydi
/// (<c>"K8|K14|…|PD999"</c>). Yeni bir ayar (çelik) eklendiğinde dördünden biri
/// unutuluyor ve "ürün kartında var, emanette yok" gibi sessiz sapmalar doğuyordu.
/// <c>[RegularExpression(Ayarlar.Desen)]</c> ile bağlamak da mümkün DEĞİLDİ:
/// öznitelik argümanı <c>const</c> olmak zorundadır, oysa desen artık
/// <see cref="Ayarlar.Tumu"/>'nden ÜRETİLİYOR. Bu öznitelik doğrulamayı
/// derleme zamanı sabitinden ÇALIŞMA ZAMANI SÖZLÜĞÜNE taşır: ayar eklemek
/// yalnız <c>Ayarlar.cs</c>'i değiştirir.
/// </para>
/// <para>
/// Boş/null değer GEÇERLİ sayılır — zorunluluk <see cref="RequiredAttribute"/>
/// işidir; PATCH sözleşmesinde "gönderilmeyen alan korunur" kuralı bozulmasın.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AyarIdAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return true;
        return Ayarlar.Gecerli(s);
    }

    public override string FormatErrorMessage(string name) =>
        $"Ayar geçersiz — kabul edilen değerler: {string.Join(", ", Ayarlar.GecerliIdler)}";
}
