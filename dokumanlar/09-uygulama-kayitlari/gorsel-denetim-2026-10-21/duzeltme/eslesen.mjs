import { baslat } from '/Users/ramazanakkocan/ByGatech-yerel/jewellery/dokumanlar/09-uygulama-kayitlari/betikler/tur/cdp.mjs';
const S = new URL('.', import.meta.url).pathname.replace(/\/$/, ''); const bek = ms => new Promise(r => setTimeout(r, ms));
const [V, R, ACICI, SEL] = process.argv.slice(2);
const b = await baslat(9979, S + '/profil-sahip'); await b.git(V + '/__giris', 6000); await b.boyut(1280, 900);
await b.ev(`Blazor.navigateTo(${JSON.stringify(R)}); 1`); await bek(3500);
if (ACICI) { await b.ev(`(async()=>{const t=document.getElementById(${JSON.stringify(ACICI)}); t.scrollIntoView({block:'center'}); t.click(); return 1})()`); await bek(800); }
await b.cmd('DOM.enable'); await b.cmd('CSS.enable');
const { root } = await b.cmd('DOM.getDocument', { depth: 0 }); const { nodeId } = await b.cmd('DOM.querySelector', { nodeId: root.nodeId, selector: SEL });
const m = await b.cmd('CSS.getMatchedStylesForNode', { nodeId });
const ILGI = /^(top|bottom|left|right|inset|inset-block-start|inset-block-end|translate|transform|position|animation|animation-name)$/;
for (const r of m.matchedCSSRules || []) { const st = r.rule.style; const p = st.cssProperties.filter(x => ILGI.test(x.name) && !x.disabled && x.text); if (p.length) console.log((r.rule.origin) + ' ' + (r.rule.styleSheetId || '') + ' ' + r.rule.selectorList.text.slice(0, 100) + ' => ' + p.map(x => x.text).join(' ')); }
console.log('inline:', JSON.stringify((m.inlineStyle?.cssProperties || []).map(x => x.text)));
for (const a of m.cssKeyframesRules || []) console.log('keyframes', a.animationName.text);
b.kapat();
