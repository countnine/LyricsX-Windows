using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LyricsX.Core;

namespace LyricsX.App;

/// <summary>
/// 현재 가사 편집 창. 확장 LRC 텍스트(무손실)를 편집해 저장하면
/// 파싱 검증 후 onSaved 콜백으로 넘긴다. 파싱 실패 시 저장을 막고 안내한다.
/// </summary>
public sealed class LyricsEditorWindow : Window
{
    public LyricsEditorWindow(string trackLabel, string lrcText, Action<Lyrics> onSaved)
    {
        Title = $"가사 편집 — {trackLabel}";
        Width = 560;
        Height = 620;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var hint = new TextBlock
        {
            Text = "형식: [mm:ss.xx]원문  ·  번역은 [mm:ss.xx][tr]번역 줄.\n" +
                   "글자 단위 노래방 태그([tt] 줄)는 그대로 두면 보존됩니다.",
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        };

        var editor = new TextBox
        {
            Text = lrcText,
            AcceptsReturn = true,
            AcceptsTab = false,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
        };

        var status = new TextBlock
        {
            Foreground = Brushes.OrangeRed,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var save = new Button { Content = "저장", Width = 90, IsDefault = true, Margin = new Thickness(0, 8, 8, 0) };
        var cancel = new Button { Content = "취소", Width = 90, IsCancel = true, Margin = new Thickness(0, 8, 0, 0) };

        save.Click += (_, _) =>
        {
            var parsed = Lyrics.Parse(editor.Text);
            if (parsed is null || parsed.Lines.Count == 0)
            {
                status.Text = "가사 형식이 올바르지 않습니다. 타임태그([mm:ss.xx]) 줄이 하나 이상 필요합니다.";
                return;
            }
            onSaved(parsed);
            Close();
        };
        cancel.Click += (_, _) => Close();

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);

        var root = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(hint, Dock.Top);
        DockPanel.SetDock(buttons, Dock.Bottom);
        DockPanel.SetDock(status, Dock.Bottom);
        root.Children.Add(hint);
        root.Children.Add(buttons);
        root.Children.Add(status);
        root.Children.Add(editor); // 남은 공간 채움
        Content = root;
    }
}
