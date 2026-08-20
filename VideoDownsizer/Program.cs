using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

class BatchProgressState
{
    public List<string> AllFiles { get; set; } = new();
    public List<string> CompletedFiles { get; set; } = new();
    public List<string> PendingFiles { get; set; } = new();
    public List<string> FailedFiles { get; set; } = new();
    public bool IsComplete { get; set; }
}

class Program
{
    // Choose your progress bar style here: 1 = Enhanced, 2 = Minimal
    private static int PROGRESS_BAR_STYLE = 1;
    private static int CRF_QUALITY = 26; // Default CRF value (18-28 recommended)
    private static readonly object historyLock = new object(); // Thread safety
    private static List<double> fpsHistory = new List<double>();
    private static List<double> bitrateHistory = new List<double>();
    private static DateTime lastProgressUpdate = DateTime.MinValue;
    private const int PROGRESS_UPDATE_THROTTLE_MS = 100; // Progress bar güncelleme throttle

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        DisplayBanner();

        // Eğer dosya sürüklenmiş ve bırakılmışsa, işle ve çık
        if (args.Length > 0)
        {
            List<string> inputFiles = new List<string>();
            foreach (var arg in args)
            {
                string file = arg.Trim('"', '\'');
                if (File.Exists(file))
                {
                    inputFiles.Add(file);
                }
            }

            if (inputFiles.Count > 0)
            {
                ProcessMultipleVideos(inputFiles);
                Console.WriteLine("\nÇıkmak için bir tuşa basın...");
                Console.ReadKey();
                return;
            }
        }

        // Sürekli döngü - konsola sürükle bırak modu
        while (true)
        {
            DisplayMenu();
            string input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            if (input.ToLower() == "q")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nProgram sonlandırılıyor...");
                Console.ResetColor();
                System.Threading.Thread.Sleep(1000);
                return;
            }

            if (input.ToLower() == "s")
            {
                ChangeProgressBarStyle();
                continue;
            }

            if (input.ToLower() == "c")
            {
                ChangeCRFQuality();
                continue;
            }

            if (input.ToLower() == "b")
            {
                BatchProcessVideos();
                continue;
            }

            if (input.ToLower() == "h")
            {
                ShowHelp();
                continue;
            }

            if (input.ToLower() == "i")
            {
                ShowSystemInfo();
                continue;
            }

            string inputFile = input.Trim('"', '\'');
            ProcessVideo(inputFile);
        }
    }

    static void DisplayBanner()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                ║");
        Console.WriteLine("║        📹 iPhone Video Sıkıştırıcı             ║");
        Console.WriteLine("║                                                ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine("\n✨ Gelişmiş video sıkıştırma aracı\n");
    }

    static void DisplayMenu()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n" + new string('─', 70));
        Console.WriteLine("📂 Video dosyasını buraya sürükleyip ENTER'a basın");
        Console.WriteLine(new string('─', 70));
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\nKomutlar:");
        Console.WriteLine("  [s] Stil değiştir\t[c] Kalite ayarı (CRF: {0})\t[b] Toplu işlem", CRF_QUALITY);
        Console.WriteLine("  [h] Yardım\t\t[i] Sistem bilgisi\t\t[q] Çıkış");
        Console.ResetColor();
        Console.Write("\n▶ Dosya yolu: ");
    }

    static void ShowHelp()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         📖 YARDIM MENÜSÜ                       ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🎯 KULLANIM:");
        Console.ResetColor();
        Console.WriteLine("  1. Video dosyasını konsola sürükleyip bırakın");
        Console.WriteLine("  2. ENTER tuşuna basın");
        Console.WriteLine("  3. İşlem tamamlanana kadar bekleyin\n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("⚙️ SIKIŞTIRMA AYARLARI:");
        Console.ResetColor();
        Console.WriteLine("  • Codec: H.264 (libx264)");
        Console.WriteLine("  • CRF: {0} (Kalite/Boyut dengesi)", CRF_QUALITY);
        Console.WriteLine("  • Preset: veryfast (Hızlı işleme)");
        Console.WriteLine("  • Metadata: Korunur\n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📊 CRF KALİTE REHBERİ:");
        Console.ResetColor();
        Console.WriteLine("  18-22: Yüksek kalite (büyük dosya)");
        Console.WriteLine("  23-26: Dengeli (önerilen)");
        Console.WriteLine("  27-32: Düşük boyut (kalite kaybı)");
        Console.WriteLine("  33+  : Çok düşük boyut (belirgin kayıp)\n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📊 İLERLEME ÇUBUĞU STİLLERİ:");
        Console.ResetColor();
        Console.WriteLine("  [1] Gelişmiş - Detaylı, renkli 4 satır görünüm");
        Console.WriteLine("  [2] Minimal - Basit tek satır çubuk\n");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🎨 DESTEKLENEN FORMATLAR:");
        Console.ResetColor();
        Console.WriteLine("  MP4, MOV, MKV, AVI, WebM, M4V, HEVC\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("💡 İPUÇLARI:");
        Console.ResetColor();
        Console.WriteLine("  • Tipik olarak %50-70 boyut azaltması sağlanır");
        Console.WriteLine("  • Orijinal dosya tarihleri korunur");
        Console.WriteLine("  • Sıkıştırılmış dosya aynı klasöre kaydedilir");
        Console.WriteLine("  • Toplu işlem için birden fazla dosya sürükleyin");
        Console.WriteLine("  • İşlem bittikten sonra ses bildirimi gelir\n");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Devam etmek için bir tuşa basın...");
        Console.ResetColor();
        Console.ReadKey();
        Console.Clear();
        DisplayBanner();
    }

    static void ShowSystemInfo()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      💻 SİSTEM BİLGİLERİ                       ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        // FFmpeg version check
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("🔧 FFmpeg Durumu: ");
        Console.ResetColor();
        string ffmpegVersion = GetFFmpegVersion();
        if (!string.IsNullOrEmpty(ffmpegVersion))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Yüklü (v{ffmpegVersion})");
            Console.ResetColor();

            // FFmpeg location
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   Konum: {GetFFmpegPath()}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Bulunamadı!");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠️  FFmpeg Kurulum Talimatları:");
            Console.WriteLine("   1. https://ffmpeg.org/download.html adresine gidin");
            Console.WriteLine("   2. İşletim sisteminize uygun sürümü indirin");
            Console.WriteLine("   3. FFmpeg'i sistem PATH'ine ekleyin");
            Console.WriteLine("\n   Windows için hızlı kurulum:");
            Console.WriteLine("   • Chocolatey: choco install ffmpeg");
            Console.WriteLine("   • Scoop: scoop install ffmpeg");
            Console.WriteLine("   • Winget: winget install ffmpeg");
            Console.ResetColor();
        }

        Console.WriteLine();

        // System info
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("🖥️  İşletim Sistemi: ");
        Console.ResetColor();
        Console.WriteLine($"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("⚡ İşlemci Çekirdek: ");
        Console.ResetColor();
        Console.WriteLine($"{Environment.ProcessorCount} çekirdek");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("🎯 İşlemci Mimarisi: ");
        Console.ResetColor();
        Console.WriteLine(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("📦 .NET Sürümü: ");
        Console.ResetColor();
        Console.WriteLine(Environment.Version);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("🏠 Kullanıcı Dizini: ");
        Console.ResetColor();
        Console.WriteLine(Environment.UserName);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("📁 Çalışma Dizini: ");
        Console.ResetColor();
        Console.WriteLine(Directory.GetCurrentDirectory());

        Console.WriteLine();

        // Current settings
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚙️  MEVCUT AYARLAR:");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("   🎨 İlerleme Stili: ");
        Console.ResetColor();
        string[] styleNames = { "", "Gelişmiş", "Minimal" };
        Console.WriteLine(styleNames[PROGRESS_BAR_STYLE]);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("   🎬 CRF Kalitesi: ");
        Console.ResetColor();
        Console.Write($"{CRF_QUALITY} ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("(");
        if (CRF_QUALITY <= 22)
            Console.Write("Yüksek Kalite");
        else if (CRF_QUALITY <= 26)
            Console.Write("Dengeli");
        else if (CRF_QUALITY <= 32)
            Console.Write("Düşük Boyut");
        else
            Console.Write("Çok Düşük Boyut");
        Console.WriteLine(")");
        Console.ResetColor();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Devam etmek için bir tuşa basın...");
        Console.ResetColor();
        Console.ReadKey();
        Console.Clear();
        DisplayBanner();
    }

    static string GetFFmpegPath()
    {
        try
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";
                proc.StartInfo.Arguments = "ffmpeg";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();

                string output = proc.StandardOutput.ReadLine();
                proc.WaitForExit();

                return output ?? "Bilinmiyor";
            }
        }
        catch
        {
            return "Bilinmiyor";
        }
    }

    static string GetFFmpegVersion()
    {
        try
        {
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = "ffmpeg";
                ffmpeg.StartInfo.Arguments = "-version";
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.Start();

                string output = ffmpeg.StandardOutput.ReadLine();
                ffmpeg.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    var match = Regex.Match(output, @"version ([\d.]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    static void ChangeProgressBarStyle()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              🎨 İLERLEME ÇUBUĞU STİLİ SEÇİN                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ [1] 🌟 Gelişmiş Stil                                           │");
        Console.WriteLine("│     └─ Renkli, 4 satır detaylı görünüm                         │");
        Console.WriteLine("│                                                                │");
        Console.WriteLine("│ [2] ⚡ Minimal Stil                                            │");
        Console.WriteLine("│     └─ Basit, tek satır ilerleme çubuğu                        │");
        Console.WriteLine("└────────────────────────────────────────────────────────────────┘");

        Console.Write("\n▶ Seçiminiz (1-2): ");

        string choice = Console.ReadLine();
        if (int.TryParse(choice, out int style) && style >= 1 && style <= 2)
        {
            PROGRESS_BAR_STYLE = style;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Stil {style} seçildi!");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1500);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n✗ Geçersiz seçim!");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1500);
        }
        Console.Clear();
        DisplayBanner();
    }

    static void ChangeCRFQuality()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   🎬 CRF KALİTE AYARI                          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("CRF (Constant Rate Factor) Rehberi:");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  18-22: 🌟 Yüksek Kalite");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("         • Neredeyse kayıpsız görüntü");
        Console.WriteLine("         • Büyük dosya boyutu");
        Console.WriteLine("         • Arşivleme için ideal\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  23-26: ⚖️  Dengeli (Önerilen)");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("         • İyi kalite/boyut dengesi");
        Console.WriteLine("         • Günlük kullanım için ideal");
        Console.WriteLine("         • Varsayılan: 26\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("  27-32: 📦 Düşük Boyut");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("         • Küçük dosya boyutu");
        Console.WriteLine("         • Hafif kalite kaybı");
        Console.WriteLine("         • Paylaşım için uygun\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  33+  : ⚠️  Çok Düşük Boyut");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("         • Çok küçük dosya");
        Console.WriteLine("         • Belirgin kalite kaybı");
        Console.WriteLine("         • Önerilmez\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Mevcut CRF: {CRF_QUALITY}");
        Console.ResetColor();
        Console.Write("\n▶ Yeni CRF değeri (18-51, Enter=iptal): ");

        string input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nİşlem iptal edildi.");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1000);
        }
        else if (int.TryParse(input, out int crf) && crf >= 18 && crf <= 51)
        {
            CRF_QUALITY = crf;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ CRF {crf} olarak ayarlandı!");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1500);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n✗ Geçersiz değer! (18-51 arası olmalı)");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1500);
        }
        Console.Clear();
        DisplayBanner();
    }

    static void BatchProcessVideos()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    📦 TOPLU İŞLEM MODU                         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.WriteLine("Klasör yolu girin veya birden fazla dosyayı sürükleyip bırakın:");
        Console.Write("\n▶ Yol: ");
        string input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nİşlem iptal edildi.");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1000);
            Console.Clear();
            DisplayBanner();
            return;
        }

        List<string> files = new List<string>();

        // Check if it's a directory
        if (Directory.Exists(input.Trim('"', '\'')))
        {
            string directory = input.Trim('"', '\'');
            string[] videoExtensions = { ".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v", ".hevc" };

            foreach (string ext in videoExtensions)
            {
                files.AddRange(Directory.GetFiles(directory, "*" + ext, SearchOption.TopDirectoryOnly));
            }
        }
        else
        {
            // Parse multiple files
            var parts = input.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string file = part.Trim();
                if (File.Exists(file))
                {
                    files.Add(file);
                }
            }
        }

        if (files.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n✗ Hiç video dosyası bulunamadı!");
            Console.ResetColor();
            System.Threading.Thread.Sleep(2000);
            Console.Clear();
            DisplayBanner();
            return;
        }

        ProcessMultipleVideos(files);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\nDevam etmek için bir tuşa basın...");
        Console.ResetColor();
        Console.ReadKey();
        Console.Clear();
        DisplayBanner();
    }

    static string GetBatchProgressFilePath(List<string> files)
    {
        string directory = Path.GetDirectoryName(files.FirstOrDefault() ?? string.Empty) ?? Directory.GetCurrentDirectory();
        return Path.Combine(directory, "video_downsizer_batch_progress.json");
    }

    static BatchProgressState? LoadBatchProgressState(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BatchProgressState>(json);
        }
        catch
        {
            return null;
        }
    }

    static void SaveBatchProgressState(string path, BatchProgressState state)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    static void DeleteBatchProgressState(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    static void DisplayBatchStatus(BatchProgressState state)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("\n📋 Batch durumu:");
        Console.WriteLine($"   ✅ Tamamlandı: {state.CompletedFiles.Count}");
        Console.WriteLine($"   ⏳ Bekliyor: {state.PendingFiles.Count}");
        Console.WriteLine($"   ❌ Başarısız: {state.FailedFiles.Count}");

        Console.WriteLine("   Tamamlanan dosyalar:");
        if (state.CompletedFiles.Count == 0)
        {
            Console.WriteLine("     - yok");
        }
        else
        {
            foreach (string completedFile in state.CompletedFiles)
            {
                Console.WriteLine($"     - {Path.GetFileName(completedFile)}");
            }
        }

        Console.WriteLine("   Bekleyen dosyalar:");
        if (state.PendingFiles.Count == 0)
        {
            Console.WriteLine("     - yok");
        }
        else
        {
            foreach (string pendingFile in state.PendingFiles)
            {
                Console.WriteLine($"     - {Path.GetFileName(pendingFile)}");
            }
        }

        Console.ResetColor();
    }

    static bool ShouldPauseBatchAfterCurrentFile()
    {
        if (!Console.KeyAvailable)
        {
            return false;
        }

        ConsoleKeyInfo key = Console.ReadKey(true);
        return key.KeyChar == 'p' || key.KeyChar == 'P';
    }

    static void ProcessMultipleVideos(List<string> files)
    {
        List<string> inputFiles = files.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        string progressFile = GetBatchProgressFilePath(inputFiles);
        BatchProgressState state = LoadBatchProgressState(progressFile);

        if (state != null && state.PendingFiles.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠ Önceki batch işleme durduruldu. {state.CompletedFiles.Count} dosya tamamlandı, {state.PendingFiles.Count} dosya bekliyor.");
            Console.WriteLine("   Kaldığınız yerden devam etmek ister misiniz? (E/H)");
            Console.ResetColor();
            string response = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (response == "E" || response == "Y")
            {
                state.PendingFiles = state.PendingFiles.ToList();
            }
            else
            {
                state = new BatchProgressState
                {
                    AllFiles = inputFiles,
                    CompletedFiles = new List<string>(),
                    PendingFiles = inputFiles.ToList(),
                    FailedFiles = new List<string>(),
                    IsComplete = false
                };
            }
        }
        else
        {
            state = new BatchProgressState
            {
                AllFiles = inputFiles,
                CompletedFiles = new List<string>(),
                PendingFiles = inputFiles.ToList(),
                FailedFiles = new List<string>(),
                IsComplete = false
            };
        }

        SaveBatchProgressState(progressFile, state);

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  📦 TOPLU İŞLEM: {state.AllFiles.Count} dosya bulundu                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.WriteLine("💡 'P' tuşuna basarsanız, mevcut dosya bittikten sonra durur ve kaldığınız yerden devam edebilirsiniz.");
        Console.ResetColor();

        long totalOriginalSize = 0;
        long totalCompressedSize = 0;
        int successCount = 0;
        int failCount = 0;

        while (state.PendingFiles.Count > 0)
        {
            string currentFile = state.PendingFiles[0];
            state.PendingFiles.RemoveAt(0);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[{state.CompletedFiles.Count + 1}/{state.AllFiles.Count}] İşleniyor: {Path.GetFileName(currentFile)}");
            Console.ResetColor();

            FileInfo originalFile = new FileInfo(currentFile);
            totalOriginalSize += originalFile.Length;

            bool success = ProcessVideo(currentFile);

            if (success)
            {
                string outputFile = GetCompressedOutputPath(currentFile);

                if (File.Exists(outputFile))
                {
                    FileInfo compressedFile = new FileInfo(outputFile);
                    totalCompressedSize += compressedFile.Length;
                    state.CompletedFiles.Add(currentFile);
                    successCount++;
                }
                else
                {
                    state.FailedFiles.Add(currentFile);
                    failCount++;
                }
            }
            else
            {
                state.FailedFiles.Add(currentFile);
                failCount++;
            }

            SaveBatchProgressState(progressFile, state);
            DisplayBatchStatus(state);

            if (ShouldPauseBatchAfterCurrentFile())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⏸️ Tamam, bu dosyadan sonra sıkıştırmayı durduracağım.");
                Console.WriteLine("   Programı yeniden başlattığında kaldığınız yerden devam edebilirsiniz.");
                Console.ResetColor();
                state.IsComplete = false;
                SaveBatchProgressState(progressFile, state);
                return;
            }
        }

        state.IsComplete = true;
        SaveBatchProgressState(progressFile, state);
        DeleteBatchProgressState(progressFile);

        // Final summary
        Console.WriteLine("\n");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  ✅ TOPLU İŞLEM TAMAMLANDI                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"📊 İşlem Özeti:");
        Console.WriteLine($"   ✓ Başarılı: {successCount}");
        if (failCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   ✗ Başarısız: {failCount}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"   📦 Toplam Orijinal: {FormatFileSize(totalOriginalSize)}");
        Console.WriteLine($"   📦 Toplam Sıkıştırılmış: {FormatFileSize(totalCompressedSize)}");

        if (totalOriginalSize > 0)
        {
            double totalRatio = (1 - ((double)totalCompressedSize / totalOriginalSize)) * 100;
            long totalSaved = totalOriginalSize - totalCompressedSize;
            Console.WriteLine($"   💰 Toplam Kazanç: {FormatFileSize(totalSaved)} ({totalRatio:0.0}%)");
        }
        Console.ResetColor();
    }

    static string GetCompressedOutputPath(string inputFile)
    {
        string directory = Path.GetDirectoryName(inputFile) ?? ".";
        string compressedDirectory = Path.Combine(directory, "compressed");
        Directory.CreateDirectory(compressedDirectory);

        string fileName = Path.GetFileNameWithoutExtension(inputFile);
        string extension = Path.GetExtension(inputFile);
        return Path.Combine(compressedDirectory, fileName + "_compressed" + extension);
    }

    static bool ProcessVideo(string inputFile)
    {
        // Reset history - Thread safe
        lock (historyLock)
        {
            fpsHistory.Clear();
            bitrateHistory.Clear();
        }

        if (!File.Exists(inputFile))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ Dosya bulunamadı!");
            Console.ResetColor();
            return false;
        }

        string[] videoExtensions = { ".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v", ".hevc" };
        string extension = Path.GetExtension(inputFile).ToLower();

        if (!Array.Exists(videoExtensions, ext => ext == extension))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n❌ Bu dosya bir video dosyası değil!");
            Console.WriteLine($"   Desteklenen formatlar: {string.Join(", ", videoExtensions)}");
            Console.ResetColor();
            return false;
        }

        string outputFile = GetCompressedOutputPath(inputFile);

        // Check if output file already exists
        if (File.Exists(outputFile))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠ Uyarı: '{Path.GetFileName(outputFile)}' zaten mevcut!");
            Console.Write("   Üzerine yazmak istiyor musunuz? (E/H): ");
            Console.ResetColor();
            string response = Console.ReadLine()?.ToUpper();
            if (response != "E" && response != "Y")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("   İşlem iptal edildi.");
                Console.ResetColor();
                return false;
            }
        }

        FileInfo fileInfo = new FileInfo(inputFile);
        string fileSize = FormatFileSize(fileInfo.Length);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");

        string videoName = Path.GetFileName(inputFile);
        if (videoName.Length > 52)
            videoName = videoName.Substring(0, 49) + "...";

        Console.Write("║ 📹 Video: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(videoName.PadRight(52));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║");
        Console.Write("║ 💾 Boyut: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(fileSize.PadRight(52));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        double duration = GetVideoDuration(inputFile);
        VideoInfo videoInfo = GetVideoInfo(inputFile);
        DateTime start = DateTime.Now;

        if (duration <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Video süresi alınamadı, ilerleme çubuğu sınırlı olacak.");
            Console.ResetColor();
        }

        if (videoInfo != null && videoInfo.IsValid())
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"ℹ️  Çözünürlük: {videoInfo.Width}x{videoInfo.Height} | Codec: {videoInfo.Codec} | FPS: {videoInfo.Fps:0.0}");
            Console.ResetColor();
            Console.WriteLine();
        }

        bool success = CompressVideoWithProgress(inputFile, outputFile, duration, fileInfo.Length, videoInfo);

        if (!success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ❌ İŞLEM BAŞARISIZ                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine("\nSıkıştırma işlemi tamamlanamadı. FFmpeg hatası oluştu.");
            Console.ResetColor();
            return false;
        }

        TimeSpan elapsed = DateTime.Now - start;

        Console.WriteLine();

        // Completion animation
        PlayCompletionAnimation();

        // Sıkıştırılmış dosya boyutunu hesapla ve istatistikleri göster
        if (File.Exists(outputFile))
        {
            FileInfo compressedInfo = new FileInfo(outputFile);
            double compressionRatio = (1 - ((double)compressedInfo.Length / fileInfo.Length)) * 100;
            long savedBytes = fileInfo.Length - compressedInfo.Length;
            double speedFactor = duration / elapsed.TotalSeconds;

            DisplayFinalStatistics(fileInfo, compressedInfo, compressionRatio, savedBytes, elapsed, speedFactor);
            return true;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ❌ İŞLEM BAŞARISIZ                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine("\nÇıktı dosyası oluşturulamadı.");
            Console.ResetColor();
            return false;
        }
    }

    static void PlayCompletionAnimation()
    {
        // Sadece Windows'ta çalışır
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            try
            {
                Console.Beep(800, 150);
                System.Threading.Thread.Sleep(50);
                Console.Beep(1000, 150);
                System.Threading.Thread.Sleep(50);
                Console.Beep(1200, 200);
                System.Threading.Thread.Sleep(50);
                Console.Beep(1400, 250);
            }
            catch
            {
                // Beep may not work on all systems
            }
        }
    }

    static void DisplayFinalStatistics(FileInfo original, FileInfo compressed, double ratio, long saved, TimeSpan elapsed, double speed)
    {
        int labelWidth = 24;
        int valueWidth = 20;

        string FormatLine(string label, string value, ConsoleColor? valueColor = null)
        {
            StringBuilder line = new StringBuilder();
            line.Append("║ ");
            line.Append(label.PadRight(labelWidth));
            return line.ToString();
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    ✅ İŞLEM TAMAMLANDI                         ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

        // Original size
        Console.Write(FormatLine("📦 Orijinal Boyut :", ""));
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(FormatFileSize(original.Length).PadLeft(valueWidth));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ║");

        // Compressed size
        Console.Write(FormatLine("📦 Sıkıştırılmış :", ""));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(FormatFileSize(compressed.Length).PadLeft(valueWidth));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ║");

        // Saved space
        Console.Write(FormatLine("💰 Kazanılan Alan :", ""));
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(FormatFileSize(saved).PadLeft(valueWidth));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ║");

        // Compression ratio
        Console.Write(FormatLine("🗜️  Sıkıştırma Oranı :", ""));
        ConsoleColor ratioColor = ratio > 60 ? ConsoleColor.Green : ratio > 40 ? ConsoleColor.Yellow : ConsoleColor.Red;
        Console.ForegroundColor = ratioColor;
        Console.Write($"%{ratio:0.00}".PadLeft(valueWidth));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ║");

        // Processing time
        Console.Write(FormatLine("⏱️  İşlem Süresi :", ""));
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(FormatDuration(elapsed).PadLeft(valueWidth));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ║");

        // Processing speed
        Console.Write(FormatLine("⚡ İşleme Hızı :", ""));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{speed:0.00}x".PadLeft(valueWidth));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ║");

        // Average FPS
        double avgFps = 0;
        lock (historyLock)
        {
            if (fpsHistory.Count > 0)
            {
                avgFps = fpsHistory.Average();
            }
        }

        if (avgFps > 0)
        {
            Console.Write(FormatLine("🎬 Ortalama FPS :", ""));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{avgFps:0.0} fps".PadLeft(valueWidth));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ║");
        }

        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Video başarıyla sıkıştırıldı!");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  📁 Konum: {compressed.FullName}");
        Console.ResetColor();
    }

    static bool CompressVideoWithProgress(string inputFile, string outputFile, double duration, long originalSize, VideoInfo videoInfo)
    {
        try
        {
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = "ffmpeg";
                ffmpeg.StartInfo.Arguments = $"-i \"{inputFile}\" -vcodec libx264 -crf {CRF_QUALITY} -preset veryfast -map_metadata 0 \"{outputFile}\" -y -progress pipe:2 -nostats";
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.CreateNoWindow = true;

                ProgressState state = new ProgressState
                {
                    StartTime = DateTime.Now,
                    Progress = 0,
                    OriginalSize = originalSize,
                    TotalDuration = duration,
                    VideoInfo = videoInfo
                };

                ffmpeg.Start();

                string line;
                while ((line = ffmpeg.StandardError.ReadLine()) != null)
                {
                    // Parse time
                    var timeMatch = Regex.Match(line, @"time=(\d+):(\d+):(\d+)\.(\d+)");
                    if (timeMatch.Success)
                    {
                        int hours = int.Parse(timeMatch.Groups[1].Value);
                        int minutes = int.Parse(timeMatch.Groups[2].Value);
                        int seconds = int.Parse(timeMatch.Groups[3].Value);
                        state.CurrentSeconds = hours * 3600 + minutes * 60 + seconds;
                        state.Progress = Math.Min(state.CurrentSeconds / duration, 1.0);
                    }

                    // Parse bitrate
                    var bitrateMatch = Regex.Match(line, @"bitrate=\s*(\d+\.?\d*)kbits/s");
                    if (bitrateMatch.Success)
                    {
                        state.Bitrate = double.Parse(bitrateMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        lock (historyLock)
                        {
                            bitrateHistory.Add(state.Bitrate);
                            if (bitrateHistory.Count > 50) bitrateHistory.RemoveAt(0);
                        }
                    }

                    // Parse fps
                    var fpsMatch = Regex.Match(line, @"fps=\s*(\d+\.?\d*)");
                    if (fpsMatch.Success)
                    {
                        state.Fps = double.Parse(fpsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        state.FrameCount++;
                        lock (historyLock)
                        {
                            fpsHistory.Add(state.Fps);
                            if (fpsHistory.Count > 50) fpsHistory.RemoveAt(0);
                        }
                    }

                    // Parse frame number
                    var frameMatch = Regex.Match(line, @"frame=\s*(\d+)");
                    if (frameMatch.Success)
                    {
                        state.CurrentFrame = int.Parse(frameMatch.Groups[1].Value);
                    }

                    // Parse size
                    var sizeMatch = Regex.Match(line, @"total_size=(\d+)");
                    if (sizeMatch.Success)
                    {
                        state.ProcessedBytes = long.Parse(sizeMatch.Groups[1].Value);
                    }

                    // Throttle progress bar updates
                    if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds >= PROGRESS_UPDATE_THROTTLE_MS)
                    {
                        lastProgressUpdate = DateTime.Now;

                        // Draw progress based on selected style
                        switch (PROGRESS_BAR_STYLE)
                        {
                            case 1:
                                DrawEnhancedProgressBar(state, duration);
                                break;
                            case 2:
                                DrawMinimalProgressBar(state, duration);
                                break;
                        }
                    }
                }

                ffmpeg.WaitForExit();

                // Check exit code
                if (ffmpeg.ExitCode != 0)
                {
                    return false;
                }

                // Show 100% completion
                state.Progress = 1.0;
                state.CurrentSeconds = duration;

                switch (PROGRESS_BAR_STYLE)
                {
                    case 1:
                        DrawEnhancedProgressBar(state, duration);
                        Console.WriteLine("\n\n");
                        break;
                    case 2:
                        DrawMinimalProgressBar(state, duration);
                        Console.WriteLine("\n");
                        break;
                }

                // Copy file dates
                CopyFileDates(inputFile, outputFile);

                return true;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Hata: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    // İlk çizim için başlangıç pozisyonunu sakla
    private static int progressBarStartLine = -1;

    // STYLE 1: Enhanced Progress Bar
    static void DrawEnhancedProgressBar(ProgressState state, double totalDuration)
    {
        int barWidth = 50;
        int filledWidth = (int)(state.Progress * barWidth);
        filledWidth = Math.Min(filledWidth, barWidth);

        TimeSpan elapsed = DateTime.Now - state.StartTime;
        string eta = CalculateETA(state.Progress, elapsed);
        TimeSpan current = TimeSpan.FromSeconds(state.CurrentSeconds);
        TimeSpan total = TimeSpan.FromSeconds(totalDuration);
        string timeString = $"{(int)current.TotalMinutes:D2}:{current.Seconds:D2} / {(int)total.TotalMinutes:D2}:{total.Seconds:D2}";

        StringBuilder bar = new StringBuilder();
        char[] spinner = { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
        char spinnerChar = spinner[state.FrameCount % spinner.Length];

        for (int i = 0; i < barWidth; i++)
        {
            if (i < filledWidth)
            {
                if (i == filledWidth - 1 && filledWidth < barWidth)
                    bar.Append('▓');
                else
                    bar.Append('█');
            }
            else
            {
                bar.Append('░');
            }
        }

        try
        {
            // İlk çizimde pozisyonu kaydet
            if (progressBarStartLine == -1)
            {
                progressBarStartLine = Console.CursorTop;
            }
            else
            {
                // Kaydedilmiş pozisyona git
                Console.SetCursorPosition(0, progressBarStartLine);
            }

            // 4 satırı temizle
            for (int i = 0; i < 4; i++)
            {
                Console.Write(new string(' ', Math.Min(Console.WindowWidth - 1, 120)));
                if (i < 3) Console.WriteLine();
            }

            // Başa dön
            Console.SetCursorPosition(0, progressBarStartLine);

            // Satır 1: Progress Bar
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{spinnerChar} Süreç: ");
            Console.ForegroundColor = state.Progress < 0.3 ? ConsoleColor.Red :
                                       state.Progress < 0.7 ? ConsoleColor.Yellow :
                                       ConsoleColor.Green;
            Console.Write($"[{bar}]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($" {state.Progress * 100:0.0}%");
            Console.ResetColor();
            Console.WriteLine();

            // Satır 2: Time, ETA, Speed
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("⏱ Süre: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{timeString}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  │  ⏳ Kalan Süre: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{eta}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  │  🎬 Hız: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{state.Fps:0.0} fps");
            Console.ResetColor();
            Console.WriteLine();

            // Satır 3: Frame, Bitrate, Processed
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("📊 Kare: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{state.CurrentFrame:N0}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  │  💾 Bitrate: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{state.Bitrate:0.0} kb/s");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  │  📦 İşlenen: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(FormatFileSize(state.ProcessedBytes));
            Console.ResetColor();
            Console.WriteLine();

            // Satır 4: Est. Compression, Elapsed
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("🗜 Tahmini Sıkıştırma: ");

            if (state.ProcessedBytes > 0 && state.Progress > 0.05)
            {
                long estimatedFinalSize = (long)(state.ProcessedBytes / state.Progress);
                double estimatedRatio = (1 - ((double)estimatedFinalSize / state.OriginalSize)) * 100;
                Console.ForegroundColor = estimatedRatio > 50 ? ConsoleColor.Green : ConsoleColor.Yellow;
                Console.Write($"~{estimatedRatio:0.0}%");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("Hesaplanıyor...");
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("  │  ⏰ Geçen süre: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(FormatDuration(elapsed));
            Console.ResetColor();
        }
        catch
        {
            // Cursor positioning failed, continue anyway
        }
    }

    // STYLE 2: Minimal Progress Bar
    static void DrawMinimalProgressBar(ProgressState state, double totalDuration)
    {
        int totalBlocks = 40;
        int filledBlocks = (int)(state.Progress * totalBlocks);
        filledBlocks = Math.Min(filledBlocks, totalBlocks);
        string bar = new string('#', filledBlocks) + new string('-', totalBlocks - filledBlocks);

        try
        {
            Console.CursorLeft = 0;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{bar}] {state.Progress * 100:0.0}% | {state.Fps:0.0} fps | {state.Bitrate:0.0} kb/s");
            Console.ResetColor();
        }
        catch
        {
            // Cursor positioning failed
        }
    }

    static void ClearLines(int lineCount)
    {
        try
        {
            int currentTop = Console.CursorTop;
            // Negatif pozisyon kontrolü
            if (currentTop < lineCount)
            {
                return; // Buffer'ın üstüne çıkamazsınız
            }

            Console.SetCursorPosition(0, currentTop);
            for (int i = 0; i < lineCount; i++)
            {
                Console.Write(new string(' ', Math.Min(Console.WindowWidth - 1, 120)));
                if (i < lineCount - 1) Console.Write("\n");
            }

            int targetTop = Math.Max(0, currentTop - (lineCount - 1));
            Console.SetCursorPosition(0, targetTop);
        }
        catch
        {
            // Ignore cursor positioning errors
        }
    }

    static string CalculateETA(double progress, TimeSpan elapsed)
    {
        if (progress <= 0.01)
        {
            return "Calculating...";
        }

        double totalEstimatedSeconds = elapsed.TotalSeconds / progress;
        double remainingSeconds = totalEstimatedSeconds - elapsed.TotalSeconds;

        if (remainingSeconds < 0)
        {
            return "00:00";
        }

        TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        else
            return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    static double GetVideoDuration(string filePath)
    {
        try
        {
            using (Process ffprobe = new Process())
            {
                ffprobe.StartInfo.FileName = "ffprobe";
                ffprobe.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"";
                ffprobe.StartInfo.UseShellExecute = false;
                ffprobe.StartInfo.RedirectStandardOutput = true;
                ffprobe.StartInfo.CreateNoWindow = true;
                ffprobe.Start();

                string output = ffprobe.StandardOutput.ReadToEnd().Trim();
                ffprobe.WaitForExit();

                if (double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds))
                    return seconds;
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }

    static VideoInfo GetVideoInfo(string filePath)
    {
        try
        {
            using (Process ffprobe = new Process())
            {
                ffprobe.StartInfo.FileName = "ffprobe";
                ffprobe.StartInfo.Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,codec_name,r_frame_rate -of default=noprint_wrappers=1 \"{filePath}\"";
                ffprobe.StartInfo.UseShellExecute = false;
                ffprobe.StartInfo.RedirectStandardOutput = true;
                ffprobe.StartInfo.CreateNoWindow = true;
                ffprobe.Start();

                VideoInfo info = new VideoInfo();
                string line;
                while ((line = ffprobe.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith("width="))
                    {
                        if (int.TryParse(line.Substring(6), out int width))
                        {
                            info.Width = width;
                        }
                    }
                    else if (line.StartsWith("height="))
                    {
                        if (int.TryParse(line.Substring(7), out int height))
                        {
                            info.Height = height;
                        }
                    }
                    else if (line.StartsWith("codec_name="))
                    {
                        info.Codec = line.Substring(11);
                    }
                    else if (line.StartsWith("r_frame_rate="))
                    {
                        string fps = line.Substring(13);
                        var parts = fps.Split('/');
                        if (parts.Length == 2 && double.TryParse(parts[0], out double num) && double.TryParse(parts[1], out double den) && den != 0)
                        {
                            info.Fps = num / den;
                        }
                    }
                }

                ffprobe.WaitForExit();
                return info;
            }
        }
        catch
        {
            return null;
        }
    }

    static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}s {ts.Minutes}d {ts.Seconds}sn";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}d {ts.Seconds}sn";
        return $"{ts.Seconds}sn";
    }

    static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    static void CopyFileDates(string sourceFile, string destinationFile)
    {
        try
        {
            FileInfo sourceInfo = new FileInfo(sourceFile);
            FileInfo destInfo = new FileInfo(destinationFile);

            destInfo.CreationTime = sourceInfo.CreationTime;
            destInfo.LastWriteTime = sourceInfo.LastWriteTime;
            destInfo.LastAccessTime = sourceInfo.LastAccessTime;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nUyarı: Dosya tarihleri kopyalanamadı: {ex.Message}");
            Console.ResetColor();
        }
    }
}

// Progress state tracking class
class ProgressState
{
    public double Progress { get; set; }
    public double CurrentSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public long ProcessedBytes { get; set; }
    public double Bitrate { get; set; }
    public double Fps { get; set; }
    public int FrameCount { get; set; }
    public int CurrentFrame { get; set; }
    public long OriginalSize { get; set; }
    public double TotalDuration { get; set; }
    public VideoInfo VideoInfo { get; set; }
}

// Video information class
class VideoInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Codec { get; set; }
    public double Fps { get; set; }

    // Null/invalid check method
    public bool IsValid()
    {
        return Width > 0 && Height > 0 && !string.IsNullOrEmpty(Codec) && Fps > 0;
    }
}
