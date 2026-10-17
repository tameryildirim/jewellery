#!/bin/zsh
# 17.10 — TEMİZ YAYIN: yalnız COMMIT'lenmiş kod yayına girer (iki oturum aynı klasörde çalışırken
# birinin yarım işi ötekinin yayınına karışmasın). Kullanım: zsh yayin-temiz.sh <etiket> [commit=mihenk]
# 1) ~/ByGatech-yerel/jewellery-yayin ayrı çalışma kopyası (git worktree) istenen commit'e alınır
# 2) yayinla.sh KUYUMCU_KAYNAK ile O kopyadan derler; damga o kopyadan ölçülür (.kaynak-kok)
# 3) servis yeniden başlatılır, 5199 sağlığı beklenir.
# ⚠ Geçiş (migration) varsa ÖNCE yedek + `dotnet ef database update` (bekleyen geçişte sunucu açılmaz).
ANA=/Users/ramazanakkocan/ByGatech-yerel/jewellery
KOPYA=/Users/ramazanakkocan/ByGatech-yerel/jewellery-yayin
ETIKET=${1:-temiz}; HEDEF_COMMIT=${2:-mihenk}
cd "$ANA" || exit 1
SHA=$(git rev-parse --verify "$HEDEF_COMMIT^{commit}") || { echo "RET: commit bulunamadı: $HEDEF_COMMIT"; exit 2; }
if [ ! -d "$KOPYA/.git" ] && [ ! -f "$KOPYA/.git" ]; then
  git worktree add --detach "$KOPYA" "$SHA" >/dev/null 2>&1 || { echo "RET: çalışma kopyası açılamadı"; exit 3; }
else
  git -C "$KOPYA" checkout --detach -q --force "$SHA" || { echo "RET: kopya $SHA'ya alınamadı"; exit 3; }
  git -C "$KOPYA" clean -fdq -e bin -e obj
fi
echo "kaynak: $KOPYA @ $(git -C "$KOPYA" log --oneline -1)"
KUYUMCU_KAYNAK="$KOPYA" zsh "$ANA/dokumanlar/09-uygulama-kayitlari/betikler/yayinla.sh" "$ETIKET" 2>&1 | grep -E 'YAYIN HAZIR|RET|HATA' | head -3
launchctl kickstart -k gui/$(id -u)/com.bygatech.mihenk
for i in $(seq 1 40); do sleep 4; C=$(curl -s -o /dev/null -w '%{http_code}' http://localhost:5199/); [ "$C" = 200 ] && break; done
echo "5199=$C · yayın kaynağı $(cat "$HOME/.kuyumcu-yayin/.kaynak-kok" 2>/dev/null) @ ${SHA:0:7}"
