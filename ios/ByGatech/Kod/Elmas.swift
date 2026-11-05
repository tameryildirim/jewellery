import SwiftUI
import SceneKit

/// Gerçek görünümlü 3B amblem: web'deki çizimle aynı taş (TasVerisi), fakat gerçek hacim —
/// cam/pırlanta yüzeyler (fizik tabanlı, stüdyo yansımalı) + web'deki gibi parlayan kenar şeritleri.
/// Zemin şeffaf; renk markadan (açık: koyu gövde + parıltı, koyu: açık gövde).
enum ElmasSahnesi {
    static func yuzeyGeometrisi() -> SCNGeometry {
        var konum: [SCNVector3] = [], normal: [SCNVector3] = [], indis: [Int32] = []
        for (a, b, c) in TasVerisi.ucgenler {
            let pa = TasVerisi.noktalar[a], pb = TasVerisi.noktalar[b], pc = TasVerisi.noktalar[c]
            var n = simd_cross(pb - pa, pc - pa); let L = simd_length(n); if L > 0 { n /= L }
            for p in [pa, pb, pc] { konum.append(SCNVector3(p.x, p.y, p.z)); normal.append(SCNVector3(n.x, n.y, n.z)) }
            let k = Int32(indis.count); indis += [k, k + 1, k + 2]
        }
        let g = SCNGeometry(sources: [SCNGeometrySource(vertices: konum), SCNGeometrySource(normals: normal)],
                            elements: [SCNGeometryElement(indices: indis, primitiveType: .triangles)])
        g.materials = [camMalzeme()]
        return g
    }

    static func camMalzeme() -> SCNMaterial {
        let m = SCNMaterial()
        m.lightingModel = .physicallyBased
        m.diffuse.contents = UIColor(white: 0.96, alpha: 1)
        m.metalness.contents = 0.2
        m.roughness.contents = 0.04
        m.transparency = 0.38
        m.transparencyMode = .dualLayer
        m.isDoubleSided = true
        m.reflective.contents = ortamHaritasi()
        m.reflective.intensity = 2.2
        m.fresnelExponent = 1.3
        m.specular.contents = UIColor.white
        return m
    }

    /// Kenar şeridi: web'deki ince gradyanlı çizgi — burada ince silindir, kendinden ışıklı (parıltı).
    static func kenarMalzeme(renk: UIColor) -> SCNMaterial {
        let m = SCNMaterial()
        m.lightingModel = .constant
        m.diffuse.contents = renk
        m.emission.contents = renk
        m.emission.intensity = 0.9
        return m
    }

    static func kenarDugumu(renk: UIColor, kalinlik: CGFloat) -> SCNNode {
        let kok = SCNNode()
        let malzeme = kenarMalzeme(renk: renk)
        for (a, b) in TasVerisi.kenarlar {
            let pa = TasVerisi.noktalar[a], pb = TasVerisi.noktalar[b]
            let d = pb - pa; let L = simd_length(d)
            let sil = SCNCylinder(radius: kalinlik, height: CGFloat(L)); sil.radialSegmentCount = 8; sil.materials = [malzeme]
            let n = SCNNode(geometry: sil)
            let orta = (pa + pb) / 2; n.position = SCNVector3(orta.x, orta.y, orta.z)
            // Silindir y ekseni → kenar yönü
            let y = SIMD3<Float>(0, 1, 0), v = d / L
            let eksen = simd_cross(y, v); let s = simd_length(eksen); let c = simd_dot(y, v)
            if s > 1e-5 { n.rotation = SCNVector4(eksen.x / s, eksen.y / s, eksen.z / s, atan2(s, c)) } else if c < 0 { n.rotation = SCNVector4(1, 0, 0, Float.pi) }
            kok.addChildNode(n)
            let kure = SCNNode(geometry: { let k = SCNSphere(radius: kalinlik); k.materials = [malzeme]; return k }())
            kure.position = SCNVector3(pa.x, pa.y, pa.z); kok.addChildNode(kure)
        }
        return kok
    }

    /// Stüdyo ortam haritası: koyu zemin üstünde parlak ışık kutuları → yüzeylerde keskin parıltılar.
    static func ortamHaritasi() -> UIImage {
        let boy = CGSize(width: 512, height: 256)
        return UIGraphicsImageRenderer(size: boy).image { ctx in
            let c = ctx.cgContext
            let grad = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(), colors: [UIColor(white: 0.95, alpha: 1).cgColor, UIColor(white: 0.3, alpha: 1).cgColor, UIColor(white: 0.06, alpha: 1).cgColor] as CFArray, locations: [0, 0.55, 1])!
            c.drawLinearGradient(grad, start: .zero, end: CGPoint(x: 0, y: boy.height), options: [])
            c.setFillColor(UIColor.white.cgColor)
            for (x, w) in [(40, 70), (180, 40), (300, 90), (440, 30)] { c.fill(CGRect(x: x, y: 20, width: w, height: 70)) }
            c.setFillColor(UIColor(white: 0.85, alpha: 1).cgColor)
            for (x, w) in [(110, 30), (250, 20), (400, 25)] { c.fill(CGRect(x: x, y: 150, width: w, height: 40)) }
        }
    }

    /// koyu: arayüz koyu modda mı (kenar rengi tersine döner: açıkta koyu çizgi, koyuda açık çizgi — web gibi).
    static func sahne(donuyor: Bool, koyu: Bool, kenarKalinligi: CGFloat) -> SCNScene {
        let sahne = SCNScene()
        sahne.background.contents = UIColor.clear
        sahne.lightingEnvironment.contents = ortamHaritasi()
        sahne.lightingEnvironment.intensity = 1.6
        let tas = SCNNode()
        tas.addChildNode(SCNNode(geometry: yuzeyGeometrisi()))
        let kenarRenk = koyu ? UIColor(red: 0.953, green: 0.949, blue: 0.937, alpha: 1) : UIColor(red: 0.078, green: 0.078, blue: 0.078, alpha: 1)
        tas.addChildNode(kenarDugumu(renk: kenarRenk, kalinlik: kenarKalinligi))
        tas.eulerAngles = SCNVector3(-0.28, 0, 0)
        // Web'deki "parlama": 11 sn döngüde kenar ışığı kısa süre yükselir.
        if let km = tas.childNodes.last?.childNodes.first?.geometry?.firstMaterial {
            let parla = SCNAction.customAction(duration: 11) { _, t in
                let f = Double(t) / 11; let z = f > 0.14 && f < 0.24 ? 1 - abs((f - 0.19) / 0.05) : 0
                km.emission.intensity = 0.9 + CGFloat(z) * 1.6
            }
            tas.runAction(.repeatForever(parla))
        }
        let doner = SCNNode(); doner.addChildNode(tas)
        if donuyor { doner.runAction(.repeatForever(.rotateBy(x: 0, y: 2 * .pi, z: 0, duration: 11))) }
        sahne.rootNode.addChildNode(doner)
        let kamera = SCNNode(); kamera.camera = SCNCamera(); kamera.camera?.fieldOfView = 26
        kamera.position = SCNVector3(0, 0.15, 6.2); sahne.rootNode.addChildNode(kamera)
        let anahtar = SCNNode(); anahtar.light = SCNLight(); anahtar.light?.type = .directional; anahtar.light?.intensity = 800
        anahtar.eulerAngles = SCNVector3(-0.7, 0.6, 0); sahne.rootNode.addChildNode(anahtar)
        let dolgu = SCNNode(); dolgu.light = SCNLight(); dolgu.light?.type = .omni; dolgu.light?.intensity = 400
        dolgu.position = SCNVector3(-3, 2, 3); sahne.rootNode.addChildNode(dolgu)
        return sahne
    }
}

/// SwiftUI sarmalı: şeffaf zemin, dönen taş; koyu/açık moda göre kenar rengi.
struct ElmasGorunumu: UIViewRepresentable {
    var donuyor = true
    var kenarKalinligi: CGFloat = 0.022
    @Environment(\.colorScheme) private var sema

    func makeUIView(context: Context) -> SCNView {
        let v = SCNView()
        v.backgroundColor = .clear; v.isOpaque = false
        v.antialiasingMode = .multisampling4X
        v.autoenablesDefaultLighting = false
        v.isUserInteractionEnabled = false
        v.preferredFramesPerSecond = donuyor ? 60 : 1
        v.scene = ElmasSahnesi.sahne(donuyor: donuyor, koyu: sema == .dark, kenarKalinligi: kenarKalinligi)
        v.isPlaying = donuyor
        context.coordinator.koyu = sema == .dark
        return v
    }

    func updateUIView(_ v: SCNView, context: Context) {
        let koyu = sema == .dark
        if context.coordinator.koyu != koyu {
            context.coordinator.koyu = koyu
            v.scene = ElmasSahnesi.sahne(donuyor: donuyor, koyu: koyu, kenarKalinligi: kenarKalinligi)
        }
    }

    func makeCoordinator() -> Koordinator { Koordinator() }
    final class Koordinator { var koyu = false }
}
