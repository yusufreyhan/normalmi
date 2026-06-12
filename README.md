# Normal mi? 🧾

**Ödediğin fiyat gerçekten normal mi? Hemen öğren.**

<div align="center">

## 🌐 [normalmi.onrender.com](https://normalmi.onrender.com)

</div>

[![Live](https://img.shields.io/badge/🌐_Canlı_Site-normalmi.onrender.com-4CAF50?style=for-the-badge)](https://normalmi.onrender.com)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=flat-square&logo=docker)](./Dockerfile)

---

## Ne yapıyor?

Markette, kasap tezgahında ya da dijital mağazada gördüğün fiyatın piyasaya göre ucuz mu, normal mi, pahalı mı olduğunu söyleyen bir web uygulaması.

Şu an iki kategori aktif:

- **Meyve & Sebze** — Türkiye Hal fiyatlarıyla (hal.gov.tr) karşılaştırma
- **Bilgisayar Oyunları** — CheapShark API üzerinden gerçek zamanlı en düşük fiyat arama

---

## Nasıl çalışıyor?

### Meyve & Sebze
Her gün saat **04:00**'te bir arka plan servisi (`HalBackgroundService`) otomatik olarak hal.gov.tr'den günlük fiyat verilerini çeker. Veriler önce XLS (HTML tablo) formatında indirilir, ayrıştırılır ve belleğe alınır. İstek geldiğinde veri bu önbellekten döner; %15 tolerans ile "Ucuz / Normal / Pahalı" kararı verilir.

### Bilgisayar Oyunları
Kullanıcı oyun adını arar → CheapShark API'den gerçek zamanlı fiyatlar çekilir → mağaza bazlı karşılaştırma yapılır → kullanıcının girdiği fiyat ile piyasa ortalaması %10 toleransla karşılaştırılır. Oyun başına deal sonuçları 6 saat boyunca önbellekte tutulur.

---

## Teknik stack

| Katman | Teknoloji |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8) |
| Frontend | Vanilla JS, HTML, CSS (wwwroot static) |
| Veri kaynağı 1 | hal.gov.tr (XLS/HTML scraping) |
| Veri kaynağı 2 | CheapShark API |
| HTML parse | HtmlAgilityPack |
| Excel parse | EPPlus 8 |
| Deploy | Docker → Render.com |

---

## Proje yapısı

```
normalmi/
├── NormalMi.API/
│   ├── Controllers/
│   │   ├── CategoriesController.cs   # GET /api/categories
│   │   ├── ProduceController.cs      # GET /api/produce/*
│   │   └── GameController.cs         # GET /api/game/*
│   ├── Models/
│   │   ├── Category.cs
│   │   ├── Produce.cs                # Ürün + fiyat karşılaştırma modeli
│   │   └── Game.cs                   # Oyun + deal + karşılaştırma modelleri
│   ├── Services/
│   │   ├── HalService.cs             # hal.gov.tr scraper (XLS + HTML fallback)
│   │   ├── HalBackgroundService.cs   # Günlük 04:00 cron görevi
│   │   ├── ProduceCacheService.cs    # Thread-safe in-memory cache
│   │   ├── GameService.cs            # CheapShark API entegrasyonu
│   │   ├── GameCacheService.cs       # 6 saatlik deal cache
│   │   └── CategoryService.cs        # Statik kategori listesi
│   ├── Program.cs
│   └── wwwroot/
│       ├── index.html                # Ana sayfa (kategori grid)
│       ├── meyve-sebze.html          # Meyve & sebze fiyat sorgulama
│       ├── oyunlar.html              # Oyun fiyat karşılaştırma
│       ├── css/style.css
│       └── js/
│           ├── api.js                # API istemcisi
│           ├── index.js              # Ana sayfa mantığı
│           ├── meyve-sebze.js        # Sebze/meyve UI mantığı
│           └── oyunlar.js            # Oyun UI mantığı
├── Dockerfile
└── render.yaml
```

---

## API

### Kategoriler

```
GET /api/categories          → Tüm kategoriler
GET /api/categories/{id}     → Tek kategori
```

### Meyve & Sebze

```
GET /api/produce/list                                       → Tüm ürünler
GET /api/produce/price?product={ad}&type={tür}              → Ürün fiyatı
GET /api/produce/compare?product={ad}&price={fiyat}         → Fiyat karşılaştır
POST /api/produce/refresh                                   → Cache'i yenile
```

**Örnek karşılaştırma yanıtı:**
```json
{
  "userPrice": 30.00,
  "marketPrice": 25.50,
  "result": "Pahalı",
  "difference": 4.50,
  "differencePercentage": 17.6,
  "produceInfo": { ... }
}
```

### Bilgisayar Oyunları

```
GET /api/game/search?query={oyun adı}       → Oyun ara
GET /api/game/{gameId}/deals                → Mağaza bazlı fiyatlar
GET /api/game/{gameId}/compare?userPrice=X  → Fiyat karşılaştır
```

---

## Yerel kurulum

**Gereksinimler:** .NET 8 SDK

```bash
git clone https://github.com/yusufreyhan/normalmi.git
cd normalmi/NormalMi.API
dotnet restore
dotnet run
```

Uygulama şu adreste çalışır:
- http://localhost:5036
- https://localhost:7160

> İlk açılışta meyve/sebze verileri arka planda yüklenmeye başlar. Birkaç saniye sonra hazır olur.

**Docker ile çalıştırma:**
```bash
docker build -t normalmi .
docker run -p 8080:8080 normalmi
```

---

## Kategoriler ve yol haritası

| Kategori | Durum | Veri Kaynağı |
|---|---|---|
| 🥦 Meyve & Sebze | ✅ Aktif | hal.gov.tr |
| 🎮 Bilgisayar Oyunları | ✅ Aktif | CheapShark API |
| ⛽ Akaryakıt | 🔜 Yakında | — |
| 🏠 Kira | 🔜 Yakında | — |
| ⚡ Elektrik | 🔜 Yakında | — |
| 📱 Telefon Tarifeleri | 🔜 Yakında | — |

---

## Fiyat değerlendirme mantığı

**Meyve & Sebze:** Kullanıcının fiyatı, hal.gov.tr piyasa ortalamasıyla karşılaştırılır.
- Fark ≤ −%15 → **Ucuz**
- −%15 ile +%15 arası → **Normal**
- Fark ≥ +%15 → **Pahalı**

**Oyunlar:** Kullanıcının fiyatı, aktif deal'ların ortalamasıyla karşılaştırılır.
- Fark ≤ −%10 → **Ucuz**
- −%10 ile +%10 arası → **Normal**
- Fark ≥ +%10 → **Pahalı**

---

## Lisans

Kişisel / eğitim amaçlıdır.
