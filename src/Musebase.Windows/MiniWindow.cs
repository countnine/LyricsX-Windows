using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Musebase.Windows.Services;

namespace Musebase.Windows;

/// <summary>
/// 작업표시줄에 상주하는 작은 창. 오버레이가 숨겨져도(사용자 숨김·일시정지·가림방지)
/// 여기서 항상 되살릴 수 있는 손잡이 역할을 한다.
/// - 닫기(X)는 종료가 아니라 최소화(작업표시줄 상주). 실제 종료는 "종료" 버튼/트레이.
/// - 작업표시줄에서 창을 복원(클릭)하면 오버레이를 다시 보이게 한다(Revive).
/// - "오버레이 표시/숨김"·"설정"·"종료" 버튼 제공. 표시 상태는 트레이와 동기화.
/// </summary>
public sealed class MiniWindow : Window
{
    private readonly TextBlock _status;
    private readonly Button _overlayToggle;
    private readonly Func<bool> _isOverlayVisible;
    private readonly Action<bool> _setOverlayVisible;
    private readonly Action _reviveOverlay;
    private bool _closingToExit; // "종료" 경로에서만 실제 닫힘 허용

    public MiniWindow(
        System.Drawing.Icon? appIcon,
        Func<bool> isOverlayVisible,
        Action<bool> setOverlayVisible,
        Action reviveOverlay,
        Action openSettings,
        Action exit)
    {
        _isOverlayVisible = isOverlayVisible;
        _setOverlayVisible = setOverlayVisible;
        _reviveOverlay = reviveOverlay;

        Title = Loc.T("mini.title");
        Width = 320;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.CanMinimize;
        ShowInTaskbar = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        if (appIcon is not null)
        {
            try
            {
                Icon = Imaging.CreateBitmapSourceFromHIcon(
                    appIcon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch { /* 아이콘 변환 실패는 무시(기본 아이콘) */ }
        }

        _status = new TextBlock
        {
            Text = Loc.T("mini.status.idle"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            MinHeight = 34,
        };

        _overlayToggle = new Button
        {
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 8, 0),
        };
        _overlayToggle.Click += (_, _) => _setOverlayVisible(!_isOverlayVisible());

        var settingsButton = new Button { Content = Loc.T("mini.settings"), Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 8, 0) };
        settingsButton.Click += (_, _) => openSettings();

        var exitButton = new Button { Content = Loc.T("mini.exit"), Padding = new Thickness(10, 4, 10, 4) };
        exitButton.Click += (_, _) => { _closingToExit = true; exit(); };

        var buttonRow = new StackPanel { Orientation = Orientation.Horizontal };
        buttonRow.Children.Add(_overlayToggle);
        buttonRow.Children.Add(settingsButton);
        buttonRow.Children.Add(exitButton);

        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(_status);
        root.Children.Add(buttonRow);
        Content = root;

        SyncOverlayVisible(_isOverlayVisible());

        // 닫기(X) → 최소화(작업표시줄 상주). "종료" 버튼일 때만 실제 닫힘.
        Closing += (_, e) =>
        {
            if (_closingToExit) return;
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        };

        // 작업표시줄에서 복원(클릭) → 오버레이 되살리기.
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Normal) _reviveOverlay();
        };
        Activated += (_, _) =>
        {
            if (WindowState != WindowState.Minimized) _reviveOverlay();
        };

        Loc.CultureChanged += ApplyText;
        Closed += (_, _) => Loc.CultureChanged -= ApplyText;
    }

    /// <summary>현재 상태 한 줄을 갱신한다(코디네이터 상태/힌트).</summary>
    public void SetStatus(string text) => _status.Text = text;

    /// <summary>오버레이 표시 상태에 맞춰 토글 버튼 라벨을 갱신한다(트레이와 동기화).</summary>
    public void SyncOverlayVisible(bool visible) =>
        _overlayToggle.Content = Loc.T(visible ? "mini.hideOverlay" : "mini.showOverlay");

    private void ApplyText()
    {
        Title = Loc.T("mini.title");
        SyncOverlayVisible(_isOverlayVisible());
    }

    /// <summary>종료 경로(트레이 종료 등)에서 실제 닫힘을 허용하고 창을 닫는다.</summary>
    public void CloseForExit()
    {
        _closingToExit = true;
        Close();
    }
}
