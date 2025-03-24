using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;
using NAudio.Lame;
using NAudio.Wave;
using System.Diagnostics;

namespace WaveToMp3Converter
{
    public class Program
    {
        private static readonly string LogFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "WaveToMp3Converter.log");

        public static void Main(string[] args)
        {
            // サービスとして実行するかどうかを判断
            if (Environment.UserInteractive)
            {
                // コンソールモードとして実行
                RunAsConsole(args);
            }
            else
            {
                // Windowsサービスとして実行
                ServiceBase[] ServicesToRun = new ServiceBase[]
                {
                    new Mp3ConverterService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private static void RunAsConsole(string[] args)
        {
            LogMessage("WaveからMP3への変換ツールを起動しました。");

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            try
            {
                ParseAndExecuteCommands(args);
            }
            catch (Exception ex)
            {
                LogMessage($"エラーが発生しました: {ex.Message}", EventLogEntryType.Error);
                Console.WriteLine($"エラー: {ex.Message}");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("WAVEからMP3への変換ツール - 使用方法:");
            Console.WriteLine("  -f, --file <WAVEファイルパス>: 変換する単一のWAVEファイルを指定します");
            Console.WriteLine("  -d, --directory <ディレクトリパス>: 指定されたディレクトリ内のすべてのWAVEファイルを変換します");
            Console.WriteLine("  -o, --output <出力ディレクトリ>: 変換されたMP3ファイルの出力先ディレクトリを指定します");
            Console.WriteLine("  -w, --watch <監視ディレクトリ>: 指定されたディレクトリを監視し、新しいWAVEファイルを自動変換します");
            Console.WriteLine("  -h, --help: このヘルプを表示します");
            Console.WriteLine();
            Console.WriteLine("例:");
            Console.WriteLine("  WaveToMp3Converter.exe -f \"C:\\Music\\song.wav\"");
            Console.WriteLine("  WaveToMp3Converter.exe -d \"C:\\Music\" -o \"C:\\MP3s\"");
            Console.WriteLine("  WaveToMp3Converter.exe -w \"C:\\WatchFolder\" -o \"C:\\OutputFolder\"");
        }

        private static void ParseAndExecuteCommands(string[] args)
        {
            string inputFile = null;
            string inputDirectory = null;
            string outputDirectory = null;
            string watchDirectory = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-f":
                    case "--file":
                        if (i + 1 < args.Length) inputFile = args[++i];
                        break;

                    case "-d":
                    case "--directory":
                        if (i + 1 < args.Length) inputDirectory = args[++i];
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length) outputDirectory = args[++i];
                        break;

                    case "-w":
                    case "--watch":
                        if (i + 1 < args.Length) watchDirectory = args[++i];
                        break;

                    case "-h":
                    case "--help":
                        ShowHelp();
                        return;
                }
            }

            // 出力ディレクトリが指定されていない場合は入力と同じ場所を使用
            if (string.IsNullOrEmpty(outputDirectory))
            {
                if (!string.IsNullOrEmpty(inputFile))
                {
                    outputDirectory = Path.GetDirectoryName(inputFile);
                }
                else if (!string.IsNullOrEmpty(inputDirectory))
                {
                    outputDirectory = inputDirectory;
                }
                else if (!string.IsNullOrEmpty(watchDirectory))
                {
                    outputDirectory = watchDirectory;
                }
            }

            // コマンドを実行
            if (!string.IsNullOrEmpty(inputFile))
            {
                // 単一ファイルの変換
                ConvertWaveToMp3(inputFile, Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFile) + ".mp3"));
            }
            else if (!string.IsNullOrEmpty(inputDirectory))
            {
                // ディレクトリ内のすべてのWAVEファイルを変換
                ConvertAllWaveFilesInDirectory(inputDirectory, outputDirectory);
            }
            else if (!string.IsNullOrEmpty(watchDirectory))
            {
                // ディレクトリを監視
                WatchDirectory(watchDirectory, outputDirectory);
            }
            else
            {
                ShowHelp();
            }
        }

        public static void ConvertWaveToMp3(string waveFile, string mp3File)
        {
            try
            {
                LogMessage($"変換開始: {waveFile} -> {mp3File}");
                Console.WriteLine($"変換中: {Path.GetFileName(waveFile)}");

                using (var reader = new AudioFileReader(waveFile))
                using (var writer = new LameMP3FileWriter(mp3File, reader.WaveFormat, LAMEPreset.STANDARD))
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

        private static void ConvertAllWaveFilesInDirectory(string inputDirectory, string outputDirectory)
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

        private static void WatchDirectory(string watchDirectory, string outputDirectory)
        {
            if (!Directory.Exists(watchDirectory))
            {
                throw new DirectoryNotFoundException($"監視ディレクトリが見つかりません: {watchDirectory}");
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // 処理済みファイルを追跡するためのディクショナリ (ファイルパス -> 最終更新時刻)
            Dictionary<string, DateTime> processedFiles = new Dictionary<string, DateTime>();

            // ファイル検索の対象となる拡張子
            string searchPattern = "*.wav";

            // 高精度タイマーを使用してポーリング
            System.Timers.Timer pollingTimer = new System.Timers.Timer
            {
                Interval = 1000 // 1秒ごとにポーリング（必要に応じて調整可能）
            };

            LogMessage($"ディレクトリポーリング監視を開始しました: {watchDirectory}");
            Console.WriteLine($"ディレクトリポーリング監視を開始しました: {watchDirectory}");
            Console.WriteLine("新しいWAVEファイルを待機中... (終了するには Ctrl+C を押してください)");

            // ファイル処理用のキュー
            ConcurrentQueue<string> processingQueue = new ConcurrentQueue<string>();

            // 別スレッドでファイル処理を行うためのタスク
            Task fileProcessingTask = Task.Run(() =>
            {
                while (true)
                {
                    if (processingQueue.TryDequeue(out string filePath))
                    {
                        try
                        {
                            // ファイルの相対パスを取得
                            string relativePath = filePath.Substring(watchDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                            string outputPath = Path.Combine(outputDirectory, Path.ChangeExtension(relativePath, ".mp3"));

                            // 出力ディレクトリが存在するか確認
                            string outputDir = Path.GetDirectoryName(outputPath);
                            if (!Directory.Exists(outputDir))
                            {
                                Directory.CreateDirectory(outputDir);
                            }

                            ConvertWaveToMp3(filePath, outputPath);
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"ファイル処理中にエラーが発生しました: {ex.Message}", EventLogEntryType.Error);
                            Console.WriteLine($"エラー: {ex.Message}");
                        }
                    }
                    else
                    {
                        // キューが空の場合は少し待機
                        Thread.Sleep(100);
                    }
                }
            });

            // ポーリング処理
            pollingTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    // すべてのWAVEファイルを検索（サブディレクトリ含む）
                    string[] files = Directory.GetFiles(watchDirectory, searchPattern, SearchOption.AllDirectories);

                    foreach (string filePath in files)
                    {
                        try
                        {
                            // ファイルの最終更新時刻を取得
                            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                            string mp3Path = Path.ChangeExtension(filePath.Replace(watchDirectory, outputDirectory), ".mp3");

                            // ファイルが既に処理済みかチェック
                            if (processedFiles.TryGetValue(filePath, out DateTime processedTime))
                            {
                                // 更新時刻が変わっていなければスキップ
                                if (lastWriteTime <= processedTime)
                                {
                                    continue;
                                }
                            }

                            // ファイルが完全に書き込まれたかを確認（ファイルロックチェック）
                            if (!IsFileReady(filePath))
                            {
                                continue;
                            }

                            // すでにMP3が存在し、WAVよりも新しい場合はスキップ
                            if (File.Exists(mp3Path) && File.GetLastWriteTime(mp3Path) >= lastWriteTime)
                            {
                                processedFiles[filePath] = lastWriteTime;
                                continue;
                            }

                            // 処理キューに追加
                            processingQueue.Enqueue(filePath);

                            // 処理済みとしてマーク
                            processedFiles[filePath] = lastWriteTime;

                            LogMessage($"新しいファイルを検出しました: {filePath}");
                            Console.WriteLine($"新しいファイルを検出: {Path.GetFileName(filePath)}");
                        }
                        catch (IOException ex)
                        {
                            // ファイルアクセス中の例外は無視（次のポーリングで再試行）
                            LogMessage($"ファイルアクセス中に例外が発生しました（スキップします）: {ex.Message}", EventLogEntryType.Warning);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            // アクセス権限の問題は記録
                            LogMessage($"アクセス権限エラー: {ex.Message}", EventLogEntryType.Error);
                        }
                    }

                    // 定期的に古いエントリをクリーンアップ（任意）
                    DateTime cleanupThreshold = DateTime.Now.AddHours(-24); // 24時間以上前のエントリを削除
                    var keysToRemove = processedFiles.Where(kv => kv.Value < cleanupThreshold).Select(kv => kv.Key).ToList();
                    foreach (var key in keysToRemove)
                    {
                        processedFiles.Remove(key);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"ディレクトリ監視中にエラーが発生しました: {ex.Message}", EventLogEntryType.Error);
                    Console.WriteLine($"監視エラー: {ex.Message}");
                }
            };

            // ファイルが使用中かどうかをチェックするヘルパーメソッド
            bool IsFileReady(string filePath)
            {
                try
                {
                    // ファイルが存在するか確認
                    if (!File.Exists(filePath))
                    {
                        return false;
                    }

                    // ファイルが空でないか確認
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == 0)
                    {
                        return false;
                    }

                    // ファイルが読み取り可能かどうかを確認
                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    // ファイルがロックされている場合
                    return false;
                }
                catch
                {
                    // その他のエラー
                    return false;
                }
            }

            // タイマーを開始
            pollingTimer.Start();

            // Ctrl+C で終了できるようにする
            Console.CancelKeyPress += (sender, e) =>
            {
                pollingTimer.Stop();
                LogMessage("ディレクトリポーリング監視を停止しました。");
                Console.WriteLine("ディレクトリポーリング監視を停止しました。");
            };

            // メインスレッドをブロック
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            resetEvent.WaitOne();
        }

        public static void LogMessage(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logLevel = type.ToString();
            string logMessage = $"[{timestamp}] [{logLevel}] {message}";

            try
            {
                // ファイルにログを書き込む
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);

                // イベントログにも記録（サービスとして実行時に有効）
                try
                {
                    if (!EventLog.SourceExists("WaveToMp3Converter"))
                    {
                        EventLog.CreateEventSource("WaveToMp3Converter", "Application");
                    }
                    EventLog.WriteEntry("WaveToMp3Converter", message, type);
                }
                catch
                {
                    // イベントログへの書き込みに失敗しても処理を続行
                }
            }
            catch
            {
                // ロギング失敗時は無視して処理を続行
            }
        }
    }

    // Windowsサービスクラス
    public class Mp3ConverterService : ServiceBase
    {
        private System.Timers.Timer _pollingTimer;
        private string _watchDirectory;
        private string _outputDirectory;
        private Dictionary<string, DateTime> _processedFiles;
        private ConcurrentQueue<string> _processingQueue;
        private Task _fileProcessingTask;
        private CancellationTokenSource _cancellationTokenSource;

        public Mp3ConverterService()
        {
            ServiceName = "WaveToMp3ConverterService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            // 設定ファイルから監視ディレクトリと出力ディレクトリを読み込む
            _watchDirectory = ConfigurationManager.AppSettings["WatchDirectory"];
            _outputDirectory = ConfigurationManager.AppSettings["OutputDirectory"];

            // 設定ファイルからポーリング間隔を読み込む（デフォルトは1000ms）
            int pollingInterval = 1000;
            if (int.TryParse(ConfigurationManager.AppSettings["PollingInterval"], out int configInterval) && configInterval > 0)
            {
                pollingInterval = configInterval;
            }

            if (string.IsNullOrEmpty(_watchDirectory) || !Directory.Exists(_watchDirectory))
            {
                Program.LogMessage($"監視ディレクトリが無効です: {_watchDirectory}", EventLogEntryType.Error);
                Stop();
                return;
            }

            if (string.IsNullOrEmpty(_outputDirectory))
            {
                _outputDirectory = _watchDirectory;
            }

            if (!Directory.Exists(_outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_outputDirectory);
                }
                catch (Exception ex)
                {
                    Program.LogMessage($"出力ディレクトリの作成に失敗しました: {ex.Message}", EventLogEntryType.Error);
                    Stop();
                    return;
                }
            }

            // 処理済みファイルを追跡
            _processedFiles = new Dictionary<string, DateTime>();

            // ファイル処理キュー
            _processingQueue = new ConcurrentQueue<string>();

            // キャンセルトークン
            _cancellationTokenSource = new CancellationTokenSource();

            // ファイル処理タスクを開始
            _fileProcessingTask = Task.Run(() => ProcessFiles(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            // ポーリングタイマーを設定
            _pollingTimer = new System.Timers.Timer(pollingInterval);
            _pollingTimer.Elapsed += OnTimerElapsed;
            _pollingTimer.Start();

            Program.LogMessage($"サービスを開始しました。監視ディレクトリ: {_watchDirectory}, 出力ディレクトリ: {_outputDirectory}, ポーリング間隔: {pollingInterval}ms");
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // すべてのWAVEファイルを検索（サブディレクトリ含む）
                string[] files = Directory.GetFiles(_watchDirectory, "*.wav", SearchOption.AllDirectories);

                foreach (string filePath in files)
                {
                    try
                    {
                        // ファイルの最終更新時刻を取得
                        DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                        string mp3Path = Path.ChangeExtension(filePath.Replace(_watchDirectory, _outputDirectory), ".mp3");

                        // ファイルが既に処理済みかチェック
                        if (_processedFiles.TryGetValue(filePath, out DateTime processedTime))
                        {
                            // 更新時刻が変わっていなければスキップ
                            if (lastWriteTime <= processedTime)
                            {
                                continue;
                            }
                        }

                        // ファイルが完全に書き込まれたかを確認
                        if (!IsFileReady(filePath))
                        {
                            continue;
                        }

                        // すでにMP3が存在し、WAVよりも新しい場合はスキップ
                        if (File.Exists(mp3Path) && File.GetLastWriteTime(mp3Path) >= lastWriteTime)
                        {
                            _processedFiles[filePath] = lastWriteTime;
                            continue;
                        }

                        // 処理キューに追加
                        _processingQueue.Enqueue(filePath);

                        // 処理済みとしてマーク
                        _processedFiles[filePath] = lastWriteTime;

                        Program.LogMessage($"新しいファイルを検出しました: {filePath}");
                    }
                    catch (IOException)
                    {
                        // ファイルアクセス中の例外は無視（次のポーリングで再試行）
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Program.LogMessage($"アクセス権限エラー: {ex.Message}", EventLogEntryType.Error);
                    }
                }

                // 定期的に古いエントリをクリーンアップ
                DateTime cleanupThreshold = DateTime.Now.AddHours(-24);
                var keysToRemove = _processedFiles.Where(kv => kv.Value < cleanupThreshold).Select(kv => kv.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    _processedFiles.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"ポーリング中にエラーが発生しました: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void ProcessFiles(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_processingQueue.TryDequeue(out string filePath))
                {
                    try
                    {
                        // ファイルの相対パスを取得
                        string relativePath = filePath.Substring(_watchDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                        string outputPath = Path.Combine(_outputDirectory, Path.ChangeExtension(relativePath, ".mp3"));

                        // 出力ディレクトリが存在するか確認
                        string outputDir = Path.GetDirectoryName(outputPath);
                        if (!Directory.Exists(outputDir))
                        {
                            Directory.CreateDirectory(outputDir);
                        }

                        Program.ConvertWaveToMp3(filePath, outputPath);
                    }
                    catch (Exception ex)
                    {
                        Program.LogMessage($"ファイル処理中にエラーが発生しました: {ex.Message}", EventLogEntryType.Error);
                    }
                }
                else
                {
                    // キューが空の場合は少し待機
                    Thread.Sleep(100);
                }
            }
        }

        // ファイルが使用中かどうかをチェックするヘルパーメソッド
        private bool IsFileReady(string filePath)
        {
            try
            {
                // ファイルが存在するか確認
                if (!File.Exists(filePath))
                {
                    return false;
                }

                // ファイルが空でないか確認
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return false;
                }

                // ファイルが読み取り可能かどうかを確認
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return true;
                }
            }
            catch
            {
                // エラーが発生した場合、ファイルはまだ準備ができていないと見なす
                return false;
            }
        }

        protected override void OnStop()
        {
            // タイマーを停止
            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }

            // ファイル処理タスクをキャンセル
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                try
                {
                    // タスクの完了を最大5秒待機
                    _fileProcessingTask?.Wait(5000);
                }
                catch (AggregateException)
                {
                    // タスクがキャンセルされたときの例外は無視
                }
                finally
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }

            Program.LogMessage("サービスを停止しました。");
        }
    }
}