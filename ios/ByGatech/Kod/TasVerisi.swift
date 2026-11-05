import simd

/// Web amblemiyle (YildizUstMenu.AmblemTasi + tema-yildiz.css .amb-f0…41) BİREBİR aynı taş:
/// CSS matrix3d şeritlerinden çözülen 11 köşe, 26 kenar, dışbükey zarf 20 üçgen. Birim: kuşak yarıçapı ≈ 1.
enum TasVerisi {
    static let noktalar: [SIMD3<Float>] = [
        SIMD3<Float>(-0.6745, 0.8125, -0.0147),
        SIMD3<Float>(0.0147, 0.8125, 0.6744),
        SIMD3<Float>(0.6744, 0.8125, -0.0147),
        SIMD3<Float>(-0.4691, 0.3869, 0.8676),
        SIMD3<Float>(0.4881, 0.3827, 0.8554),
        SIMD3<Float>(-0.9828, 0.3958, -0.018),
        SIMD3<Float>(0.9827, 0.3958, -0.0179),
        SIMD3<Float>(0.0063, -0.8006, -0.0115),
        SIMD3<Float>(-0.4691, 0.3869, -0.8676),
        SIMD3<Float>(0.0147, 0.8125, -0.6744),
        SIMD3<Float>(0.488, 0.3827, -0.8554),
    ]
    static let kenarlar: [(Int, Int)] = [
        (0, 1), (1, 2), (0, 3), (1, 3), (1, 4), (2, 4),
        (5, 3), (3, 4), (4, 6), (3, 7), (4, 7), (5, 0),
        (2, 6), (5, 7), (6, 7), (0, 8), (0, 9), (9, 8),
        (9, 10), (9, 2), (2, 10), (5, 8), (8, 10), (10, 6),
        (8, 7), (10, 7),
    ]
    static let ucgenler: [(Int, Int, Int)] = [
        (0, 1, 2), (0, 3, 1), (0, 1, 9), (0, 2, 9), (0, 5, 3),
        (0, 8, 5), (0, 9, 8), (1, 4, 2), (1, 2, 9), (1, 3, 4),
        (2, 4, 6), (2, 6, 10), (2, 10, 9), (3, 7, 4), (3, 5, 7),
        (4, 7, 6), (5, 8, 7), (6, 7, 10), (7, 8, 10), (8, 9, 10),
    ]
}
