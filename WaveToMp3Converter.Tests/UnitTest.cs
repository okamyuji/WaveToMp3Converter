using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using WaveToMp3Converter.Tests;
using Xunit.Abstractions;
using Xunit;

public class ProgramTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDirectory;
    private readonly string _outputDirectory;
    private readonly string _watchDirectory;
    private readonly string _testWaveFile;
    private List<IDisposable> _disposables = new List<IDisposable>();

    public ProgramTests(ITestOutputHelper output)
    {
        _output = output;

        // テスト用のディレクトリを作成
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WaveToMp3ConverterTests_{Guid.NewGuid()}");
        _outputDirectory = Path.Combine(_testDirectory, "Output");
        _watchDirectory = Path.Combine(_testDirectory, "Watch");

        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_outputDirectory);
        Directory.CreateDirectory(_watchDirectory);

        // テスト用のWAVEファイルを作成
        _testWaveFile = Path.Combine(_testDirectory, "test.wav");
        CreateTestWaveFile(_testWaveFile);
    }

    public void Dispose()
    {
        // 先にすべてのDisposableオブジェクトを解放
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _output.WriteLine($"リソース解放中にエラーが発生しました: {ex.Message}");
            }
        }

        // ファイルハンドルが閉じられるまで少し待機
        Thread.Sleep(500);

        // テスト終了時にテスト用ディレクトリを削除
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"テストディレクトリの削除に失敗しました: {ex.Message}");

            // ファイルが使用中である場合、どのファイルが問題かを特定
            try
            {
                foreach (var file in Directory.GetFiles(_testDirectory, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        // ファイルを開いて閉じることでロックされているかチェック
                        using (var fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            // ファイルはロックされていない
                        }
                    }
                    catch
                    {
                        _output.WriteLine($"ファイルがロックされています: {file}");
                    }
                }
            }
            catch
            {
                // ディレクトリ列挙中のエラーは無視
            }
        }
    }

    // テスト用のWAVEファイルを作成するヘルパーメソッド
    private void CreateTestWaveFile(string filePath)
    {
        // 1秒間の無音WAVEファイルを作成
        using (var writer = new WaveFileWriter(filePath, new WaveFormat(44100, 1)))
        {
            var silence = new byte[44100 * 2]; // 1秒間の無音（16ビット）
            writer.Write(silence, 0, silence.Length);
        }
    }

    // テスト内でReaderやその他のDisposableオブジェクトを追跡するヘルパーメソッド
    private T TrackDisposable<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    [Fact]
    public void ConvertWaveToMp3_SuccessfulConversion_CreatesValidMp3File()
    {
        // 準備
        string outputFile = Path.Combine(_outputDirectory, "test.mp3");

        // 実行
        ProgramForTest.ConvertWaveToMp3(_testWaveFile, outputFile);

        // 検証
        Assert.True(File.Exists(outputFile), "MP3ファイルが作成されていません");
        Assert.True(new FileInfo(outputFile).Length > 0, "MP3ファイルが空です");

        // NAudioを使用してファイルを開いて有効なMP3かどうかを確認
        var reader = TrackDisposable(new Mp3FileReader(outputFile));
        Assert.NotNull(reader);
    }

    // 他のテストメソッドも同様に、IDisposableオブジェクトをTrackDisposableでラップ
}