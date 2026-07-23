using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Musebase.Engine;
using Musebase.Core;
using System;

namespace Musebase.Android;

/// <summary>
/// Phase 2 UI — 앱 내 동기 가사 표시(오버레이는 다음 단계).
/// 1) 알림 접근 권한 상태 표시 + 시스템 설정으로 이동하는 버튼
/// 2) 가사 영역: 전체 가사 스크롤 뷰 + 검색 상태 문구
/// 3) 감지된 곡명/아티스트/위치/소스앱을 1초마다 갱신 표시
/// </summary>
[Activity(
    Label = "Musebase",
    Name = "com.countnine.musebase.MainActivity",
    MainLauncher = true,
    Exported = true)]
public sealed class MainActivity : Activity
{
    private const int UiRefreshMs = 1000;

    private readonly Handler _handler = new(Looper.MainLooper!);
    private TextView? _permissionText;
    private TextView? _overlayPermissionText;
    private Button? _overlayToggleButton;
    private TextView? _lyricsStatusText;
    private ScrollView? _lyricsScrollView;
    private LinearLayout? _lyricsContainer;
    private TextView? _statusText;
    private bool _uiLoopRunning;

    // 구독 해제를 위해 델리게이트 보관
    private Action<PlaybackViewState>? _onStateChanged;
    private Action<LyricsStatus>? _onStatusChanged;
    private Action<TranslationDisplayStatus>? _onTranslationStatusChanged;
    private LyricsStatus _lastLyricsStatus = new(LyricsStatusKind.NoTrack);
    private string? _lastTrackKey;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // ---- UI (코드 생성) ----
        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
        };
        root.SetPadding(48, 96, 48, 48);

        var title = new TextView(this) { Text = "Musebase" };
        title.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 20f);
        root.AddView(title);

        _permissionText = new TextView(this);
        _permissionText.SetPadding(0, 32, 0, 0);
        root.AddView(_permissionText);

        var permissionButton = new Button(this) { Text = "알림 접근 권한 설정 열기" };
        permissionButton.Click += (_, _) =>
            StartActivity(new Intent(global::Android.Provider.Settings.ActionNotificationListenerSettings));
        root.AddView(permissionButton);

        _overlayPermissionText = new TextView(this);
        _overlayPermissionText.SetPadding(0, 32, 0, 0);
        root.AddView(_overlayPermissionText);

        var overlayButtonLayout = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        var overlayPermissionButton = new Button(this) { Text = "오버레이 권한 허용" };
        overlayPermissionButton.Click += (_, _) => RequestOverlayPermission();
        overlayButtonLayout.AddView(overlayPermissionButton);

        _overlayToggleButton = new Button(this) { Text = "가사 오버레이 켜기" };
        _overlayToggleButton.Click += (_, _) => ToggleOverlay();
        overlayButtonLayout.AddView(_overlayToggleButton);
        root.AddView(overlayButtonLayout);

        var settingsButton = new Button(this) { Text = "번역 설정" };
        settingsButton.Click += (_, _) => StartActivity(new Intent(this, typeof(SettingsActivity)));
        root.AddView(settingsButton);

        _statusText = new TextView(this) { Text = "감지 대기 중…" };
        _statusText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 14f);
        _statusText.SetPadding(0, 32, 0, 0);
        root.AddView(_statusText);

        _lyricsStatusText = new TextView(this) { Text = "가사 대기 중" };
        _lyricsStatusText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 13f);
        _lyricsStatusText.SetPadding(0, 32, 0, 0);
        root.AddView(_lyricsStatusText);

        _lyricsScrollView = new ScrollView(this) { FillViewport = true };
        _lyricsScrollView.SetPadding(0, 16, 0, 0);
        _lyricsContainer = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _lyricsScrollView.AddView(_lyricsContainer, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        
        // Let lyrics take remaining space
        var lyricsParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f);
        root.AddView(_lyricsScrollView, lyricsParams);

        SetContentView(root, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        if (MusebaseApp.Instance is { } app)
        {
            _onStateChanged = RenderState;
            _onStatusChanged = s => { _lastLyricsStatus = s; RenderLyricsStatus(); PopulateLyrics(); };
            _onTranslationStatusChanged = _ => { RenderLyricsStatus(); PopulateLyrics(); }; 
            app.Coordinator.StateChanged += _onStateChanged;
            app.Coordinator.StatusChanged += _onStatusChanged;
            app.Coordinator.TranslationStatusChanged += _onTranslationStatusChanged;
            RenderState(app.Coordinator.CurrentState); 
            _lastLyricsStatus = app.LastStatus;
            RenderLyricsStatus();
            PopulateLyrics();
        }
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

    private void UiTick()
    {
        if (!_uiLoopRunning) return;
        RenderStatus();
        _handler.PostDelayed(UiTick, UiRefreshMs);
    }

    private void PopulateLyrics()
    {
        if (_lyricsContainer is null) return;
        _lyricsContainer.RemoveAllViews();

        var lyrics = MusebaseApp.Instance?.Coordinator.CurrentLyrics;
        if (lyrics is null || lyrics.Lines.Count == 0)
        {
            var empty = new TextView(this) { Text = "♪" };
            empty.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 20f);
            empty.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
            empty.Gravity = GravityFlags.CenterHorizontal;
            _lyricsContainer.AddView(empty);
            return;
        }

        foreach (var line in lyrics.Lines)
        {
            var lineLayout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            lineLayout.SetPadding(0, 8, 0, 8);

            var originalText = new TextView(this) { Text = line.Content };
            originalText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 18f);
            originalText.SetTextColor(Color.ParseColor("#DDDDDD"));
            lineLayout.AddView(originalText);

            var translation = MusebaseApp.Instance?.Coordinator.GetType().GetMethod("ResolveDisplayTranslation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(MusebaseApp.Instance?.Coordinator, new object[] { line.Attachments }) as string;
            
            if (!string.IsNullOrEmpty(translation))
            {
                var transText = new TextView(this) { Text = translation };
                transText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 14f);
                transText.SetTextColor(Color.ParseColor("#AAAAAA"));
                lineLayout.AddView(transText);
            }
            
            // Store time in tag for scrolling later
            lineLayout.Tag = (Java.Lang.Object)(line.Position.TotalSeconds);
            _lyricsContainer.AddView(lineLayout);
        }
    }

    private void RenderState(PlaybackViewState state)
    {
        var trackKey = $"{state.TrackTitle}-{state.TrackArtist}";
        if (_lastTrackKey != trackKey)
        {
            _lastTrackKey = trackKey;
            PopulateLyrics();
        }

        if (_lyricsContainer is null || _lyricsScrollView is null) return;
        
        // Highlight current line and scroll
        int targetScrollY = -1;
        for (int i = 0; i < _lyricsContainer.ChildCount; i++)
        {
            var child = _lyricsContainer.GetChildAt(i) as LinearLayout;
            if (child is null) continue;
            
            var originalText = child.GetChildAt(0) as TextView;
            if (originalText is null) continue;

            if (originalText.Text == state.LineContent)
            {
                originalText.SetTextColor(Color.White);
                originalText.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
                targetScrollY = child.Top;
            }
            else
            {
                originalText.SetTextColor(Color.ParseColor("#DDDDDD"));
                originalText.SetTypeface(Typeface.Default, TypefaceStyle.Normal);
            }
        }
        
        if (targetScrollY >= 0)
        {
            _lyricsScrollView.Post(() => {
                _lyricsScrollView.SmoothScrollTo(0, targetScrollY - _lyricsScrollView.Height / 2 + 100);
            });
        }
    }

    private void RenderLyricsStatus()
    {
        if (_lyricsStatusText is null) return;
        var s = _lastLyricsStatus;
        var baseText = s.Kind switch
        {
            LyricsStatusKind.NoTrack => "재생 중인 곡 없음",
            LyricsStatusKind.HiddenByUser => "이 곡은 틀린 가사로 표시되어 숨김",
            LyricsStatusKind.Cache => $"가사: 캐시 · {s.Service}",
            LyricsStatusKind.Searching => "가사 검색 중…",
            LyricsStatusKind.Found => $"가사: {s.Service} (품질 {s.Quality ?? 0:0.00})",
            LyricsStatusKind.NotFound => "가사를 찾지 못했습니다",
            LyricsStatusKind.Wrong => "틀린 가사로 표시됨",
            LyricsStatusKind.Manual => $"가사: 수동 선택 · {s.Service}",
            LyricsStatusKind.Edited => "가사: 사용자 편집",
            _ => "",
        };
        var suffix = (MusebaseApp.Instance?.Coordinator.CurrentTranslationStatus ?? TranslationDisplayStatus.None) switch
        {
            TranslationDisplayStatus.Translating => " · 번역: 번역 중",
            TranslationDisplayStatus.Live => " · 번역: 정상 번역",
            TranslationDisplayStatus.Cache => " · 번역: 캐시 이용",
            TranslationDisplayStatus.Quota => " · 번역: 한도 초과",
            TranslationDisplayStatus.Failed => " · 번역: 실패",
            _ => "",
        };
        _lyricsStatusText.Text = baseText + suffix;
    }

    private void RequestOverlayPermission()
    {
        if (global::Android.Provider.Settings.CanDrawOverlays(this)) return;
        StartActivity(new Intent(
            global::Android.Provider.Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse("package:" + PackageName)));
    }

    private void ToggleOverlay()
    {
        if (Services.OverlayService.IsRunning)
        {
            StopService(new Intent(this, typeof(Services.OverlayService)));
        }
        else
        {
            if (!global::Android.Provider.Settings.CanDrawOverlays(this))
            {
                Toast.MakeText(this, "먼저 '오버레이 권한 허용'을 눌러 주세요.", ToastLength.Long)?.Show();
                RequestOverlayPermission();
                return;
            }
            var intent = new Intent(this, typeof(Services.OverlayService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) StartForegroundService(intent);
            else StartService(intent);
        }
        UpdateOverlayControls();
    }

    private void UpdateOverlayControls()
    {
        if (_overlayPermissionText is not null)
        {
            _overlayPermissionText.Text = global::Android.Provider.Settings.CanDrawOverlays(this)
                ? "다른 앱 위 표시: 허용됨 ✓"
                : "다른 앱 위 표시: 미허용 — '오버레이 권한 허용'을 눌러 설정에서 켜 주세요.";
        }
        if (_overlayToggleButton is not null)
            _overlayToggleButton.Text = Services.OverlayService.IsRunning
                ? "가사 오버레이 끄기" : "가사 오버레이 켜기";
    }

    private void RenderStatus()
    {
        UpdateOverlayControls();

        var source = MusebaseApp.Instance?.Source;
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
            $"위치: {Format(position)} / {Format(track.Duration)}\n" +
            $"소스 앱: {track.SourceAppId}";
    }

    private static string Format(TimeSpan? t) =>
        t is { } v ? $"{(int)v.TotalMinutes}:{v.Seconds:00}" : "-:--";

    protected override void OnDestroy()
    {
        _handler.RemoveCallbacksAndMessages(null);
        if (MusebaseApp.Instance is { } app)
        {
            if (_onStateChanged is not null) app.Coordinator.StateChanged -= _onStateChanged;
            if (_onStatusChanged is not null) app.Coordinator.StatusChanged -= _onStatusChanged;
            if (_onTranslationStatusChanged is not null) app.Coordinator.TranslationStatusChanged -= _onTranslationStatusChanged;
        }
        _onStateChanged = null;
        _onStatusChanged = null;
        _onTranslationStatusChanged = null;
        base.OnDestroy();
    }
}

