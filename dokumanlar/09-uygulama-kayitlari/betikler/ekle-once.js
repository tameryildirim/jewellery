// Bir metin dosyasının içeriğini, hedef dosyada işaretle başlayan İLK satırın hemen önüne ekler (UTF-8 güvenli; perl çift kodlama tuzağı yok).
// Kullanım: node ekle-once.js <hedef> <isaret-satir-baslangici> <metin-dosyasi>
const fs = require('fs');
const [,, hedef, isaret, metinDosyasi] = process.argv;
if (!hedef || !isaret || !metinDosyasi) { console.error('kullanım: node ekle-once.js <hedef> <isaret> <metin>'); process.exit(2); }
const satirlar = fs.readFileSync(hedef, 'utf8').split('\n');
const i = satirlar.findIndex(s => s.startsWith(isaret));
if (i < 0) { console.error('işaret bulunamadı: ' + isaret); process.exit(1); }
const metin = fs.readFileSync(metinDosyasi, 'utf8').replace(/\n+$/, '');
satirlar.splice(i, 0, ...metin.split('\n'), '');
fs.writeFileSync(hedef, satirlar.join('\n'));
console.log(`eklendi: ${hedef} satır ${i + 1} öncesine ${metin.split('\n').length} satır`);
