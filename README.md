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
- **IHostedService:** Süresi dolan açık artırmalar arka planda otomatik kapatılır, kazanan belirlenir.
- **SignalR presence:** "X kişi izliyor" bilgisi canlı yayınlanır.

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

## Proje Durumu

🚧 Geliştirme aşamasında. Yol haritası için bkz. `realtime-auction-roadmap.md`.

- [x] **Hafta 0** — Solution + 6 proje iskeleti, referans grafiği
- [x] **Hafta 1** — Domain + EF Core + Identity (açık artırma listesi/detay DB'den, kayıt/giriş)
- [ ] **Hafta 2** — Web API + teklif mantığı + concurrency
- [ ] **Hafta 3** — SignalR (canlı teklif/sayaç/presence)
- [ ] **Hafta 4** — Background jobs + testler + deploy
