using Kuyumcu.Core.I18n;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kuyumcu.Api.Data.Migrations
{
    /// <summary>
    /// KESIF-P2 (#22) — cari uyruğu tek standarda (ISO 3166-1 ALPHA-3, MASAK formuyla aynı)
    /// taşınır: kolon varchar(2) → varchar(3); eski 2 harfli değerler
    /// <see cref="DonukEsleme"/> (30.09 <see cref="UyrukEslemesi.Alpha2Alpha3"/> kopyası) ile eşlenir (TR → TUR …).
    /// ⚠ EŞLENEMEYEN değer UYDURULMAZ (ekip kararı 4): olduğu gibi kalır, ekranda
    ///   "Uyruk kodunu düzeltin" rozeti çıkar. Eşlenen ve eşlenemeyen HER kart
    ///   denetim_kayitlari'na (varlik_turu 'CariUyrukGocu', varlik_id = cari kimliği)
    ///   GÜNLÜKLENİR — kişisel veri (uyruk değeri) günlüğe YAZILMAZ.
    /// </summary>
    public partial class UyrukAlpha3 : Migration
    {
        /// <summary>
        /// DONDURULMUŞ eşleme (KESIF-P2 tur 2, doğrulayıcı bulgusu): migration çalışma anındaki
        /// <see cref="UyrukEslemesi.Alpha2Alpha3"/> sözlüğüne BAĞLI DEĞİLDİR — sözlük ileride değişse
        /// de bu migration her zaman aynı SQL'i üretir. 30.09 sözlüğünden birebir üretildi (249 çift);
        /// Uyruk_eslemesi_migrationda_donuk_ve_sozlukle_ayni testi eşitliği sınar (sözlük değişirse
        /// test kırmızı olur → yeni bir migration yazılır, bu dosya değişmez).
        /// </summary>
        public const string DonukEsleme = """
            AD:AND AE:ARE AF:AFG AG:ATG AI:AIA AL:ALB AM:ARM AO:AGO AQ:ATA AR:ARG AS:ASM AT:AUT
            AU:AUS AW:ABW AX:ALA AZ:AZE BA:BIH BB:BRB BD:BGD BE:BEL BF:BFA BG:BGR BH:BHR BI:BDI
            BJ:BEN BL:BLM BM:BMU BN:BRN BO:BOL BQ:BES BR:BRA BS:BHS BT:BTN BV:BVT BW:BWA BY:BLR
            BZ:BLZ CA:CAN CC:CCK CD:COD CF:CAF CG:COG CH:CHE CI:CIV CK:COK CL:CHL CM:CMR CN:CHN
            CO:COL CR:CRI CU:CUB CV:CPV CW:CUW CX:CXR CY:CYP CZ:CZE DE:DEU DJ:DJI DK:DNK DM:DMA
            DO:DOM DZ:DZA EC:ECU EE:EST EG:EGY EH:ESH ER:ERI ES:ESP ET:ETH FI:FIN FJ:FJI FK:FLK
            FM:FSM FO:FRO FR:FRA GA:GAB GB:GBR GD:GRD GE:GEO GF:GUF GG:GGY GH:GHA GI:GIB GL:GRL
            GM:GMB GN:GIN GP:GLP GQ:GNQ GR:GRC GS:SGS GT:GTM GU:GUM GW:GNB GY:GUY HK:HKG HM:HMD
            HN:HND HR:HRV HT:HTI HU:HUN ID:IDN IE:IRL IL:ISR IM:IMN IN:IND IO:IOT IQ:IRQ IR:IRN
            IS:ISL IT:ITA JE:JEY JM:JAM JO:JOR JP:JPN KE:KEN KG:KGZ KH:KHM KI:KIR KM:COM KN:KNA
            KP:PRK KR:KOR KW:KWT KY:CYM KZ:KAZ LA:LAO LB:LBN LC:LCA LI:LIE LK:LKA LR:LBR LS:LSO
            LT:LTU LU:LUX LV:LVA LY:LBY MA:MAR MC:MCO MD:MDA ME:MNE MF:MAF MG:MDG MH:MHL MK:MKD
            ML:MLI MM:MMR MN:MNG MO:MAC MP:MNP MQ:MTQ MR:MRT MS:MSR MT:MLT MU:MUS MV:MDV MW:MWI
            MX:MEX MY:MYS MZ:MOZ NA:NAM NC:NCL NE:NER NF:NFK NG:NGA NI:NIC NL:NLD NO:NOR NP:NPL
            NR:NRU NU:NIU NZ:NZL OM:OMN PA:PAN PE:PER PF:PYF PG:PNG PH:PHL PK:PAK PL:POL PM:SPM
            PN:PCN PR:PRI PS:PSE PT:PRT PW:PLW PY:PRY QA:QAT RE:REU RO:ROU RS:SRB RU:RUS RW:RWA
            SA:SAU SB:SLB SC:SYC SD:SDN SE:SWE SG:SGP SH:SHN SI:SVN SJ:SJM SK:SVK SL:SLE SM:SMR
            SN:SEN SO:SOM SR:SUR SS:SSD ST:STP SV:SLV SX:SXM SY:SYR SZ:SWZ TC:TCA TD:TCD TF:ATF
            TG:TGO TH:THA TJ:TJK TK:TKL TL:TLS TM:TKM TN:TUN TO:TON TR:TUR TT:TTO TV:TUV TW:TWN
            TZ:TZA UA:UKR UG:UGA UM:UMI US:USA UY:URY UZ:UZB VA:VAT VC:VCT VE:VEN VG:VGB VI:VIR
            VN:VNM VU:VUT WF:WLF WS:WSM YE:YEM YT:MYT ZA:ZAF ZM:ZMB ZW:ZWE
            """;

        /// <summary>Donuk eşlemenin (alpha-2, alpha-3) çiftleri, dosyadaki sırayla.</summary>
        public static IReadOnlyList<(string A2, string A3)> DonukCiftler() =>
            DonukEsleme.Split((char[])[' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(':'))
                .Select(p => (p[0], p[1]))
                .ToList();

        /// <summary>Günlük + eşleme SQL'i (test aynı metni koşar: Migration_uyruk_TR_TUR_eslenemeyen_korunur).</summary>
        public static string EslemeSql()
        {
            var ciftler = DonukCiftler();
            var liste = string.Join(",", ciftler.Select(c => $"'{c.A2}'"));
            var durumlar = string.Join(" ", ciftler.Select(c => $"WHEN '{c.A2}' THEN '{c.A3}'"));
            return $"""
                INSERT INTO denetim_kayitlari (id, tenant_id, kullanici_id, eylem, varlik_turu, varlik_id, hassas, detay, tarih, sube_id)
                SELECT UUID(), c.tenant_id, NULL, 'GUNCELLEME', 'CariUyrukGocu', c.id, 1,
                  CASE WHEN UPPER(TRIM(c.uyruk)) IN ({liste})
                       THEN 'Uyruk kodu 2 harfli eski standarttan 3 harfliye eşlendi (migration UyrukAlpha3).'
                       ELSE 'Uyruk kodu EŞLENEMEDİ: değer uydurulmadı, olduğu gibi korundu; kartta Uyruk kodunu düzeltin uyarısı çıkar (migration UyrukAlpha3).' END,
                  UTC_TIMESTAMP(6), NULL
                FROM cariler c
                WHERE c.uyruk IS NOT NULL AND CHAR_LENGTH(TRIM(c.uyruk)) = 2;
                UPDATE cariler SET uyruk = CASE UPPER(TRIM(uyruk)) {durumlar} ELSE uyruk END
                WHERE uyruk IS NOT NULL AND CHAR_LENGTH(TRIM(uyruk)) = 2 AND UPPER(TRIM(uyruk)) IN ({liste});
                """;
        }

        /// <summary>Geri dönüş: eşlenmiş 3 harfli değer 2 harfe döner (eşlenemeyen 3 harfli değer
        /// varsa kolon daraltması MySQL katı kipte DURUR — sessiz kırpma yok).</summary>
        public static string GeriSql()
        {
            var ciftler = DonukCiftler();
            var liste = string.Join(",", ciftler.Select(c => $"'{c.A3}'"));
            var durumlar = string.Join(" ", ciftler.Select(c => $"WHEN '{c.A3}' THEN '{c.A2}'"));
            return $"UPDATE cariler SET uyruk = CASE uyruk {durumlar} ELSE uyruk END WHERE uyruk IN ({liste});";
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "uyruk",
                table: "cariler",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldMaxLength: 2,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(EslemeSql());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(GeriSql());
            migrationBuilder.AlterColumn<string>(
                name: "uyruk",
                table: "cariler",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
