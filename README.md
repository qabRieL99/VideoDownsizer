# 📹 iPhone Video Sıkıştırıcı

Gelişmiş, kullanıcı dostu C# tabanlı video sıkıştırma aracı. FFmpeg kullanarak yüksek kaliteli video sıkıştırma işlemleri gerçekleştirir.

## ✨ Özellikler

- **Sürükle-Bırak Desteği**: Video dosyalarını doğrudan konsola sürükleyip bırakın
- **Toplu İşlem**: Birden fazla videoyu tek seferde işleyin
- **İki İlerleme Çubuğu Stili**:
  - 🌟 Gelişmiş: Detaylı 4 satır görünüm (FPS, bitrate, ETA, frame sayısı)
  - ⚡ Minimal: Basit tek satır ilerleme çubuğu
- **Özelleştirilebilir Kalite**: CRF (18-51) ayarı ile kalite/boyut dengesi
- **Detaylı İstatistikler**: Sıkıştırma oranı, kazanılan alan, işlem hızı
- **Metadata Koruma**: Orijinal dosya tarihleri ve bilgileri korunur
- **Ses Bildirimi**: İşlem tamamlandığında sesli uyarı
- **Sistem Bilgileri**: FFmpeg durumu ve sistem özellikleri görüntüleme

## 📋 Gereksinimler

### Zorunlu
- **.NET Runtime**: .NET 6.0 veya üzeri
- **FFmpeg**: Sistem PATH'inde yüklü olmalı
- **İşletim Sistemi**: Windows, macOS veya Linux

### FFmpeg Kurulumu

#### Windows
```bash
# Chocolatey ile
choco install ffmpeg

# Scoop ile
scoop install ffmpeg

# Winget ile
winget install FFmpeg
```

#### macOS
```bash
# Homebrew ile
brew install ffmpeg
```

#### Linux
```bash
# Ubuntu/Debian
sudo apt install ffmpeg

# Fedora
sudo dnf install ffmpeg

# Arch Linux
sudo pacman -S ffmpeg
```

## 🚀 Kullanım

### 1. Program Başlatma
```bash
# Derlenmiş .exe dosyasını çalıştırın
VideoCompressor.exe

# Veya .NET CLI ile
dotnet run
```

### 2. Video İşleme Yöntemleri

#### A. Tek Dosya İşleme
1. Programı çalıştırın
2. Video dosyasını konsola sürükleyin
3. ENTER tuşuna basın

#### B. Sürükle-Bırak Başlatma
- Video dosyasını doğrudan .exe üzerine sürükleyin
- İşlem otomatik başlar

#### C. Toplu İşlem
1. Programda `b` tuşuna basın
2. Klasör yolu girin veya birden fazla dosyayı sürükleyin
3. İşlem otomatik devam eder

### 3. Komutlar

| Komut | Açıklama |
|-------|----------|
| `s` | İlerleme çubuğu stilini değiştir |
| `c` | CRF kalite ayarını değiştir (18-51) |
| `b` | Toplu işlem modunu başlat |
| `h` | Yardım menüsünü göster |
| `i` | Sistem bilgilerini göster |
| `q` | Programdan çık |

## ⚙️ Sıkıştırma Ayarları

### CRF (Constant Rate Factor) Değerleri

| CRF Aralığı | Kalite | Boyut | Kullanım |
|-------------|--------|-------|----------|
| **18-22** | 🌟 Yüksek | Büyük | Arşivleme, profesyonel |
| **23-26** | ⚖️ Dengeli | Orta | Günlük kullanım (önerilen) |
| **27-32** | 📦 Düşük | Küçük | Paylaşım, web |
| **33+** | ⚠️ Çok Düşük | Çok Küçük | Önerilmez |

### Varsayılan Ayarlar
- **Codec**: H.264 (libx264)
- **CRF**: 26
- **Preset**: veryfast
- **Metadata**: Korunur

## 📊 Desteklenen Formatlar

- MP4
- MOV
- MKV
- AVI
- WebM
- M4V
- HEVC

## 📁 Çıktı Dosyaları

Sıkıştırılmış dosyalar orijinal dosyayla aynı klasöre kaydedilir:
```
Orijinal: video.mp4
Çıktı:    video_compressed.mp4
```

## 💡 İpuçları

- **Tipik Sıkıştırma**: %50-70 boyut azaltması
- **Kalite**: CRF 23-26 arası genelde gözle fark edilmez
- **Hız**: `veryfast` preset hızlı işleme sağlar
- **Metadata**: Orijinal dosya tarihleri korunur
- **Yedekleme**: Orijinal dosyalar silinmez

## 🔧 Derleme

```bash
# Projeyi derle
dotnet build

# Release derlemesi
dotnet publish -c Release -r win-x64 --self-contained false

# Tek dosya olarak derle
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true
```

## 📈 İstatistikler

Program her işlem sonunda aşağıdaki bilgileri gösterir:

- 📦 Orijinal ve sıkıştırılmış dosya boyutu
- 💰 Kazanılan disk alanı
- 🗜️ Sıkıştırma oranı (%)
- ⏱️ İşlem süresi
- ⚡ İşleme hızı (örn: 2.5x)
- 🎬 Ortalama FPS

## 🐛 Sorun Giderme

### FFmpeg Bulunamadı
```bash
# FFmpeg'in PATH'de olduğunu kontrol edin
ffmpeg -version

# Yoksa yükleyin (yukarıdaki kurulum talimatlarına bakın)
```

### Video İşlenemiyor
- Video dosyasının bozuk olmadığından emin olun
- Dosya yolunda Türkçe karakter varsa tırnak içinde yazın
- Disk alanının yeterli olduğunu kontrol edin

### İlerleme Çubuğu Görünmüyor
- Konsol penceresini tam ekran yapın
- Minimal stil (`s` → `2`) deneyin

## 📝 Lisans

Bu proje açık kaynaklıdır ve herhangi bir amaç için kullanılabilir.

## 🤝 Katkıda Bulunma

Katkılarınızı bekliyoruz! Özellikle:
- Yeni özellikler
- Hata düzeltmeleri
- Dokümantasyon iyileştirmeleri
- Çeviri desteği

## 📧 İletişim

Sorularınız veya önerileriniz için issue açabilirsiniz.

---

**Not**: Bu araç iPhone videolarına özel optimize edilmiştir ancak tüm video formatları ile uyumludur.

Made with ❤️ using C#, FFmpeg, and Mr. Claude
