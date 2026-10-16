# Yerel inceleme vekili: 127.0.0.1:5179 → 5181. /__giris test hesabinin jetonlarini
# tarayiciya SUNUCU TARAFINDA verir (jeton hicbir ciktiya yazilmaz). Yalniz dongu arayuzu.
import http.server, socketserver, urllib.request, json, sys
# Kullanim: python3 inceleme-vekili.py <fikstur.json> <hedef_port> <vekil_port>
# fikstur.json: {email, sifre} (chmod 600; test kiracisi). Tarayicida: http://localhost:<vekil_port>/__giris
FIK=sys.argv[1]; HEDEF='http://localhost:%s'%sys.argv[2]; VPORT=int(sys.argv[3])
def jeton():
    d=json.load(open(FIK))
    r=urllib.request.urlopen(urllib.request.Request(HEDEF+'/api/v1/auth/giris',data=json.dumps({'email':d['email'],'sifre':d['sifre']}).encode(),headers={'Content-Type':'application/json'}),timeout=15)
    return json.loads(r.read())
class H(http.server.BaseHTTPRequestHandler):
    protocol_version='HTTP/1.1'
    def log_message(self,*a): pass
    def _ilet(self):
        if self.path.startswith('/__giris'):
            b=jeton(); h=('<!doctype html><script>localStorage.setItem("kuyumcu-access",%s);localStorage.setItem("kuyumcu-refresh",%s);location.replace("/");</script>'%(json.dumps(b['accessToken']),json.dumps(b['refreshToken']))).encode()
            self.send_response(200); self.send_header('Content-Type','text/html; charset=utf-8'); self.send_header('Content-Length',str(len(h))); self.send_header('Cache-Control','no-store'); self.end_headers(); self.wfile.write(h); return
        n=int(self.headers.get('Content-Length') or 0); govde=self.rfile.read(n) if n else None
        bas={k:v for k,v in self.headers.items() if k.lower() not in ('host','connection','accept-encoding','content-length')}
        req=urllib.request.Request(HEDEF+self.path,data=govde,headers=bas,method=self.command)
        try: r=urllib.request.urlopen(req,timeout=60); kod=r.status; hb=r.headers; v=r.read()
        except urllib.error.HTTPError as e: kod=e.code; hb=e.headers; v=e.read()
        self.send_response(kod)
        for k,val in hb.items():
            if k.lower() in ('transfer-encoding','connection','content-length'): continue
            self.send_header(k,val)
        self.send_header('Content-Length',str(len(v))); self.end_headers(); self.wfile.write(v)
    do_GET=do_POST=do_PATCH=do_PUT=do_DELETE=do_HEAD=_ilet
class S(socketserver.ThreadingMixIn, http.server.HTTPServer): daemon_threads=True
S(('127.0.0.1',VPORT),H).serve_forever()
