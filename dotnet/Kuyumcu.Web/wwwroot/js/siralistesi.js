// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-37 — GRUP İÇİ SÜRÜKLE-BIRAK SIRALAMA (fare + DOKUNMATİK)
//
// NEDEN HTML5 draggable DEĞİL: HTML5 sürükleme dokunmatik ekranda ÇALIŞMAZ
// (mobil tarayıcılar dragstart üretmez). Kuyumcu tezgâhında tablet kullanılıyor;
// "çalışmayan sürükleme" = kurucu için özelliğin hiç olmaması demektir.
// Pointer Events ikisini de tek yoldan verir.
//
// EŞİK: 5 piksel. Eşiksiz bir listede menüye TIKLAMAK bile sürükleme sayılır ve
// kullanıcı farkında olmadan sırasını bozar.
//
// ⚠ BU DOSYA KARAR VERMEZ. Yalnız "şunu şunun yerine bırak" olayını .NET'e
//   bildirir; sıra .NET tarafında hesaplanır ve SUNUCUYA yazılır.
// ═══════════════════════════════════════════════════════════════════════════
window.kuyumcuSira = {
  EPSIK: 5,

  /// kap: liste kapsayıcısı; her öğede data-sira-anahtar bulunur.
  /// net: DotNetObjectReference — Birak(kaynak, hedef) çağrılır.
  baslat: function (kapVeyaId, net) {
    const kap = (typeof kapVeyaId === "string") ? document.getElementById(kapVeyaId) : kapVeyaId;
    if (!kap || !net) return;
    if (kap._siraTemizle) kap._siraTemizle();

    let kaynak = null, basX = 0, basY = 0, gecti = false;

    const anahtar = (el) => {
      const o = el && el.closest ? el.closest("[data-sira-anahtar]") : null;
      return o ? o.getAttribute("data-sira-anahtar") : null;
    };

    const bas = (e) => {
      const a = anahtar(e.target);
      if (!a) return;
      kaynak = a; basX = e.clientX; basY = e.clientY; gecti = false;
    };

    const hareket = (e) => {
      if (kaynak === null || gecti) return;
      if (Math.abs(e.clientX - basX) + Math.abs(e.clientY - basY) < window.kuyumcuSira.EPSIK) return;
      gecti = true;                       // eşik aşıldı: artık sürükleme
      kap.classList.add("sira-suruklemede");
      if (e.cancelable) e.preventDefault();
    };

    const birak = (e) => {
      const k = kaynak, esikAsildi = gecti;
      kaynak = null; gecti = false;
      kap.classList.remove("sira-suruklemede");
      if (!k || !esikAsildi) return;      // eşik aşılmadıysa bu bir TIKLAMADIR
      const hedefEl = document.elementFromPoint(e.clientX, e.clientY);
      const h = anahtar(hedefEl);
      if (!h || h === k) return;
      net.invokeMethodAsync("Birak", k, h);
    };

    kap.addEventListener("pointerdown", bas);
    kap.addEventListener("pointermove", hareket, { passive: false });
    kap.addEventListener("pointerup", birak);
    kap.addEventListener("pointercancel", birak);

    kap._siraTemizle = function () {
      kap.removeEventListener("pointerdown", bas);
      kap.removeEventListener("pointermove", hareket);
      kap.removeEventListener("pointerup", birak);
      kap.removeEventListener("pointercancel", birak);
      kap._siraTemizle = null;
    };
  },

  durdur: function (kapVeyaId) {
    const kap = (typeof kapVeyaId === "string") ? document.getElementById(kapVeyaId) : kapVeyaId;
    if (kap && kap._siraTemizle) kap._siraTemizle();
  }
};

// ═══════════════════════════════════════════════════════════════════════════
// PAKET P-37 — KISAYOL: Ctrl/Cmd + K menü arama kutusuna ODAKLANIR
// Ölçülen kusur (21 Eylül): kısayol HİÇ bağlı değildi ve arama açılınca odak
// kutuya gitmiyordu (odak "ust-arama-ac" düğmesinde kalıyordu).
// ═══════════════════════════════════════════════════════════════════════════
window.kuyumcuKisayol = {
  // PAKET S1-j: Ctrl/Cmd+K KAYIT PALETİNİ açar. Palet kapalıyken kutu DOM'da
  // yoktur; o zaman .NET tarafına (GlobalArama.KisayolAc) haber verilir ve
  // bileşen açıldığı çizimde odağı kutuya kendisi taşır.
  aramaKur: function (girdiId, dotnet) {
    if (window._kuyumcuAramaKisayol) document.removeEventListener("keydown", window._kuyumcuAramaKisayol);
    const el = function (e) {
      if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) {
        const g = document.getElementById(girdiId);
        e.preventDefault();
        if (g) { g.focus(); g.select(); return; }
        if (dotnet) dotnet.invokeMethodAsync("KisayolAc");
      }
    };
    document.addEventListener("keydown", el);
    window._kuyumcuAramaKisayol = el;
  },

  odakla: function (id) {
    const g = document.getElementById(id);
    if (g) g.focus();
  }
};
