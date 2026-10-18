using System.Text;

namespace Kuyumcu.Core.Domain;

/// <summary>
/// D1-02 (18.10) — QR kodu (ISO/IEC 18004), dış kütüphanesiz (Code128 ile aynı ilke: üçüncü taraf paket yok).
/// Kapsam bilinçli olarak dar: BAYT kipi (UTF-8), hata düzeltme M (%15), sürüm 1–10 (en çok 213 bayt) — etikette
/// ürün kodu/barkodu taşımak için yeter. Sığmayan veri istisna atar (sessiz kırpma yok).
/// Algoritma: veri bitleri → Reed–Solomon (GF(256), 0x11D) → blok serpiştirme → zikzak yerleşim →
/// 8 maskeden ceza puanı en düşük olanı → biçim (BCH 0x537 ^ 0x5412) ve sürüm (BCH 0x1F25, v≥7) bilgisi.
/// </summary>
public static class QrKod
{
    // Sürüm başına (M düzeyi): blok başına EC kod sözcüğü, kısa blok sayısı, kısa blok veri sözcüğü, uzun blok sayısı.
    private static readonly (int Ec, int B1, int D1, int B2)[] MTablosu =
    [
        default,
        (10, 1, 16, 0), (16, 1, 28, 0), (26, 1, 44, 0), (18, 2, 32, 0), (24, 2, 43, 0),
        (16, 4, 27, 0), (18, 4, 31, 0), (22, 2, 38, 2), (22, 3, 36, 2), (26, 4, 43, 1),
    ];

    private static readonly int[][] Hizalama =
    [
        [], [], [6, 18], [6, 22], [6, 26], [6, 30], [6, 34], [6, 22, 38], [6, 24, 42], [6, 26, 46], [6, 28, 50],
    ];

    /// <summary>Metni QR modül matrisine çevirir (true = koyu). Kenar boşluğu YOKTUR (çizim 4 modül ekler).</summary>
    public static bool[,] Matris(string metin)
    {
        var veri = Encoding.UTF8.GetBytes(metin ?? "");
        int surum = 0;
        for (var v = 1; v <= 10; v++)
        {
            var (_, b1, d1, b2) = MTablosu[v];
            var kapasiteBit = (b1 * d1 + b2 * (d1 + 1)) * 8;
            var gereken = 4 + (v < 10 ? 8 : 16) + veri.Length * 8;
            if (gereken <= kapasiteBit) { surum = v; break; }
        }
        if (surum == 0) throw new ArgumentException("QR için metin çok uzun (en çok 213 bayt)", nameof(metin));

        var kodSozcukleri = Kodla(veri, surum);
        var n = 17 + surum * 4;
        var m = new bool[n, n];
        var islev = new bool[n, n];
        IslevDesenleri(m, islev, surum);
        Yerlestir(m, islev, kodSozcukleri);

        int enIyiMaske = 0; long enIyiCeza = long.MaxValue;
        for (var maske = 0; maske < 8; maske++)
        {
            Maskele(m, islev, maske);
            BicimBilgisi(m, islev, maske);
            var ceza = Ceza(m);
            if (ceza < enIyiCeza) { enIyiCeza = ceza; enIyiMaske = maske; }
            Maskele(m, islev, maske);   // XOR: geri al
        }
        Maskele(m, islev, enIyiMaske);
        BicimBilgisi(m, islev, enIyiMaske);
        return m;
    }

    /// <summary>SVG (kenarda 4 modül sessiz alan). Boyut CSS ile verilir; modül kare kalır.</summary>
    public static string Svg(string metin)
    {
        var m = Matris(metin);
        var n = m.GetLength(0);
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {n + 8} {n + 8}\" shape-rendering=\"crispEdges\" role=\"img\" aria-label=\"QR\"><path fill=\"#000\" d=\"");
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
                if (m[y, x]) sb.Append($"M{x + 4} {y + 4}h1v1h-1z");
        sb.Append("\"/></svg>");
        return sb.ToString();
    }

    // ── Veri + hata düzeltme ────────────────────────────────────────────────

    private static byte[] Kodla(byte[] veri, int surum)
    {
        var (ec, b1, d1, b2) = MTablosu[surum];
        var veriSozcukSayisi = b1 * d1 + b2 * (d1 + 1);
        var bitler = new List<bool>();
        void Ekle(int deger, int uzunluk) { for (var i = uzunluk - 1; i >= 0; i--) bitler.Add(((deger >> i) & 1) != 0); }
        Ekle(0b0100, 4);
        Ekle(veri.Length, surum < 10 ? 8 : 16);
        foreach (var b in veri) Ekle(b, 8);
        var kapasite = veriSozcukSayisi * 8;
        Ekle(0, Math.Min(4, kapasite - bitler.Count));
        while (bitler.Count % 8 != 0) bitler.Add(false);
        for (var dolgu = 0xEC; bitler.Count < kapasite; dolgu ^= 0xEC ^ 0x11) Ekle(dolgu, 8);

        var sozcukler = new byte[veriSozcukSayisi];
        for (var i = 0; i < bitler.Count; i++)
            if (bitler[i]) sozcukler[i >> 3] |= (byte)(0x80 >> (i & 7));

        // Bloklar: önce b1 kısa (d1), sonra b2 uzun (d1+1) blok.
        var bloklar = new List<byte[]>();
        var ecBloklar = new List<byte[]>();
        var bolen = RsBolen(ec);
        var k = 0;
        for (var i = 0; i < b1 + b2; i++)
        {
            var uz = d1 + (i < b1 ? 0 : 1);
            var blok = sozcukler[k..(k + uz)];
            k += uz;
            bloklar.Add(blok);
            ecBloklar.Add(RsKalan(blok, bolen));
        }
        var sonuc = new List<byte>();
        for (var i = 0; i <= d1; i++)
            foreach (var bl in bloklar)
                if (i < bl.Length) sonuc.Add(bl[i]);
        for (var i = 0; i < ec; i++)
            foreach (var e in ecBloklar) sonuc.Add(e[i]);
        return [.. sonuc];
    }

    private static byte Carp(int x, int y)
    {
        var z = 0;
        for (var i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return (byte)z;
    }

    private static byte[] RsBolen(int derece)
    {
        var sonuc = new byte[derece];
        sonuc[derece - 1] = 1;
        var kok = 1;
        for (var i = 0; i < derece; i++)
        {
            for (var j = 0; j < sonuc.Length; j++)
            {
                sonuc[j] = Carp(sonuc[j], kok);
                if (j + 1 < sonuc.Length) sonuc[j] ^= sonuc[j + 1];
            }
            kok = Carp(kok, 0x02);
        }
        return sonuc;
    }

    private static byte[] RsKalan(byte[] veri, byte[] bolen)
    {
        var sonuc = new byte[bolen.Length];
        foreach (var b in veri)
        {
            var faktor = b ^ sonuc[0];
            Array.Copy(sonuc, 1, sonuc, 0, sonuc.Length - 1);
            sonuc[^1] = 0;
            for (var i = 0; i < sonuc.Length; i++) sonuc[i] ^= Carp(bolen[i], faktor);
        }
        return sonuc;
    }

    // ── Desenler ve yerleşim ────────────────────────────────────────────────

    private static void Koy(bool[,] m, bool[,] islev, int x, int y, bool koyu) { m[y, x] = koyu; islev[y, x] = true; }

    private static void IslevDesenleri(bool[,] m, bool[,] islev, int surum)
    {
        var n = m.GetLength(0);
        for (var i = 0; i < n; i++) { Koy(m, islev, 6, i, i % 2 == 0); Koy(m, islev, i, 6, i % 2 == 0); }
        Bulucu(m, islev, 3, 3); Bulucu(m, islev, n - 4, 3); Bulucu(m, islev, 3, n - 4);
        var h = Hizalama[surum];
        for (var i = 0; i < h.Length; i++)
            for (var j = 0; j < h.Length; j++)
                if (!((i == 0 && j == 0) || (i == 0 && j == h.Length - 1) || (i == h.Length - 1 && j == 0)))
                    HizalamaDeseni(m, islev, h[i], h[j]);
        BicimBilgisi(m, islev, 0);   // yerleri ayır (değerler maske seçiminde yazılır)
        SurumBilgisi(m, islev, surum);
    }

    private static void Bulucu(bool[,] m, bool[,] islev, int x, int y)
    {
        var n = m.GetLength(0);
        for (var dy = -4; dy <= 4; dy++)
            for (var dx = -4; dx <= 4; dx++)
            {
                var d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && xx < n && yy >= 0 && yy < n) Koy(m, islev, xx, yy, d != 2 && d != 4);
            }
    }

    private static void HizalamaDeseni(bool[,] m, bool[,] islev, int x, int y)
    {
        for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                Koy(m, islev, x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private static void BicimBilgisi(bool[,] m, bool[,] islev, int maske)
    {
        var n = m.GetLength(0);
        var veri = (0b00 << 3) | maske;   // M düzeyi = 00
        var kalan = veri;
        for (var i = 0; i < 10; i++) kalan = (kalan << 1) ^ ((kalan >> 9) * 0x537);
        var bitler = ((veri << 10) | kalan) ^ 0x5412;
        bool Bit(int i) => ((bitler >> i) & 1) != 0;
        for (var i = 0; i <= 5; i++) Koy(m, islev, 8, i, Bit(i));
        Koy(m, islev, 8, 7, Bit(6)); Koy(m, islev, 8, 8, Bit(7)); Koy(m, islev, 7, 8, Bit(8));
        for (var i = 9; i < 15; i++) Koy(m, islev, 14 - i, 8, Bit(i));
        for (var i = 0; i < 8; i++) Koy(m, islev, n - 1 - i, 8, Bit(i));
        for (var i = 8; i < 15; i++) Koy(m, islev, 8, n - 15 + i, Bit(i));
        Koy(m, islev, 8, n - 8, true);   // her zaman koyu modül
    }

    private static void SurumBilgisi(bool[,] m, bool[,] islev, int surum)
    {
        if (surum < 7) return;
        var n = m.GetLength(0);
        var kalan = surum;
        for (var i = 0; i < 12; i++) kalan = (kalan << 1) ^ ((kalan >> 11) * 0x1F25);
        var bitler = (surum << 12) | kalan;
        for (var i = 0; i < 18; i++)
        {
            var bit = ((bitler >> i) & 1) != 0;
            int a = n - 11 + i % 3, b = i / 3;
            Koy(m, islev, a, b, bit); Koy(m, islev, b, a, bit);
        }
    }

    private static void Yerlestir(bool[,] m, bool[,] islev, byte[] sozcukler)
    {
        var n = m.GetLength(0);
        var i = 0;
        for (var sag = n - 1; sag >= 1; sag -= 2)
        {
            if (sag == 6) sag = 5;
            for (var dikey = 0; dikey < n; dikey++)
                for (var j = 0; j < 2; j++)
                {
                    var x = sag - j;
                    var yukari = ((sag + 1) & 2) == 0;
                    var y = yukari ? n - 1 - dikey : dikey;
                    if (!islev[y, x] && i < sozcukler.Length * 8)
                    {
                        m[y, x] = ((sozcukler[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                        i++;
                    }
                }
        }
    }

    private static void Maskele(bool[,] m, bool[,] islev, int maske)
    {
        var n = m.GetLength(0);
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                if (islev[y, x]) continue;
                var ters = maske switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                };
                if (ters) m[y, x] = !m[y, x];
            }
    }

    // ── Ceza puanı (ISO 18004 §8.8.2: N1=3, N2=3, N3=40, N4=10) ─────────────

    private static long Ceza(bool[,] m)
    {
        var n = m.GetLength(0);
        long ceza = 0;
        for (var yon = 0; yon < 2; yon++)
            for (var a = 0; a < n; a++)
            {
                var seri = 0; var onceki = false;
                var satir = new bool[n];
                for (var b = 0; b < n; b++)
                {
                    var v = yon == 0 ? m[a, b] : m[b, a];
                    satir[b] = v;
                    if (b > 0 && v == onceki) seri++;
                    else { if (seri >= 5) ceza += 3 + (seri - 5); seri = 1; }
                    onceki = v;
                }
                if (seri >= 5) ceza += 3 + (seri - 5);
                // N3: 1:1:3:1:1 bulucu benzeri desen + 4 açık modül (her iki yönde)
                for (var b = 0; b + 11 <= n; b++)
                {
                    bool D(int i) => satir[b + i];
                    var desen = D(0) && !D(1) && D(2) && D(3) && D(4) && !D(5) && D(6);
                    var desen2 = D(4) && !D(5) && D(6) && D(7) && D(8) && !D(9) && D(10);
                    if (desen && !D(7) && !D(8) && !D(9) && !D(10)) ceza += 40;
                    if (desen2 && !D(0) && !D(1) && !D(2) && !D(3)) ceza += 40;
                }
            }
        for (var y = 0; y < n - 1; y++)
            for (var x = 0; x < n - 1; x++)
            {
                var c = m[y, x];
                if (c == m[y, x + 1] && c == m[y + 1, x] && c == m[y + 1, x + 1]) ceza += 3;
            }
        var koyu = 0;
        foreach (var v in m) if (v) koyu++;
        var toplam = n * n;
        var k = (Math.Abs(koyu * 20 - toplam * 10) + toplam - 1) / toplam - 1;
        ceza += Math.Max(0, k) * 10;
        return ceza;
    }
}
