using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace LyricsX.App.Overlay;

/// <summary>
/// 오버레이 우상단에 뜨는 자물쇠 버튼 (이동 모드 토글).
/// 오버레이 본체는 클릭스루라 마우스 입력을 못 받으므로,
/// 클릭 가능한 별도 소형 창으로 띄운다.
/// </summary>
public sealed class LockButtonWindow : Window
{
    private readonly TextBlock _icon;
    private readonly Border _chrome;

    public LockButtonWindow(Action onToggle)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Width = 32;
        Height = 32;

        _icon = new TextBlock
        {
            Text = "🔒",
            FontSize = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _chrome = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x20, 0x20, 0x20)),
            Child = _icon,
            Margin = new Thickness(2),
        };
        Content = _chrome;
        Cursor = Cursors.Hand;

        MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            onToggle();
        };
        MouseEnter += (_, _) => _chrome.Background = new SolidColorBrush(Color.FromArgb(0xE0, 0x35, 0x35, 0x35));
        MouseLeave += (_, _) => _chrome.Background = new SolidColorBrush(Color.FromArgb(0xB8, 0x20, 0x20, 0x20));

        SourceInitialized += (_, _) =>
        {
            // 포커스 훔치지 않기 + Alt-Tab 목록 제외 (클릭스루는 아님!)
            var hwnd = new WindowInteropHelper(this).Handle;
            var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            style |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, style);
        };
    }

    /// <summary>잠금 상태 표시 갱신 (🔒 = 클릭스루/고정, 🔓 = 이동 모드)</summary>
    public void SetLocked(bool locked)
    {
        _icon.Text = locked ? "🔒" : "🔓";
        ToolTip = locked ? "클릭: 오버레이 이동/크기 조절 모드" : "클릭: 오버레이 고정 (클릭스루)";
    }

    public void ShowAt(double left, double top)
    {
        Left = left;
        Top = top;
        if (!IsVisible) Show();
    }
}
