# Normal mi? - Fiyat Karşılaştırma Platformu

"Normal mi?" adlı, fiyat karşılaştırma ve fiyat algısı ölçümü yapan, modüler bir web platformu.

## Teknik Stack

- **Frontend**: HTML, CSS, Vanilla JavaScript
- **Backend**: ASP.NET Core Web API (.NET 8.0)
- **Mimari**: Katmanlı, modüler ve genişlemeye uygun

## Özellikler

### MVP (Mevcut)
- ✅ Ana sayfa - Kategori kartları
- ✅ Meyve & Sebze kategorisi - Tam çalışır durumda
  - HAL.gov.tr'den gerçek zamanlı fiyat verisi
  - Ürün seçimi ve tür seçimi
  - Fiyat karşılaştırma (Ucuz/Normal/Pahalı)

### Yakında
- Akaryakıt kategorisi
- Kira kategorisi
- Elektrik kategorisi

## Kurulum

### Backend

```bash
cd NormalMi.API
dotnet restore
dotnet run
```

Backend şu adreslerde çalışacak:
- HTTPS: `https://localhost:7160`
- HTTP: `http://localhost:5036`

### Frontend

Frontend dosyaları `wwwroot` klasöründe bulunmaktadır. Backend çalıştığında static dosyalar otomatik olarak servis edilir.

Alternatif olarak, Live Server veya benzeri bir araçla `wwwroot` klasörünü açabilirsiniz.

## API Endpoints

### Kategoriler
- `GET /api/categories` - Tüm kategorileri listele
- `GET /api/categories/{id}` - Belirli bir kategoriyi getir

### Meyve & Sebze
- `GET /api/produce/list` - Tüm ürünleri listele
- `GET /api/produce/price?product={name}&type={type}` - Ürün fiyatını getir
- `GET /api/produce/compare?product={name}&price={price}&type={type}` - Fiyat karşılaştır

## Proje Yapısı

```
NormalMi.API/
├── Controllers/        # API Controller'ları
├── Models/            # Veri modelleri
├── Services/          # İş mantığı servisleri
└── wwwroot/           # Frontend dosyaları
    ├── css/
    ├── js/
    └── *.html
```

## Geliştirme Notları

- Kategori sistemi modüler olarak tasarlanmıştır
- Yeni kategoriler kolayca eklenebilir
- HAL.gov.tr'den veri çekme servisi mevcuttur
- Fiyat karşılaştırma mantığı %10 tolerans ile çalışır

## Lisans

Bu proje eğitim/kişisel kullanım amaçlıdır.

