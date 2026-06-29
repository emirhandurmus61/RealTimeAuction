# RealTimeAuction — Canlı Açık Artırma Platformu

Gerçek zamanlı, çok kullanıcılı bir açık artırma platformu. Bir kullanıcı teklif verdiği anda, o açık artırmayı izleyen herkesin ekranında fiyat, geri sayım ve izleyici sayısı **anlık** olarak güncellenir.

> Bu proje; ASP.NET Core ekosisteminde **MVC + Web API + SignalR** üçlüsünü, katmanlı (Clean) mimari ve eşzamanlılık kontrolüyle birlikte doğal bir senaryoda kullanmak için tasarlanmış bir vitrin projesidir.

## Teknoloji Yığını

- **ASP.NET Core 8 (MVC)** — sayfalar, Razor view'ları
- **ASP.NET Core Web API** — REST endpoint'leri
- **SignalR** — gerçek zamanlı teklif/sayaç/presence yayını
- **EF Core** — veri erişimi (SQLite ile başlar)
- **ASP.NET Core Identity** — kimlik doğrulama, roller (`Admin`, `Seller`, `Bidder`)
- **xUnit** — birim testleri

## Öne Çıkan Teknik Kararlar

- **Optimistic concurrency (RowVersion):** Aynı anda gelen iki teklifin çakışması `DbUpdateConcurrencyException` ile yakalanıp doğru çözülür.
- **Server-authoritative geri sayım:** İstemci saatine güvenilmez; süreyi sunucu belirler.
- **Anti-sniping:** Son saniyelerde gelen teklifte bitiş süresi otomatik uzatılır.
- **IHostedService (BackgroundService):** Süresi dolan açık artırmalar arka planda periyodik taranır, `Status = Ended` yapılır, kazanan belirlenir ve gruba `AuctionEnded` yayınlanır.
- **SignalR presence:** "X kişi izliyor" bilgisi canlı yayınlanır.
- **Temiz mimari soyutlaması:** `IAuctionNotifier` sayesinde Infrastructure katmanı SignalR'a doğrudan bağımlı değildir; canlı bildirim implementasyonunu Hubs katmanı sağlar.
- **Serilog:** Yapılandırılabilir loglama (konsol + günlük dosyaları), HTTP istek özeti.

## Çözüm Mimarisi

```
RealTimeAuction.sln
├── AuctionHouse.Core            → Domain (Entities, Interfaces, DTOs)
├── AuctionHouse.Infrastructure  → EF Core, DbContext, Repositories, Services
├── AuctionHouse.Hubs            → SignalR (AuctionHub)
├── AuctionHouse.Api             → Web API controllers (REST)
├── AuctionHouse.Web             → MVC (sayfalar, Razor view'ları)
└── AuctionHouse.Tests           → xUnit
```

## Çalıştırma

```bash
dotnet build
dotnet run --project AuctionHouse.Web
# http://localhost:5017
```

İlk çalıştırmada veritabanı otomatik oluşturulur ve örnek veriyle doldurulur
(roller, kategoriler, 3 aktif açık artırma). Hazır örnek satıcı hesabı:

```
E-posta: seller@auction.local
Parola:  Seller123!
```

### Docker ile çalıştırma

```bash
docker compose up --build
# http://localhost:8080
```

SQLite veritabanı ve loglar adlandırılmış volume'larda kalıcı tutulur.

### REST API (Swagger)

```bash
dotnet run --project AuctionHouse.Api
# Swagger UI: http://localhost:<port>/swagger
```

Başlıca endpoint'ler:

| Method | Route | Açıklama | Yetki |
|--------|-------|----------|-------|
| GET  | `/api/auctions`           | Açık artırma listesi      | herkes |
| GET  | `/api/auctions/{id}`      | Detay + teklif geçmişi    | herkes |
| POST | `/api/auctions`           | Yeni açık artırma         | Seller/Admin |
| POST | `/api/auctions/{id}/bids` | Teklif ver                | giriş yapmış |
| POST | `/register`, `/login`     | Identity (bearer token)   | herkes |

Teklif iş kuralları: teklif `güncel fiyat + min artış` değerinden düşük olamaz,
açık artırma aktif/süresi geçmemiş olmalı, satıcı kendi artırmasına teklif veremez.
Eşzamanlı teklifler `RowVersion` (optimistic concurrency) + sınırlı retry ile çözülür.

### Canlı (SignalR) demo

`AuctionHouse.Web`'i çalıştırıp bir açık artırma detay sayfasını **iki ayrı tarayıcıda** aç.
Birinde teklif verince diğerinde anında güncellenenler:

- **Güncel fiyat** ve **teklif geçmişi** (gruba `BidPlaced` yayını)
- **İzleyici sayısı** ("👁 N izliyor" — `OnConnectedAsync`/`OnDisconnectedAsync` presence)
- **Geri sayım** sunucudan gelen `EndTime`'a göre (server-authoritative; istemci saatine güvenmez)
- **Anti-sniping:** son 10 sn'de teklif gelirse bitiş +30 sn uzar, sayaç senkronlanır
- **"Teklifin geçildi"** uyarısı — önceki en yüksek teklif sahibine targeted mesaj

Hub endpoint'i: `/hubs/auction` · JS client: `wwwroot/lib/signalr/signalr.min.js`

### Testler

```bash
dotnet test
```

8 test: teklif iş kuralları, optimistic concurrency çözümü, anti-sniping uzatması,
outbid bildirimi (gerçek SQLite in-memory ile).

## Proje Durumu

🚧 Geliştirme aşamasında. Yol haritası için bkz. `realtime-auction-roadmap.md`.

- [x] **Hafta 0** — Solution + 6 proje iskeleti, referans grafiği
- [x] **Hafta 1** — Domain + EF Core + Identity (açık artırma listesi/detay DB'den, kayıt/giriş)
- [x] **Hafta 2** — Web API (REST + Swagger + bearer auth), teklif iş kuralları, optimistic concurrency + retry, xUnit testleri
- [x] **Hafta 3** — SignalR ⭐ (canlı teklif yayını, server-authoritative geri sayım, anti-sniping, presence, outbid bildirimi)
- [x] **Hafta 4** — Otomatik kapanış (BackgroundService), Serilog, kullanıcı paneli, Docker
