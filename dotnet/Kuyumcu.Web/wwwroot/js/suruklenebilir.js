// Modal penceresini başlığından tutup sürükleme (mouse + dokunmatik).
// Blazor IJSRuntime ile çağrılır: kuyumcuSurukle.baslat(panel, tutamac)
// Konum transform:translate ile taşınır; böylece flexbox ortalaması bozulmaz.
window.kuyumcuSurukle = {
  baslat: function (panel, tutamac) {
    if (!panel || !tutamac) return;

    // Önceki açılıştan kalan dinleyicileri temizle ve konumu sıfırla
    if (panel._surukleTemizle) panel._surukleTemizle();
    panel.style.transform = "";

    let surukluyor = false;
    let baslangicX = 0, baslangicY = 0;   // basış anındaki fare konumu
    let ofsetX = 0, ofsetY = 0;           // birikmiş toplam kayma

    const nokta = (e) => (e.touches && e.touches[0]) || (e.changedTouches && e.changedTouches[0]) || e;

    const basla = (e) => {
      // Başlıktaki butona (kapat ×) basınca sürükleme başlatma
      if (e.target.closest("button")) return;
      surukluyor = true;
      const n = nokta(e);
      baslangicX = n.clientX;
      baslangicY = n.clientY;
      document.addEventListener("mousemove", hareket);
      document.addEventListener("mouseup", bitir);
      document.addEventListener("touchmove", hareket, { passive: false });
      document.addEventListener("touchend", bitir);
      e.preventDefault();
    };

    const hareket = (e) => {
      if (!surukluyor) return;
      const n = nokta(e);
      const dx = n.clientX - baslangicX;
      const dy = n.clientY - baslangicY;
      panel.style.transform = "translate(" + (ofsetX + dx) + "px, " + (ofsetY + dy) + "px)";
      if (e.cancelable) e.preventDefault();
    };

    const bitir = (e) => {
      if (!surukluyor) return;
      surukluyor = false;
      const n = nokta(e);
      ofsetX += n.clientX - baslangicX;
      ofsetY += n.clientY - baslangicY;
      document.removeEventListener("mousemove", hareket);
      document.removeEventListener("mouseup", bitir);
      document.removeEventListener("touchmove", hareket);
      document.removeEventListener("touchend", bitir);
    };

    tutamac.style.cursor = "move";
    tutamac.addEventListener("mousedown", basla);
    tutamac.addEventListener("touchstart", basla, { passive: false });

    panel._surukleTemizle = () => {
      tutamac.removeEventListener("mousedown", basla);
      tutamac.removeEventListener("touchstart", basla);
      panel._surukleTemizle = null;
    };
  },
};
