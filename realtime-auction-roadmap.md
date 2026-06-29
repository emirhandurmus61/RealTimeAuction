# Canlı Açık Artırma Platformu — Yol Haritası

**Stack:** ASP.NET Core MVC + Web API + SignalR + EF Core + Identity + xUnit
**Hedef:** İş ilanı / mülakat vitrini. Real-time'ın yıldız olduğu, 3 teknolojiyi (MVC + API + SignalR) doğal kullanan proje.
**Süre:** 4 hafta (haftada ~10-15 saat varsayımıyla; daha yoğun çalışırsan 2-3 haftaya iner)
**Repo:** /home/emirhan-ubuntu/repos/RealTimeAuction (önerilen)

---

## Çözüm Mimarisi (hedef yapı)

```
RealTimeAuction.sln
├── AuctionHouse.Web            → MVC (sayfalar, viewlar, Razor)
├── AuctionHouse.Api            → Web API controllers (REST)
├── AuctionHouse.Hubs           → SignalR (AuctionHub)
├── AuctionHouse.Core           → Domain (Entities, Interfaces, DTOs)
├── AuctionHouse.Infrastructure → EF Core, DbContext, Repositories, Services
└── AuctionHouse.Tests          → xUnit
```

**Mülakatta anlatılacak teknik vitrin noktaları:**
- Optimistic concurrency (RowVersion) ile eşzamanlı teklif çakışması çözümü
- Server-authoritative geri sayım (client saatine güvenme)
- Anti-sniping (son saniyelerde gelen teklifte süre uzatma)
- IHostedService ile otomatik açık artırma kapanışı
- SignalR connection tracking ile presence ("X kişi izliyor")
- Katmanlı/Clean mimari, dependency injection, xUnit testleri

---

## Hafta 0 — Hazırlık (1 gün)

- [ ] .NET SDK kurulumu (`dotnet bulunamadı` — önce bunu kur): `sudo apt-get install -y dotnet-sdk-8.0` ya da resmi installer
- [ ] `dotnet --version` ile doğrula (.NET 8 LTS hedefle)
- [ ] Git repo başlat: `RealTimeAuction` klasörü + `.gitignore` (dotnet)
- [ ] Boş solution + 6 projeyi oluştur, proje referanslarını bağla
- [ ] README'ye proje vizyonunu yaz (mülakatta repo'yu açan kişi ilk bunu görür)

---

## Hafta 1 — İskelet + Domain + Auth

**Hedef:** Çalışan bir uygulama, giriş yapılabiliyor, ürün/açık artırma veritabanında.

- [ ] **Core katmanı:** Entity'ler — `User`, `Auction`, `Bid`, `Category`
  - `Auction`: Title, Description, StartPrice, CurrentPrice, StartTime, EndTime, Status, `RowVersion` (byte[])
  - `Bid`: AuctionId, UserId, Amount, Timestamp
- [ ] **Infrastructure:** EF Core DbContext, ilişkiler, ilk migration, SQLite ile başla (kurulum kolay), sonra istersen PostgreSQL
- [ ] **Identity:** ASP.NET Core Identity entegrasyonu, roller: `Admin`, `Seller`, `Bidder`
- [ ] **MVC Web:** Layout, ana sayfa, kayıt/giriş sayfaları (Identity UI)
- [ ] Seed data: birkaç örnek açık artırma
- [ ] **Milestone:** Uygulama ayağa kalkıyor, kullanıcı kayıt/giriş yapabiliyor, açık artırma listesi DB'den geliyor.

---

## Hafta 2 — Web API + Teklif Mantığı + Concurrency

**Hedef:** Teklif verme çalışıyor, çakışmalar doğru yönetiliyor (real-time HENÜZ yok, sonra eklenecek).

- [ ] **Api:** REST endpoint'ler
  - `GET /api/auctions`, `GET /api/auctions/{id}`
  - `POST /api/auctions/{id}/bids` (teklif ver)
  - `POST /api/auctions` (seller — yeni açık artırma)
- [ ] **Teklif iş kuralları (Core/Services):**
  - Teklif mevcut fiyattan yüksek olmalı (min artış adımı)
  - Açık artırma aktif ve süresi dolmamış olmalı
  - Kendi açık artırmana teklif veremezsin
- [ ] **Optimistic concurrency:** `RowVersion` ile, iki teklif aynı anda gelince `DbUpdateConcurrencyException` yakala → retry/yeniden değerlendir. **(Mülakat altını)**
- [ ] **Validation:** FluentValidation veya data annotations
- [ ] Swagger/OpenAPI ekle (API'yi demo etmek için)
- [ ] **Milestone:** Postman/Swagger'dan teklif verilebiliyor, eşzamanlı teklif testi çakışmayı doğru çözüyor.

---

## Hafta 3 — SignalR (PROJENİN YILDIZI) ⭐

**Hedef:** Canlı her şey. İki tarayıcı açınca biri teklif verince diğerinde anında güncelleniyor.

- [ ] **Hubs:** `AuctionHub`
  - `JoinAuction(auctionId)` / `LeaveAuction` → SignalR Groups (her açık artırma bir grup)
  - Teklif geldiğinde → gruba `BidPlaced` broadcast (yeni fiyat, teklif veren, zaman)
- [ ] **Entegrasyon:** Teklif API'si başarılı olunca Hub üzerinden ilgili gruba push
- [ ] **Canlı geri sayım:** Server-authoritative — sunucu EndTime'ı gönderir, client sayar, periyodik server senkronu
- [ ] **Anti-sniping:** Son 10 sn'de teklif gelirse EndTime +30 sn uzat, gruba yeni süreyi push et
- [ ] **Presence:** `OnConnectedAsync`/`OnDisconnectedAsync` ile izleyici sayısı, gruba canlı push
- [ ] **Bildirim:** "Teklifin geçildi" — önceki en yüksek teklif sahibine targeted mesaj
- [ ] **MVC tarafı:** Açık artırma detay sayfasına SignalR JS client, canlı fiyat/sayaç/izleyici UI
- [ ] **Milestone:** İki tarayıcıda canlı demo çalışıyor. (Bu, mülakat demo videosu için kaydedilecek an.)

---

## Hafta 4 — Background Jobs + Test + Cila + Deploy

**Hedef:** Profesyonel bitiş. Otomatik kapanış, testler, dökümantasyon, canlı demo.

- [ ] **IHostedService / BackgroundService:** Süresi dolan açık artırmaları periyodik tara → `Status = Ended`, kazananı belirle, gruba `AuctionEnded` push et
- [ ] **xUnit testleri:** Teklif iş kuralları, concurrency senaryosu, anti-sniping mantığı (en kritik 3 alanı test et — mülakatta "test yazıyorum" demek değerli)
- [ ] **Cila:**
  - Hata yönetimi (global exception handler), loglama (Serilog)
  - Temiz UI (Bootstrap yeterli, çok zaman harcama)
  - Kullanıcı paneli: "tekliflerim", "kazandıklarım"
- [ ] **Deploy:** Dockerfile + docker-compose; Railway/Render/Azure'a deploy → canlı link
- [ ] **README:** Mimari diyagram, ekran görüntüleri, demo GIF/video linki, "nasıl çalıştırılır", öne çıkan teknik kararlar bölümü
- [ ] **Milestone:** Canlı link + repo + demo videosu. Mülakata hazır.

---

## Bonus (zaman kalırsa)

- [ ] Resim yükleme (ürün fotoğrafı) — Azure Blob / local storage
- [ ] Otomatik teklif (proxy bidding — max limit belirle, sistem senin için artırsın)
- [ ] Açık artırma kategorileri + arama/filtre
- [ ] Admin paneli: kullanıcı/açık artırma yönetimi
- [ ] Rate limiting (teklif spam'i önle)

---

## Alternatif domain (aynı iskelet, farklı tema)
İskelet birebir aynı kalır, sadece domain değişir:
- **Canlı quiz/yarışma (Kahoot benzeri):** Auction → Quiz, Bid → Answer
- **Canlı spor skoru + tahmin:** Auction → Match, Bid → Prediction
