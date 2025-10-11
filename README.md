# 📹 iPhone Video Compressor

A simple, user-friendly command-line tool for compressing video files using FFmpeg. Perfect for reducing iPhone video file sizes while maintaining good quality.

## ✨ Features

- **Drag & Drop Interface** - Simply drag video files into the console window
- **Real-time Progress Bar** - Visual feedback during compression
- **Batch Processing** - Process multiple videos in one session
- **Smart Compression** - Uses H.264 codec with optimized settings (CRF 26)
- **Metadata Preservation** - Keeps original video metadata intact
- **Date Preservation** - Maintains original file creation and modification dates
- **Compression Statistics** - Shows before/after file sizes and compression ratio
- **Audio Notification** - Plays a beep sound when compression is complete
- **Multi-format Support** - Works with MP4, MOV, MKV, AVI, WebM, M4V, and HEVC

## 🚀 Quick Start

### Prerequisites

You need FFmpeg installed on your system:

**Windows:**
```bash
winget install FFmpeg
```
Or download from [ffmpeg.org](https://ffmpeg.org/download.html)

**macOS:**
```bash
brew install ffmpeg
```

**Linux:**
```bash
sudo apt install ffmpeg  # Debian/Ubuntu
sudo dnf install ffmpeg  # Fedora
```

### Installation

1. Clone this repository:
```bash
git clone https://github.com/yourusername/iphone-video-compressor.git
cd iphone-video-compressor
```

2. Build the project:
```bash
dotnet build -c Release
```

3. Run the executable:
```bash
cd bin/Release/net6.0  # or your target framework
./VideoCompressor
```

## 💡 Usage

### Method 1: Drag & Drop onto Console
1. Run the program
2. Drag a video file into the console window
3. Press Enter
4. Wait for compression to complete

### Method 2: Drag & Drop onto Executable
- Drag a video file directly onto the `.exe` file
- The program will process it and automatically close when done

### Method 3: Command Line
```bash
VideoCompressor "path/to/your/video.mp4"
```

## 📊 Output

The program creates a compressed video with `_compressed` suffix in the same directory as the original file.

**Example:**
```
Input:  my_video.mp4
Output: my_video_compressed.mp4
```

### Statistics Display
```
╔════════════════════════════════════════╗
║ Orijinal Boyut :          125.50 MB ║
║ Sıkıştırılmış :            45.20 MB ║
║ Azalma :                     %64.00 ║
║ İşlem Süresi :              2d 15sn ║
╚════════════════════════════════════════╝
```

## ⚙️ Compression Settings

- **Codec:** H.264 (libx264)
- **CRF:** 26 (good balance between quality and file size)
- **Preset:** veryfast (faster encoding with slightly larger files)
- **Metadata:** Preserved from original file

### Customization

To modify compression settings, edit the FFmpeg arguments in `CompressVideoWithProgress()`:

```csharp
ffmpeg.StartInfo.Arguments = $"-i \"{inputFile}\" -vcodec libx264 -crf 26 -preset veryfast ...";
```

**CRF values:**
- 18-23: High quality (larger files)
- 23-28: Medium quality (balanced)
- 28-35: Lower quality (smaller files)

**Presets:**
- `ultrafast`, `superfast`, `veryfast`: Faster encoding
- `fast`, `medium`, `slow`: Better compression
- `slower`, `veryslow`: Best compression (much slower)

## 🛠️ Technical Details

- **Language:** C# (.NET)
- **Dependencies:** FFmpeg (external)
- **Platform:** Cross-platform (Windows, macOS, Linux)
- **Target Framework:** .NET 6.0 or higher

## 📝 Requirements

- .NET 6.0 SDK or higher
- FFmpeg installed and accessible from PATH

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [FFmpeg](https://ffmpeg.org/)
- Inspired by the need to compress large iPhone videos

## 📞 Support

If you encounter any issues or have questions, please [open an issue](https://github.com/yourusername/iphone-video-compressor/issues).

---

⭐ If you find this tool helpful, please consider giving it a star!
