namespace Kuyumcu.Api.Common;

/// <summary>ULID üretimi — TS tarafındaki ulid() ile aynı format (26 kr, sıralanabilir).</summary>
public static class Kimlik
{
    public static string Ulid() => NUlid.Ulid.NewUlid().ToString();
}
