# 🧾 Pazar Yeri Fatura Otomasyonu

Türkiye'deki pazar yerleri (Trendyol, Hepsiburada) üzerinden gelen siparişleri otomatik olarak çeken, GİB e-Arşiv portalı üzerinden fatura kesen ve tüm süreci tek bir panelden yöneten **full-stack otomasyon sistemi**.

---

## 📋 İçindekiler

- [Özellikler](#-özellikler)
- [Mimari](#-mimari)
- [Teknoloji Stack](#-teknoloji-stack)
- [Kurulum](#-kurulum)
- [Ortam Değişkenleri](#-ortam-değişkenleri)
- [Kullanım](#-kullanım)
- [API Endpoints](#-api-endpoints)
- [Proje Yapısı](#-proje-yapısı)
- [Ekran Görüntüleri](#-ekran-görüntüleri)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)

---

## ✨ Özellikler

| Özellik | Açıklama |
|---------|----------|
| 🔄 **Otomatik Sipariş Senkronizasyonu** | Trendyol ve Hepsiburada siparişlerini arka planda periyodik olarak çeker |
| 🧾 **GİB e-Arşiv Fatura Kesimi** | Siparişler için tek tıkla veya toplu olarak e-Arşiv faturası oluşturur |
| 📊 **Dashboard** | Toplam sipariş, kesilen fatura, bekleyen fatura ve hatalı işlem istatistikleri |
| 🏷️ **Platform Bazlı Filtreleme** | Siparişleri Trendyol veya Hepsiburada bazında ayrı ayrı görüntüleme |
| 📦 **Sipariş Yönetimi** | Tüm siparişleri durum, fatura ve platform bilgileriyle listeleme |
| 🐳 **Docker ile Kolay Kurulum** | Tek komutla tüm sistemi ayağa kaldırma |

---

## 🏗️ Mimari

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Frontend   │────▶│   Backend API    │────▶│   PostgreSQL    │
│  React/Vite  │     │   .NET 8 Web API │     │    Database     │
│  Port: 3000  │     │   Port: 5000     │     │   Port: 5433    │
└─────────────┘     └──────┬───────────┘     └─────────────────┘
                           │
                    ┌──────┴───────┐
                    │              │
              ┌─────▼─────┐ ┌─────▼──────┐
              │ Trendyol   │ │Hepsiburada │
              │   API      │ │   API      │
              └─────┬──────┘ └─────┬──────┘
                    │              │
                    └──────┬───────┘
                           │
                    ┌──────▼───────┐
                    │  GİB e-Arşiv │
                    │   Portalı    │
                    └──────────────┘
```

---

## 🛠️ Teknoloji Stack

### Backend
- **.NET 8** — Web API
- **Entity Framework Core 8** — ORM & Migrations
- **PostgreSQL 15** — Veritabanı
- **Swagger/OpenAPI** — API Dokümantasyonu
- **Background Worker** — Periyodik sipariş senkronizasyonu

### Frontend
- **React 18** — UI Framework
- **Vite 5** — Build Tool
- **TailwindCSS 3** — Utility-first CSS
- **React Router 6** — Client-side Routing
- **Axios** — HTTP Client

### DevOps
- **Docker & Docker Compose** — Konteynerizasyon
- **Multi-stage Dockerfile** — Optimize edilmiş imaj boyutları

---

## 🚀 Kurulum

### Ön Koşullar

- [Docker](https://docs.docker.com/get-docker/) ve [Docker Compose](https://docs.docker.com/compose/install/) yüklü olmalıdır.

### 1. Depoyu Klonlayın

```bash
git clone https://github.com/<kullanici-adi>/PazarYeriFaturaOtomasyonu.git
cd PazarYeriFaturaOtomasyonu
```

### 2. Ortam Değişkenlerini Ayarlayın

```bash
cp .env.example .env
```

`.env` dosyasını kendi API bilgilerinizle doldurun:

```env
Trendyol__SupplierId=<TRENDYOL_SATICI_ID>
Trendyol__ApiKey=<TRENDYOL_API_KEY>
Trendyol__ApiSecret=<TRENDYOL_API_SECRET>
Hepsiburada__MerchantId=<HEPSIBURADA_MERCHANT_ID>
Hepsiburada__ApiKey=<HEPSIBURADA_SERVIS_ANAHTARI>
GibPortal__UserCode=<GIB_KULLANICI_KODU>
GibPortal__Password=<GIB_SIFRE>
```

### 3. Docker ile Çalıştırın

```bash
docker compose up -d --build
```

Bu komut aşağıdaki servisleri başlatır:

| Servis | URL |
|--------|-----|
| 🌐 Frontend | [http://localhost:3000](http://localhost:3000) |
| ⚙️ Backend API | [http://localhost:5000](http://localhost:5000) |
| 📖 Swagger UI | [http://localhost:5000/swagger](http://localhost:5000/swagger) |
| 🗄️ PostgreSQL | `localhost:5433` |

### 4. Durdurmak İçin

```bash
docker compose down
```

Veritabanı verilerini de silmek isterseniz:

```bash
docker compose down -v
```

---

## 🔐 Ortam Değişkenleri

| Değişken | Açıklama | Zorunlu |
|----------|----------|---------|
| `Trendyol__SupplierId` | Trendyol satıcı ID'niz | ✅ |
| `Trendyol__ApiKey` | Trendyol API anahtarınız | ✅ |
| `Trendyol__ApiSecret` | Trendyol API gizli anahtarınız | ✅ |
| `Hepsiburada__MerchantId` | Hepsiburada merchant ID'niz | ✅ |
| `Hepsiburada__ApiKey` | Hepsiburada servis anahtarınız | ✅ |
| `GibPortal__UserCode` | GİB e-Arşiv portal kullanıcı kodunuz | ✅ |
| `GibPortal__Password` | GİB e-Arşiv portal şifreniz | ✅ |

> ⚠️ **Uyarı:** `.env` dosyanızı asla versiyon kontrolüne eklemeyin. `.gitignore` bunu otomatik olarak engelleyecektir.

---

## 📖 Kullanım

### Sipariş Senkronizasyonu
Sistem arka planda bir **Background Worker** çalıştırır. Bu worker belirli aralıklarla Trendyol ve Hepsiburada API'lerine bağlanarak "Teslim Edilmiş" (Delivered) statüsündeki siparişleri otomatik olarak veritabanına kaydeder.

### Fatura Kesimi
- **Tekli Fatura:** Siparişler sayfasında ilgili siparişin yanındaki "Fatura Kes" butonuna tıklayın.
- **Toplu Fatura:** Birden fazla sipariş seçerek "Seçilenlere Fatura Kes" butonuyla toplu fatura oluşturun.

Faturalar GİB e-Arşiv portalı üzerinden kesilir ve ETTN numarası ile birlikte sisteme kaydedilir.

---

## 📡 API Endpoints

### Dashboard
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/api/dashboard/stats` | Genel istatistikleri döndürür |

### Siparişler
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/api/orders` | Tüm siparişleri listeler |
| `GET` | `/api/orders?platform=trendyol` | Platform bazlı filtreleme |
| `POST` | `/api/orders/sync` | Manuel sipariş senkronizasyonu başlatır |
| `POST` | `/api/orders/{id}/invoice` | Tekli fatura kesimi |
| `POST` | `/api/orders/invoice/bulk` | Toplu fatura kesimi |

> 📖 Detaylı API dokümantasyonu için Swagger UI: [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

## 📁 Proje Yapısı

```
PazarYeriFaturaOtomasyonu/
├── Backend/
│   └── PazarYeriOtomasyon.API/
│       ├── Controllers/          # API Controller'ları
│       │   ├── DashboardController.cs
│       │   └── OrdersController.cs
│       ├── Data/                 # EF Core DbContext
│       │   └── AppDbContext.cs
│       ├── Migrations/           # Veritabanı migration'ları
│       ├── Models/               # Veri modelleri
│       │   ├── Order.cs
│       │   └── Invoice.cs
│       ├── Services/             # İş mantığı servisleri
│       │   ├── IMarketplaceService.cs
│       │   ├── TrendyolService.cs
│       │   ├── HepsiburadaService.cs
│       │   └── InvoiceService.cs
│       ├── Workers/              # Background servisler
│       │   └── OrderSyncWorker.cs
│       ├── Program.cs            # Uygulama giriş noktası
│       ├── Dockerfile
│       └── appsettings.json
├── Frontend/
│   ├── src/
│   │   ├── pages/
│   │   │   ├── Dashboard.jsx     # Ana panel sayfası
│   │   │   └── Orders.jsx        # Sipariş listesi sayfası
│   │   ├── App.jsx               # Ana uygulama bileşeni
│   │   ├── main.jsx              # React giriş noktası
│   │   └── index.css
│   ├── Dockerfile
│   ├── package.json
│   ├── vite.config.js
│   └── tailwind.config.js
├── docker-compose.yml            # Tüm servislerin orkestrasyon dosyası
├── .env.example                  # Ortam değişkenleri şablonu
├── .gitignore
├── LICENSE
└── README.md
```

---

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.

---

<p align="center">
  <sub>⭐ Projeyi beğendiyseniz yıldız vermeyi unutmayın!</sub>
</p>
