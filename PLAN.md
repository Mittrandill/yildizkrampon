# Yıldız Krampon — Yeni Mimari Plan

> **Hedef:** Stardew Valley tarzı yaşayan dünya + RPG futbol oyunu.
> Lineer sahne zinciri tamamen kaldırılıyor; açık harita + serbest yürüyüş + NPC rutinleri + gelişmiş futbol maçı yapılıyor.

---

## Oyun Döngüsü

```
Karakter Oluştur → Uyandır (ev yatak odası) → Serbest Keşif
    ↓
Günlük seçimler: Ye / Egzersiz / Maç Oyna / Antren / Sohbet / Oku / Uyu
    ↓
Stat kazanımı → Geceyi geç → Ertesi gün
    ↓
Kariyer: Mahalle → Gençlik Takımı → Akademi Denemesi → Pro
```

---

## Temel Sistemler

### 1. Karakter Oluşturma (CharacterCreation)
- İsim girişi (klavye, TextEdit)
- Görünüm seçimi: ten rengi, saç rengi, forma rengi
- PlayerData autoload'a kaydedilir, tüm sahnelerde kullanılır
- Oyun ilk açıldığında 1 kez çalışır, kaydedilir

### 2. Dünya Haritası (WorldMap)
- Top-down pixel art, TileMap tabanlı mahalle
- Serbestçe yürünebilir sokaklar
- **Konumlar (tıklanabilir bölgeler / kapılar):**
  - Yıldız'ın Evi
  - Rıza Bakkal
  - Mahalle Sahası (sokak futbolu sahası)
  - Atatürk İlkokulu
  - Çınar Çay Bahçesi
  - Fitness Salonu
  - Yıldız Spor Tesisleri
  - Deniz kenarı / iskele
- Kapıya yaklaşınca "E: Gir" prompt → fade geçiş → iç mekan

### 3. Ev İç Mekanı (HomeInterior)
- 4 oda gezilebilir: Yatak Odası, Mutfak, Salon, Banyo
- Oda geçişleri kapılarla, fade yok (anlık)
- **İnteraktif objeler:**
  - Yatak: Uyu → sabah (enerji +60, fatigue sıfırla, gün geç)
  - Çalışma masası: Oku → Teknik +1 (1 saat)
  - Buzdolabı: Yemek seç → Enerji +
  - Banyo: Duş al → Fatigue -10
- Anne/Baba rutinlerine göre odada olup olmayabilir
- Kapı → sokağa çıkış → WorldMap'e dön

### 4. Zaman Sistemi (GameTime autoload)
```
1 gerçek saniye = 10 oyun dakikası
Başlangıç: 07:30
Bitiş günü: 22:00 (uyuma zamanı)
```
- **Saat dilimleri:**
  - 06:00–10:00 Sabah
  - 10:00–14:00 Öğle
  - 14:00–18:00 Öğleden sonra
  - 18:00–22:00 Akşam
  - 22:00+ Gece (uyuma zorla veya geç kalma cezası)
- Aktiviteler zaman tüketir:
  - Yemek yeme: 30 dakika
  - Antrenman: 2 saat
  - Mahalle maçı: 3 saat
  - Uyuma: → 07:30 ertesi sabah
  - Sohbet: 15 dakika
- HUD'da saat + gün + hava durumu (ileride)

### 5. NPC Zamanlayıcı (NPCScheduler autoload)
Her NPC'nin saatlik konumu ve durumu:

| NPC | 06-08 | 08-12 | 12-14 | 14-18 | 18-22 |
|-----|-------|-------|-------|-------|-------|
| Anne | Mutfak | Market | Ev | Ev | Salon |
| Baba | Ev | İş | İş | İş | Ev |
| Eren | Ev | Okul | Okul | Mahalle Sahası | Ev |
| Baran | Ev | Okul | Okul | Çay Bahçesi | Ev |
| Kemal Hoca | Fitness | Fitness | Spor Tesisi | Mahalle Sahası | Ev |
| Rıza Abi | Bakkal | Bakkal | Bakkal | Bakkal | Bakkal |

- NPC mevcut konumda değilse → "şu an burada değil"
- NPC'ye yaklaşıp E basınca → diyalog

### 6. Aktivite Sistemi
Oyuncu mekan/objeyle etkileşince aktivite başlar:

| Aktivite | Mekan | Stat Etkisi | Süre |
|----------|-------|-------------|------|
| Uyuma | Yatak | Enerji +60, Fatigue -40 | Sabaha atlar |
| Kahvaltı | Mutfak | Enerji +20, Morale +5 | 30 dk |
| Protein bar | Bakkal/Mutfak | Enerji +25, ShotPower +1 | 15 dk |
| Duş | Banyo | Fatigue -15, Morale +5 | 30 dk |
| Ders çalışma | Masa | Teknik +1, Enerji -5 | 1 saat |
| Fitness | Fitness Salonu | Sprint +1, Fatigue +20 | 2 saat |
| Şut antreni | Spor Tesisi | ShotPower +2, Fatigue +15 | 2 saat |
| Mahalle maçı | Mahalle Sahası | Overall +0.5, tüm statlar etkilenir | 3 saat |
| Sohbet (Eren) | Her yerde | Morale +5 | 15 dk |
| Sohbet (Baran) | Her yerde | Rakip ilişki +1 | 15 dk |
| Sohbet (Kemal) | Saha/Spor Tesisi | İpucu + Antrenman bonus | 15 dk |
| Çay bahçesi | Çay Bahçesi | Morale +10, Enerji -5 | 1 saat |

### 7. Gelişmiş Futbol Maçı
Mevcut 5v5 sistemi üzerine:

**Yeni Kontroller:**
- **WASD/Yön tuşları**: Hareket
- **ŞUT (Space)**: Yakındaki topu fırlatır (güç = ShotPower stat)
- **PAS (F)**: En iyi konumdaki takım arkadaşına pas ver
- **DEPAR (Shift)**: Sprint (Stamina harcar)
- **Taktik İpuçları**: Oyun durumuna göre "THROUGH PASS", "ŞUTA GEÇ" gibi öneriler

**Yeni HUD:**
- Skor + süre (üst merkez)
- Mini harita (sol alt) — tüm oyuncuları gösterir, top sarı nokta
- Oyuncu puanı (sağ üst, maç sonunda güncellenir) — 1.0-10.0
- Sprint barı (alt)
- Enerji barı (üst sol)

**Maç Sonrası:**
- Oyuncu puanı (7.2 gibi)
- Öne çıkan aksiyon ("2 GOL ATTINIZ!", "MAÇIN ADAMI")
- Stat kazanımı özeti

### 8. Progression & Kariyer
**Stat sistemi (mevcut + yeni):**
- Enerji, Moral, Yorgunluk (günlük)
- Şut Gücü, Sprint, Teknik, Dayanıklılık (yetenek — kalıcı)
- Futbol IQ (yeni — takım arkadaşı AI kalitesini etkiler)
- GENEL rating (0-100)
- İlişki puanları: Anne, Baba, Eren, Baran, Kemal Hoca, Rıza Abi

**Kariyer basamakları:**
1. Mahalle çocuğu (Overall 0-45)
2. Dikkat çekti (Overall 45-60, Kemal Hoca fark etti)
3. Gençlik takımı (Overall 60-70)
4. Akademi denemesi (Overall 70+, trigger event)
5. Pro yol başlangıcı (uzun dönem hedef)

---

## Sahne Mimarisi

```
scenes/
  CharacterCreation.tscn   ← İlk açılış, bir kez
  WorldMap.tscn            ← Ana dünya, serbest yürüyüş
  HomeInterior.tscn        ← 4 oda, kapı geçişleriyle
  BakkalInterior.tscn      ← Rıza Abi'nin dükkanı
  SporTesisiInterior.tscn  ← Antrenman tesisi
  FitnessSalonu.tscn       ← Fitness salonu
  CayBahcesi.tscn          ← Çay bahçesi, sohbet yeri
  Match.tscn               ← Futbol maçı (yeniden yazılacak)
```

---

## Autoload (Singleton) Listesi

| Singleton | Dosya | Görev |
|-----------|-------|-------|
| PlayerData | scripts/PlayerData.cs | İsim, görünüm, kayıt sistemi |
| GameManager | scripts/GameManager.cs | Statlar, envanter, ilişkiler |
| GameTime | scripts/GameTime.cs | Saat, gün, zaman geçişi |
| NPCScheduler | scripts/NPCScheduler.cs | NPC konumları ve rutinleri |
| WorldManager | scripts/WorldManager.cs | Konum geçişleri (SequenceManager yerine) |
| DialogueManager | scripts/DialogueManager.cs | Diyalog UI (mevcut, küçük güncelleme) |

---

## Mevcut Koddan Korunacaklar

| Dosya | Durum |
|-------|-------|
| scripts/DialogueManager.cs | Korunuyor |
| scripts/Football.cs | Korunuyor |
| scripts/FootballAI.cs | Güncelleniyor (pas AI) |
| scripts/PlayerController.cs | Güncelleniyor (sprint, pas) |
| scripts/GameManager.cs | Güncelleniyor (ilişkiler, envanter) |
| assets/img/* | Tüm görseller korunuyor |

## Silinecekler

| Dosya | Neden |
|-------|-------|
| scripts/SequenceManager.cs | WorldManager ile değiştiriliyor |
| scripts/BedroomScene.cs | HomeInteriorScene ile birleşiyor |
| scripts/KitchenScene.cs | HomeInteriorScene ile birleşiyor |
| scripts/NeighborhoodScene.cs | WorldMap'a entegre oluyor |
| scripts/PostMatchScene.cs | Maç sonrası sistem değişiyor |
| scripts/ShopScene.cs | BakkalInterior'a dönüşüyor |
| scripts/TrainingScene.cs | SporTesisi aktivite sistemine dönüşüyor |
| scripts/EodSummaryScene.cs | GameTime + gece sistemiyle değiştiriliyor |
| scripts/NextMorningScene.cs | Gün geçişi sistemiyle değiştiriliyor |
| scenes/Build*.cs (hepsi) | Yeni builder'larla değiştiriliyor |

---

## İnşa Sırası (Risk → Core → Polish)

| # | Görev | Öncelik | Tahmini Karmaşıklık |
|---|-------|---------|---------------------|
| 1 | PlayerData + CharacterCreation sahnesi | Kritik | Orta |
| 2 | GameTime sistemi | Kritik | Düşük |
| 3 | WorldManager (WorldMap geçişleri) | Kritik | Orta |
| 4 | WorldMap.tscn (mahalle haritası, yürüyüş) | Kritik | Yüksek |
| 5 | HomeInterior.tscn (4 oda, etkileşimler) | Yüksek | Orta |
| 6 | NPCScheduler + NPC karakterleri | Yüksek | Orta |
| 7 | Aktivite sistemi (uy, ye, antren) | Yüksek | Orta |
| 8 | Gelişmiş futbol maçı (PAS/ŞUT/DEPAR + mini harita) | Yüksek | Yüksek |
| 9 | Diğer iç mekanlar (bakkal, spor tesisi, fitness, çay bahçesi) | Orta | Orta |
| 10 | Kariyer sistemi + progression events | Orta | Orta |
| 11 | Kayıt/yükleme sistemi | Orta | Düşük |
| 12 | Müzik + SFX | Düşük | Düşük |

---

## Task Durumu

| # | Görev | Durum |
|---|-------|-------|
| 1 | PlayerData + CharacterCreation | [x] tamamlandı |
| 2 | GameTime sistemi | [x] tamamlandı |
| 3 | WorldManager | [x] tamamlandı |
| 4 | WorldMap.tscn | [x] tamamlandı |
| 5 | HomeInterior.tscn | [x] tamamlandı |
| 6 | NPCScheduler | [x] tamamlandı |
| 7 | Aktivite sistemi | [x] tamamlandı (ev + tüm mekanlar) |
| 8 | Gelişmiş futbol maçı | [x] tamamlandı |
| 9 | Diğer iç mekanlar | [x] tamamlandı (Bakkal, SporTesisi, Fitness, CayBahcesi) |
| 10 | Kariyer + events | [ ] bekliyor |
| 11 | Kayıt/yükleme | [ ] bekliyor |
| 12 | Müzik + SFX | [ ] bekliyor |
