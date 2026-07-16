using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Musebase.Android.Services;

namespace Musebase.Android;

/// <summary>
/// Phase 2 스파이크 UI — 가사 오버레이 없음. 하는 일:
/// 1) 알림 접근 권한 상태 표시 + 시스템 설정으로 이동하는 버튼
/// 2) <see cref="AndroidNowPlayingSource"/>가 감지한 곡명/아티스트/위치/소스앱을 1초마다 갱신 표시
/// 레이아웃 리소스 없이 코드로 UI를 만들어 스파이크 표면적을 최소화한다.
/// </summary>
[Activity(
    Label = "Musebase",
    Name = "com.countnine.musebase.MainActivity",
    MainLauncher = true,
    Exported = true)]
public sealed class MainActivity : Activity
{
    private const int UiRefreshMs = 1000;

    private AndroidNowPlayingSource? _source;
    private readonly Handler _handler = new(Looper.MainLooper!);
    private TextView? _permissionText;
    private TextView? _statusText;
    private bool _uiLoopRunning;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // ---- UI (코드 생성) ----
        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
        };
        root.SetPadding(48, 96, 48, 48);

        var title = new TextView(this) { Text = "Musebase — MediaSession 스파이크" };
        title.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 20f);
        root.AddView(title);

        _permissionText = new TextView(this);
        _permissionText.SetPadding(0, 32, 0, 0);
        root.AddView(_permissionText);

        var permissionButton = new Button(this) { Text = "알림 접근 권한 설정 열기" };
        permissionButton.Click += (_, _) =>
            StartActivity(new Intent(global::Android.Provider.Settings.ActionNotificationListenerSettings));
        root.AddView(permissionButton);

        _statusText = new TextView(this) { Text = "감지 대기 중…" };
        _statusText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 16f);
        _statusText.SetPadding(0, 48, 0, 0);
        root.AddView(_statusText);

        SetContentView(root, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        // ---- 감지 소스 ----
        _source = new AndroidNowPlayingSource(this);
        _source.TrackChanged += t =>
            global::Android.Util.Log.Info("Musebase", $"track changed: {t?.ToString() ?? "(none)"}");
        _source.Start(); // 권한이 없어도 폴링하며 대기 — 권한이 켜지면 즉시 감지 시작
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (!_uiLoopRunning) { _uiLoopRunning = true; UiTick(); }
    }

    protected override void OnPause()
    {
        base.OnPause();
        _uiLoopRunning = false;
        _handler.RemoveCallbacksAndMessages(null);
    }

    /// <summary>1초마다 권한/감지 상태 텍스트 갱신.</summary>
    private void UiTick()
    {
        if (!_uiLoopRunning) return;
        RenderStatus();
        _handler.PostDelayed(UiTick, UiRefreshMs);
    }

    private void RenderStatus()
    {
        var source = _source;
        if (source is null || _permissionText is null || _statusText is null) return;

        var granted = source.HasNotificationAccess;
        _permissionText.Text = granted
            ? "알림 접근: 허용됨 ✓"
            : "알림 접근: 미허용 — 아래 버튼으로 설정에서 Musebase를 켜 주세요.";

        if (!granted)
        {
            _statusText.Text = "감지 불가 (알림 접근 권한 필요)";
            return;
        }

        var track = source.CurrentTrack;
        if (track is null)
        {
            _statusText.Text = "감지된 미디어 세션 없음\n(음악 앱에서 재생을 시작해 보세요)";
            return;
        }

        var position = source.GetEstimatedPosition();
        _statusText.Text =
            $"곡명: {track.Title}\n" +
            $"아티스트: {track.Artist}\n" +
            $"앨범: {track.Album}\n" +
            $"위치: {Format(position)} / {Format(track.Duration)}\n" +
            $"상태: {(source.IsPlaying ? "재생 중" : "일시정지")}\n" +
            $"소스 앱: {track.SourceAppId}";
    }

    private static string Format(TimeSpan? t) =>
        t is { } v ? $"{(int)v.TotalMinutes}:{v.Seconds:00}" : "-:--";

    protected override void OnDestroy()
    {
        _handler.RemoveCallbacksAndMessages(null);
        _source?.Stop();
        _source?.Dispose();
        _source = null;
        base.OnDestroy();
    }
}
