using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using NAudio.Wave;
using Moq;
using System.Reflection;

// テスト用の簡易的なLameMP3FileWriterモック
namespace NAudio.Lame
{
    public enum LAMEPreset
    {
        STANDARD
    }

    public class LameMP3FileWriter : Stream
    {
        private FileStream _outputStream;

        public LameMP3FileWriter(string outputPath, WaveFormat format, LAMEPreset preset)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            _outputStream = File.Create(outputPath);
            // テスト用に何かデータを書き込む
            byte[] dummyData = new byte[1024];
            new Random().NextBytes(dummyData);
            _outputStream.Write(dummyData, 0, dummyData.Length);
        }

        public override bool CanRead => _outputStream.CanRead;
        public override bool CanSeek => _outputStream.CanSeek;
        public override bool CanWrite => _outputStream.CanWrite;
        public override long Length => _outputStream.Length;

        public override long Position
        {
            get => _outputStream.Position;
            set => _outputStream.Position = value;
        }

        public override void Flush() => _outputStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _outputStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _outputStream.Seek(offset, origin);

        public override void SetLength(long value) => _outputStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _outputStream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing && _outputStream != null)
            {
                _outputStream.Dispose();
                _outputStream = null;
            }
            base.Dispose(disposing);
        }
    }
}

namespace WaveToMp3Converter.Tests
{
    // テスト用簡易的なMp3FileReader
    public class Mp3FileReader : IDisposable
    {
        private FileStream _fileStream;

        public Mp3FileReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("ファイルが見つかりません", filePath);
            }
            _fileStream = File.OpenRead(filePath);
        }

        public void Dispose()
        {
            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }
        }
    }

    // ProgramForTestクラスのConvertWaveToMp3メソッドを修正
    public class ProgramForTest
    {
        private static readonly string LogFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "WaveToMp3Converter.log");

        public static void ConvertWaveToMp3(string waveFile, string mp3File)
        {
            try
            {
                LogMessage($"変換開始: {waveFile} -> {mp3File}");
                Console.WriteLine($"変換中: {Path.GetFileName(waveFile)}");

                if (!File.Exists(waveFile))
                {
                    throw new FileNotFoundException($"入力ファイルが見つかりません: {waveFile}");
                }

                string outputDir = Path.GetDirectoryName(mp3File);
                if (!Directory.Exists(outputDir))
                {
                    throw new DirectoryNotFoundException($"出力ディレクトリが見つかりません: {outputDir}");
                }

                // WAVEファイルを読み込み
                using (var reader = new AudioFileReader(waveFile))
                {
                    // WaveFormatを取得
                    var waveFormat = reader.WaveFormat;

                    // MP3ファイルを作成
                    using (var writer = new NAudio.Lame.LameMP3FileWriter(mp3File, waveFormat, NAudio.Lame.LAMEPreset.STANDARD))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        long totalBytes = reader.Length;
                        long processedBytes = 0;
                        int prevPercentage = 0;

                        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            writer.Write(buffer, 0, bytesRead);

                            // 進行状況を表示
                            processedBytes += bytesRead;
                            int percentage = (int)((double)processedBytes / totalBytes * 100);

                            if (percentage > prevPercentage)
                            {
                                Console.Write($"\r進捗: {percentage}%");
                                prevPercentage = percentage;
                            }
                        }
                        Console.WriteLine();
                    }
                }

                LogMessage($"変換完了: {mp3File}");
                Console.WriteLine($"変換完了: {Path.GetFileName(mp3File)}");
            }
            catch (Exception ex)
            {
                LogMessage($"変換エラー [{waveFile}]: {ex.Message}", EventLogEntryType.Error);
                Console.WriteLine($"変換エラー [{Path.GetFileName(waveFile)}]: {ex.Message}");
                throw;
            }
        }

        // ConvertAllWaveFilesInDirectoryメソッド
        public static void ConvertAllWaveFilesInDirectory(string inputDirectory, string outputDirectory)
        {
            if (!Directory.Exists(inputDirectory))
            {
                throw new DirectoryNotFoundException($"入力ディレクトリが見つかりません: {inputDirectory}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string[] waveFiles = Directory.GetFiles(inputDirectory, "*.wav", SearchOption.AllDirectories);
            LogMessage($"{waveFiles.Length}個のWAVEファイルが見つかりました。");
            Console.WriteLine($"{waveFiles.Length}個のWAVEファイルが見つかりました。");

            foreach (string waveFile in waveFiles)
            {
                string relativePath = waveFile.Substring(inputDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                string outputPath = Path.Combine(outputDirectory, Path.ChangeExtension(relativePath, ".mp3"));

                // 出力ディレクトリが存在するか確認
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                ConvertWaveToMp3(waveFile, outputPath);
            }

            LogMessage("すべてのファイルの変換が完了しました。");
            Console.WriteLine("すべてのファイルの変換が完了しました。");
        }

        // 他のメソッドはそのまま

        public static void LogMessage(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logLevel = type.ToString();
            string logMessage = $"[{timestamp}] [{logLevel}] {message}";

            try
            {
                // ファイルにログを書き込む
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch
            {
                // ロギング失敗時は無視して処理を続行
            }
        }

        // WatchDirectoryメソッド、ParseAndExecuteCommandsメソッドなど、
        // 他のメソッドは必要に応じて追加
    }

    // テストクラスは以前のままで、ProgramForTestを参照
}