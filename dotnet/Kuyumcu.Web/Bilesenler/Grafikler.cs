namespace Kuyumcu.Web.Bilesenler;

/// <summary>Grafik bileşenlerinin ortak veri noktası tipi.</summary>
public sealed record GrafikNokta(string Etiket, decimal Deger);

/// <summary>İki serili nokta (örn. satış/alış karşılaştırması).</summary>
public sealed record CiftNokta(string Etiket, decimal Deger1, decimal Deger2);
