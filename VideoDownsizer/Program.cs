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

    // Thread safety for stats history
    private static readonly object historyLock = new object();
    private static List<double> fpsHistory = new List<double>();
    private static List<double> bitrateHistory = new List<double>();

    // Console rendering lock + window tracking
    private static readonly object consoleLock = new();
    private static int lastWindowWidth = 0;
    private static int lastWindowHeight = 0;

    private static DateTime lastProgressUpdate = DateTime.MinValue;
    private const int PROGRESS_UPDATE_THROTTLE_MS = 100; // Progress bar update throttle

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // initialize window size tracking
        try
        {
            lastWindowWidth = Console.WindowWidth;
            lastWindowHeight = Console.WindowHeight;
        }
        catch
        {
            lastWindowWidth = 80;
            lastWindowHeight = 25;
        }

        DisplayBanner();

        // If files were dragged and dropped, process and exit
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

        // Continuous loop - drag & drop mode
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
        Console.WriteLine("╔═══════════════════════════════════════════════════════[...]");
        Console.WriteLine("║                         📖 YARDIM MENÜSÜ                       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════[...]");
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
        Console.WriteLine("╔═══════════════════════════════════════════════════════[...]");
        Console.WriteLine("║                      💻 SİSTEM BİLGİLERİ                       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════[...]");
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
        Console.Write("   ���� İlerleme Stili: ");
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
        Console.WriteLine("╔═══════════════════════════════════════════════════════[...]");
        Console.WriteLine("║              🎨 İLERLEME ÇUBUĞU STİLİ SEÇİN                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════[...]");
        Console.ResetColor();

        Console.WriteLine("┌───────────────────────────────────────────────────────[...]");
        Console.WriteLine("│ [1] ��� Gelişmiş Stil                                           │");
        Console.WriteLine("│     └─ Renkli, 4 satır detaylı görünüm                         │");
        Console.WriteLine("│                                                                │");
        Console.WriteLine("│ [2] ⚡ Minimal Stil                                            │");
        Console.WriteLine("│     └─ Basit, tek satır ilerleme çubuğu                        │");
        Console.WriteLine("└───────────────────────────────────────────────────────[...]");

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
        Console.WriteLine("╔═══════════════════════════════════════════════════════[...]");
        Console.WriteLine("║                   🎬 CRF KALİTE AYARI                          ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════[...]");
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
        Console.WriteLine("  27-32: �'}