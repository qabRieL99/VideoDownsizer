using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=== iPhone Video Sıkıştırıcı ===");
        Console.ResetColor();
        Console.WriteLine("Bu program FFmpeg kullanarak videoları sıkıştırır.\n");

        // Eğer dosya sürüklenmiş ve bırakılmışsa, işle ve çık
        if (args.Length > 0)
        {
            string inputFile = args[0].Trim('"', '\'');
            ProcessVideo(inputFile);
            Console.WriteLine("\nÇıkmak için bir tuşa basın...");
            Console.ReadKey();
            return;
        }

        // Sürekli döngü - konsola sürükle bırak modu
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n" + new string('─', 50));
            Console.WriteLine("Video dosyasını buraya sürükleyip ENTER'a basın");
            Console.WriteLine("(Çıkmak için 'q' yazın)");
            Console.WriteLine(new string('─', 50));
            Console.ResetColor();
            Console.Write("\nDosya yolu: ");

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

            string inputFile = input.Trim('"', '\'');
            ProcessVideo(inputFile);
        }
    }

    static void ProcessVideo(string inputFile)
    {

        if (!File.Exists(inputFile))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Dosya bulunamadı!");
            Console.ResetColor();
            return;
        }

        string[] videoExtensions = { ".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v", ".hevc" };
        string extension = Path.GetExtension(inputFile).ToLower();

        if (!Array.Exists(videoExtensions, ext => ext == extension))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Bu dosya bir video dosyası değil!");
            Console.ResetColor();
            return;
        }

        string directory = Path.GetDirectoryName(inputFile);
        string fileName = Path.GetFileNameWithoutExtension(inputFile);
        string outputFile = Path.Combine(directory, fileName + "_compressed" + extension);

        FileInfo fileInfo = new FileInfo(inputFile);
        string fileSize = FormatFileSize(fileInfo.Length);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nVideo: {Path.GetFileName(inputFile)} ({fileSize})");
        Console.ResetColor();

        double duration = GetVideoDuration(inputFile);
        DateTime start = DateTime.Now;

        if (duration <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Süre alınamadı, ilerleme çubuğu gösterilemeyecek.");
            Console.ResetColor();
        }

        CompressVideoWithProgress(inputFile, outputFile, duration);

        TimeSpan elapsed = DateTime.Now - start;
        string elapsedText = FormatDuration(elapsed);

        Console.WriteLine();

        // Tüm işlemler tamamlandı - ses çal
        Console.Beep(800, 200);
        System.Threading.Thread.Sleep(100);
        Console.Beep(1000, 200);
        System.Threading.Thread.Sleep(100);
        Console.Beep(1200, 300);

        // Sıkıştırılmış dosya boyutunu hesapla ve istatistikleri göster
        if (File.Exists(outputFile))
        {
            FileInfo compressedInfo = new FileInfo(outputFile);
            double compressionRatio = (1 - ((double)compressedInfo.Length / fileInfo.Length)) * 100;

            int labelWidth = 22;
            int valueWidth = 18;

            string FormatLine(string label, string value)
            {
                // Türkçe karakterlerin genişliğini hesapla
                int visualLength = GetVisualLength(label);
                int padding = labelWidth - visualLength;
                return $"║ {label}{new string(' ', padding)}{value.PadLeft(valueWidth)} ║";
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╔" + new string('═', labelWidth + valueWidth + 2) + "╗");
            Console.WriteLine(FormatLine("Orijinal Boyut :", FormatFileSize(fileInfo.Length)));
            Console.WriteLine(FormatLine("Sıkıştırılmış :", FormatFileSize(compressedInfo.Length)));
            Console.WriteLine(FormatLine("Azalma :", $"%{compressionRatio:0.00}"));
            Console.WriteLine(FormatLine("İşlem Süresi :", elapsedText));
            Console.WriteLine("╚" + new string('═', labelWidth + valueWidth + 2) + "╝");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Video başarıyla sıkıştırıldı!");
            Console.WriteLine($"  Konum: {outputFile}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Sıkıştırma işlemi tamamlanamadı.");
            Console.ResetColor();
        }
    }

    static int GetVisualLength(string text)
    {
        // Türkçe karakterler için görsel uzunluğu hesapla
        // Çoğu konsol fontunda Türkçe karakterler normal genişlikte
        return text.Length;
    }

    static void CompressVideoWithProgress(string inputFile, string outputFile, double duration)
    {
        Process ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = "ffmpeg";
        ffmpeg.StartInfo.Arguments = $"-i \"{inputFile}\" -vcodec libx264 -crf 26 -preset veryfast -map_metadata 0 \"{outputFile}\" -y -progress pipe:2 -nostats";
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.RedirectStandardError = true;
        ffmpeg.StartInfo.RedirectStandardOutput = true;
        ffmpeg.StartInfo.CreateNoWindow = true;

        ffmpeg.Start();

        string line;
        double lastProgress = 0;
        while ((line = ffmpeg.StandardError.ReadLine()) != null)
        {
            var match = Regex.Match(line, @"time=(\d+):(\d+):(\d+).(\d+)");
            if (match.Success)
            {
                int hours = int.Parse(match.Groups[1].Value);
                int minutes = int.Parse(match.Groups[2].Value);
                int seconds = int.Parse(match.Groups[3].Value);
                double totalSeconds = hours * 3600 + minutes * 60 + seconds;

                double progress = Math.Min(totalSeconds / duration, 1.0);
                lastProgress = progress;
                DrawProgressBar(progress);
            }
        }

        ffmpeg.WaitForExit();

        // İşlem bittiyse %100 göster
        if (lastProgress < 1.0)
        {
            DrawProgressBar(1.0);
        }

        Console.WriteLine();

        // Dosya tarihlerini kopyala
        CopyFileDates(inputFile, outputFile);
    }

    static double GetVideoDuration(string filePath)
    {
        Process ffprobe = new Process();
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

        return 0;
    }

    static void DrawProgressBar(double progress)
    {
        int totalBlocks = 40;
        int filledBlocks = (int)(progress * totalBlocks);
        filledBlocks = Math.Min(filledBlocks, totalBlocks);
        string bar = new string('#', filledBlocks) + new string('-', totalBlocks - filledBlocks);

        Console.CursorLeft = 0;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{bar}] {progress * 100:0.0}%");
        Console.ResetColor();
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

            // Tüm tarih bilgilerini kopyala
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
