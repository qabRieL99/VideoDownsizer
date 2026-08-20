# 📹 iPhone Video Sıkıştırıcı

Gelişmiş, kullanıcı dostu C# tabanlı video sıkıştırma aracı. FFmpeg kullanarak yüksek kaliteli video sıkıştırma işlemleri gerçekleştirir.

## ✨ Özellikler

- **Sürükle-Bırak Desteği**: Video dosyalarını doğrudan konsola sürükleyip bırakın
- **Toplu İşlem**: Birden fazla videoyu tek seferde işleyin
- **Batch İlerleme Takibi**: Kesintiye uğrayan işlemleri kaldığınız yerden devam ettirin
- **İki İlerleme Çubuğu Stili**:
  - 🌟 Gelişmiş: Detaylı 4 satır görünüm (FPS, bitrate, ETA, frame sayısı)
  - ⚡ Minimal: Basit tek satır ilerleme çubuğu
- **Özelleştirilebilir Kalite**: CRF (18-51) ayarı ile kalite/boyut dengesi
- **Detaylı İstatistikler**: Sıkıştırma oranı, kazanılan alan, işlem hızı, ortalama FPS
- **Metadata Koruma**: Orijinal dosya tarihleri ve bilgileri korunur
- **Ses Bildirimi**: İşlem tamamlandığında sesli uyarı
- **Sistem Bilgileri**: FFmpeg durumu ve sistem özellikleri görüntüleme
- **Gelişmiş Video Bilgisi**: Çözünürlük, kodek ve FPS bilgilerini görüntüleme
- **Renkli UI**: Çeşitli durum ve hatalar için renk kodlu çıktı

## 📋 Gereksinimler

### Zorunlu
- **.NET Runtime**: .NET 6.0 veya üzeri
- **FFmpeg**: Sistem PATH'inde yüklü olmalı
- **FFprobe**: FFmpeg ile birlikte gelir (video bilgisi almak için)
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
VideoDownsizer.exe

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
4. **P tuşuna basarak** mevcut dosya bittikten sonra duraklatabilirsiniz
5. Programı yeniden başlattığınızda kaldığınız yerden devam edebilirsiniz

### 3. Komutlar

| Komut | Açıklama |
|-------|----------|
| `s` | İlerleme çubuğu stilini değiştir |
| `c` | CRF kalite ayarını değiştir (18-51) |
| `b` | Toplu işlem modunu başlat |
| `h` | Yardım menüsünü göster |
| `i` | Sistem bilgilerini göster |
| `q` | Programdan çık |
| `p` | Toplu işlemde mevcut dosya sonrası duraklat (İŞLEM SÖNÜ) |

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

Sıkıştırılmış dosyalar otomatik olarak oluşturulan `compressed` klasöründe kaydedilir:
```
Orijinal: video.mp4
Çıktı:    compressed/video_compressed.mp4
```

## 💡 İpuçları

- **Tipik Sıkıştırma**: %50-70 boyut azaltması
- **Kalite**: CRF 23-26 arası genelde gözle fark edilmez
- **Hız**: `veryfast` preset hızlı işleme sağlar
- **Metadata**: Orijinal dosya tarihleri korunur
- **Yedekleme**: Orijinal dosyalar silinmez
- **Toplu İşlem Devamı**: Kesintiye uğrayan batch işlemleri güvenli bir şekilde devam ettirin

## 🔧 Derleme

```bash
# Projeyi derle
dotnet build

# Release derlemesi
dotnet publish -c Release -r win-x64 --self-contained false

# Tek dosya olarak derle
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true

# macOS ve Linux için derleme
dotnet publish -c Release -r osx-x64 -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true
```

## 📈 İstatistikler ve İzleme

### Tek Dosya İşleme
Program her işlem sonunda aşağıdaki bilgileri gösterir:

- 📦 Orijinal ve sıkıştırılmış dosya boyutu
- 💰 Kazanılan disk alanı
- 🗜️ Sıkıştırma oranı (%)
- ⏱️ İşlem süresi
- ⚡ İşleme hızı (örn: 2.5x)
- 🎬 Ortalama FPS

### Toplu İşlem Takibi
Toplu işlem sırasında ilerleme durumu kaydedilir:

- ✅ Tamamlanan dosyalar
- ⏳ Bekleyen dosyalar
- ❌ Başarısız işlemler
- Toplam boyut tasarrufu ve genel sıkıştırma oranı

## 🎨 İlerleme Çubuğu Türleri

### Gelişmiş Stil (Varsayılan)
4 satırlı detaylı görünüm:
- Renkli ilerlemeler çubuğu (%)
- Geçen süre / Toplam süre ve tahmini kalan süre
- Frame sayısı, Bitrate ve işlenen veri miktarı
- Tahmini sıkıştırma oranı ve geçen süre

### Minimal Stil
Tek satır, basit görünüm:
- İlerleme çubuğu (%)
- Anlık FPS ve Bitrate

## 🐛 Sorun Giderme

### FFmpeg Bulunamadı
```bash
# FFmpeg'in PATH'de olduğunu kontrol edin
ffmpeg -version

# FFprobe'in de yüklü olup olmadığını kontrol edin
ffprobe -version

# Yoksa yükleyin (yukarıdaki kurulum talimatlarına bakın)
```

### Video İşlenemiyor
- Video dosyasının bozuk olmadığından emin olun
- Dosya yolunda Türkçe karakter varsa tırnak içinde yazın
- Disk alanının yeterli olduğunu kontrol edin
- Desteklenen formatlardan biri olduğunu doğrulayın

### İlerleme Çubuğu Görünmüyor
- Konsol penceresini tam ekran yapın
- Minimal stil (`s` → `2`) deneyin
- Konsol buffer boyutunu artırmayı deneyin

### Batch İşlem Kaldığı Yerden Devam Etmiyor
- `video_downsizer_batch_progress.json` dosyasının var olup olmadığını kontrol edin
- Bu dosya batch işlemin durum bilgisini içerir
- Dosya silinirse, toplu işlem sıfırdan başlanır

## 📝 Lisans

Bu proje açık kaynaklıdır ve herhangi bir amaç için kullanılabilir.

## 🤝 Katkıda Bulunma

Katkılarınızı bekliyoruz! Özellikle:
- Yeni özellikler
- Hata düzeltmeleri
- Dokümantasyon iyileştirmeleri
- Çeviri desteği
- Platform uyumluluğu iyileştirmeleri

## 📧 İletişim

Sorularınız veya önerileriniz için issue açabilirsiniz.

---

**Not**: Bu araç iPhone videolarına özel optimize edilmiştir ancak tüm video formatları ile uyumludur.

Made with ❤️ using C#, FFmpeg, and Mr. Claude
