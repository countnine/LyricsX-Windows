using System.Runtime.CompilerServices;
using Musebase.Core;
using Musebase.Core.Search;
using Musebase.Core.Translation;
using Musebase.Engine;
using Xunit;

namespace Musebase.Core.Tests;

/// <summary>
/// 코어 텔레메트리 계측 검증(contracts/telemetry-events.md).
/// FakeTelemetry로 이벤트를 캡처해 type/props가 계약과 일치하는지 확인한다.
/// </summary>
public class TelemetryTests
{
    // ---- 테스트 더블 ----

    private sealed class FakeTelemetry : ITelemetry
    {
        private readonly object _lock = new();
        private readonly List<(string Type, IReadOnlyDictionary<string, object?> Props)> _events = [];

        public void Track(string type, IReadOnlyDictionary<string, object?>? props = null)
        {
            lock (_lock) _events.Add((type, props ?? new Dictionary<string, object?>()));
        }

        public int CountOf(string type)
        {
            lock (_lock) return _events.Count(e => e.Type == type);
        }

        public List<IReadOnlyDictionary<string, object?>> AllOf(string type)
        {
            lock (_lock) return _events.Where(e => e.Type == type).Select(e => e.Props).ToList();
        }

        /// <summary>type 이벤트가 발화될 때까지 폴링 대기(비동기 검색 파이프라인 동기화용).</summary>
        public async Task<IReadOnlyDictionary<string, object?>> WaitForAsync(string type, int timeoutMs = 10_000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                lock (_lock)
                {
                    var hit = _events.FirstOrDefault(e => e.Type == type);
                    if (hit.Type is not null) return hit.Props;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException($"텔레메트리 이벤트 미발화: {type}");
        }
    }

    private sealed class FakeSource : INowPlayingSource
    {
        public TrackInfo? CurrentTrack { get; set; }
        public bool IsPlaying { get; set; }
        public event Action<TrackInfo?>? TrackChanged;
        public event Action<bool>? IsPlayingChanged;
        public TimeSpan? GetEstimatedPosition() => TimeSpan.Zero;
        public PlaybackControls GetControls() => PlaybackControls.None;
        public Task<bool> TogglePlayPauseAsync() => Task.FromResult(true);
        public Task<bool> SkipNextAsync() => Task.FromResult(true);
        public Task<bool> SkipPreviousAsync() => Task.FromResult(true);

        public void RaiseTrack(TrackInfo? track) { CurrentTrack = track; TrackChanged?.Invoke(track); }
        public void RaisePlaying(bool playing) { IsPlaying = playing; IsPlayingChanged?.Invoke(playing); }
    }

    private sealed class InlineDispatcher : IEngineDispatcher
    {
        public void Post(Action action) => action();
        public IEngineTimer CreateTimer(TimeSpan interval, Action tick) => new NoopTimer();
        private sealed class NoopTimer : IEngineTimer { public void Start() { } public void Stop() { } }
    }

    /// <summary>lrc가 null이면 결과 없음(미스), 아니면 파싱해 1건 반환(히트).</summary>
    private sealed class FakeProvider(string serviceName, string? lrc) : ILyricsProvider
    {
        public string ServiceName => serviceName;

        public async IAsyncEnumerable<Lyrics> GetLyricsAsync(
            LyricsSearchRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            if (lrc is null) yield break;
            var lyrics = Lyrics.Parse(lrc)!;
            lyrics.Metadata.ServiceName = serviceName;
            lyrics.Metadata.Request = request;
            yield return lyrics;
        }
    }

    private sealed class FakeTranslator : ITranslator
    {
        public Task<IReadOnlyList<string?>> TranslateAsync(
            IReadOnlyList<string> texts, string targetLang, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string?>>(texts.Select(t => (string?)$"{targetLang}:{t}").ToList());
    }

    private static TrackInfo Track(string title = "Song", string artist = "Artist", string appId = "TestPlayer.exe") =>
        new(title, artist, "", null, appId);

    private const string TwoLineLrc = "[00:01.00]hello\n[00:05.00]world";

    // ---- lyrics_search ----

    [Fact]
    public async Task Search_Success_EmitsLyricsSearch_WithWinnerPerSource_NoSongInfo()
    {
        var telemetry = new FakeTelemetry();
        var source = new FakeSource { CurrentTrack = Track() };
        var search = new LyricsSearchService(
            new FakeProvider("LRCLIB", TwoLineLrc),   // 히트
            new FakeProvider("NetEase", null));       // 미스
        using var coordinator = new LyricsCoordinator(source, new InlineDispatcher(), search)
        {
            Telemetry = telemetry,
        };
        coordinator.Start();

        var props = await telemetry.WaitForAsync(TelemetryEvents.LyricsSearch);

        Assert.Equal("lrclib", props["winner"]); // ServiceName "LRCLIB" → 레지스트리 id
        Assert.False((bool)props["cached"]!);
        Assert.False((bool)props["cleanedQueryUsed"]!); // 원본 검색어로 얻은 결과

        var perSource = Assert.IsType<Dictionary<string, object?>>(props["perSource"]);
        var lrclib = Assert.IsType<Dictionary<string, object?>>(perSource["lrclib"]);
        Assert.True((bool)lrclib["hit"]!);
        Assert.True((int)lrclib["latencyMs"]! >= 0);
        var netease = Assert.IsType<Dictionary<string, object?>>(perSource["netease"]);
        Assert.False((bool)netease["hit"]!);

        // 계약 ①: 곡 제목/아티스트 절대 금지
        Assert.False(props.ContainsKey("title"));
        Assert.False(props.ContainsKey("artist"));
    }

    [Fact]
    public async Task Search_AllMiss_EmitsWinnerNone_AndLyricsNotFoundWithSongInfo()
    {
        var telemetry = new FakeTelemetry();
        var source = new FakeSource { CurrentTrack = Track("Unknown Song", "Unknown Artist") };
        var search = new LyricsSearchService(
            new FakeProvider("LRCLIB", null),
            new FakeProvider("Kugou", null));
        using var coordinator = new LyricsCoordinator(source, new InlineDispatcher(), search)
        {
            Telemetry = telemetry,
        };
        coordinator.Start();

        var searchProps = await telemetry.WaitForAsync(TelemetryEvents.LyricsSearch);
        Assert.Equal("none", searchProps["winner"]);
        var perSource = Assert.IsType<Dictionary<string, object?>>(searchProps["perSource"]);
        Assert.Equal(2, perSource.Count);
        Assert.All(perSource.Values, v =>
            Assert.False((bool)Assert.IsType<Dictionary<string, object?>>(v)["hit"]!));

        // ② 검색 전패 → 곡 정보 포함 리포트(동의 필터링은 플랫폼 구현 책임)
        var nfProps = await telemetry.WaitForAsync(TelemetryEvents.LyricsNotFound);
        Assert.Equal("Unknown Song", nfProps["title"]);
        Assert.Equal("Unknown Artist", nfProps["artist"]);
    }

    // ---- wrong_lyrics ----

    [Fact]
    public async Task MarkWrongLyrics_EmitsWrongLyrics_WithAdoptedSourceId()
    {
        var telemetry = new FakeTelemetry();
        var source = new FakeSource { CurrentTrack = Track("Bad Match", "Some Artist") };
        var search = new LyricsSearchService(new FakeProvider("NetEase", TwoLineLrc));
        using var coordinator = new LyricsCoordinator(source, new InlineDispatcher(), search)
        {
            Telemetry = telemetry,
        };
        coordinator.Start();
        await telemetry.WaitForAsync(TelemetryEvents.LyricsSearch); // 채택 완료 동기화

        coordinator.MarkWrongLyrics();

        var props = await telemetry.WaitForAsync(TelemetryEvents.WrongLyrics);
        Assert.Equal("Bad Match", props["title"]);
        Assert.Equal("Some Artist", props["artist"]);
        Assert.Equal("netease", props["source"]);
    }

    // ---- playback_source ----

    [Fact]
    public void PlaybackSource_EmittedPerTrack_NotRepeatedForSameTrack()
    {
        var telemetry = new FakeTelemetry();
        var source = new FakeSource { CurrentTrack = Track(appId: "Spotify.exe") };
        var search = new LyricsSearchService(new FakeProvider("LRCLIB", null));
        using var coordinator = new LyricsCoordinator(source, new InlineDispatcher(), search)
        {
            Telemetry = telemetry,
        };
        coordinator.Start(); // 발화 지점은 OnTrackChanged의 동기 구간

        Assert.Equal(1, telemetry.CountOf(TelemetryEvents.PlaybackSource));

        source.RaiseTrack(Track(appId: "Spotify.exe")); // 같은 트랙 반복 통지 → 미발화
        Assert.Equal(1, telemetry.CountOf(TelemetryEvents.PlaybackSource));

        source.RaiseTrack(Track("Other Song", appId: "Spotify.exe")); // 트랙 변경 → 발화
        Assert.Equal(2, telemetry.CountOf(TelemetryEvents.PlaybackSource));

        Assert.All(telemetry.AllOf(TelemetryEvents.PlaybackSource),
            p => Assert.Equal("Spotify.exe", p["appId"]));
    }

    // ---- translation ----

    [Fact]
    public async Task Translation_EmittedOncePerTrack_WithEngineCacheHitPctAndBucket()
    {
        var telemetry = new FakeTelemetry();
        var source = new FakeSource { CurrentTrack = Track() };
        var search = new LyricsSearchService(new FakeProvider("LRCLIB", TwoLineLrc));

        var mtCache = new InMemoryTranslationCache();
        mtCache.Set("hello", "KO", "안녕"); // 2라인 중 1라인 캐시 적중 → 50%
        var translation = new LyricsTranslationService(new FakeTranslator(), mtCache) { EngineId = "deepl" };

        using var coordinator = new LyricsCoordinator(source, new InlineDispatcher(), search)
        {
            Telemetry = telemetry,
            Translation = translation,
        };
        coordinator.Start();

        var props = await telemetry.WaitForAsync(TelemetryEvents.Translation);
        Assert.Equal("deepl", props["engine"]);
        Assert.Equal(50, props["cacheHitPct"]);
        Assert.Equal("1-10", props["linesBucket"]);

        // 곡당 1회
        await telemetry.WaitForAsync(TelemetryEvents.LyricsSearch); // 파이프라인 종료 동기화
        Assert.Equal(1, telemetry.CountOf(TelemetryEvents.Translation));
    }
}
