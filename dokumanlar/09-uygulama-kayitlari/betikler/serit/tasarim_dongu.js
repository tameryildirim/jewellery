    if (dogrulama.karar === 'GECTI' && p.ekran !== false) {
      phase('Tasarim')
      const th = await agent(tasarimPrompt(p), { label: `tasarim:p${p.no}:t${tur}`, phase: 'Tasarim', model: 'opus', effort: 'medium', schema: TASARIM })
      dogrulama.tasarim = th
      const agir = th && th.bulgular ? th.bulgular.filter(b => ['KRITIK', 'YUKSEK'].includes(String(b.ciddiyet).toUpperCase()) && !String(b.baslik).startsWith('KAPSAM DIŞI')) : []
      if (!th || th.karar !== 'GECTI' || (th.gorulen_goruntu || 0) < 12 || agir.length) {
        dogrulama.karar = 'KALDI'
        dogrulama.bulgular = dogrulama.bulgular.concat(th ? agir.map(b => ({ baslik: `TASARIM HAKEMİ — ${b.baslik}`, ciddiyet: 'KRITIK', kanit: b.kanit, oneri: b.oneri || '' })) : [{ baslik: 'TASARIM HAKEMİ dönmedi', ciddiyet: 'KRITIK', kanit: 'ajan sonucu yok', oneri: 'yeniden koş' }])
        if (th && (th.gorulen_goruntu || 0) < 12) dogrulama.bulgular.push({ baslik: 'TASARIM HAKEMİ yeterli görüntüye bakmadı', ciddiyet: 'KRITIK', kanit: `gorulen_goruntu=${th.gorulen_goruntu}`, oneri: 'en az 12 görüntü' })
        log(`Paket ${p.no} tasarım hakemi turu ${tur}: KALDI (ağır ${agir.length}, görüntü ${th ? th.gorulen_goruntu : 0})`)
      } else log(`Paket ${p.no} tasarım hakemi turu ${tur}: GECTI (görüntü ${th.gorulen_goruntu})`)
    }
