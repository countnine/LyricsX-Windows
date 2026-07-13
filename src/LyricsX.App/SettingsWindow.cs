using System.Windows;
using System.Windows.Controls;
using LyricsX.App.Services;

namespace LyricsX.App;

/// <summary>
/// 최소 설정 창: DeepL API 키, 대상 언어, 폰트 크기.
/// 저장 시 onSaved 콜백으로 앱에 즉시 반영한다.
/// </summary>
public sealed class SettingsWindow : Window
{
    private static readonly string[] CommonLanguages =
        ["KO", "EN-US", "EN-GB", "JA", "ZH", "ES", "FR", "DE", "PT-BR"];

    public SettingsWindow(AppSettings settings, Action onSaved)
    {
        Title = "LyricsX 설정";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        var apiKeyBox = new TextBox { Text = settings.DeeplApiKey ?? "", Margin = new Thickness(0, 2, 0, 10) };
        var langBox = new ComboBox
        {
            IsEditable = true,
            Text = settings.TargetLanguage,
            ItemsSource = CommonLanguages,
            Margin = new Thickness(0, 2, 0, 10),
        };

        var fontHint = new TextBlock
        {
            Text = "텍스트 크기는 오버레이 크기에 맞춰 자동 조절됩니다.\n(오버레이에 마우스를 올려 🔒 클릭 → 이동/크기 조절 모드)",
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0),
        };

        var saveButton = new Button
        {
            Content = "저장",
            Width = 90,
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0),
        };
        saveButton.Click += (_, _) =>
        {
            settings.DeeplApiKey = string.IsNullOrWhiteSpace(apiKeyBox.Text) ? null : apiKeyBox.Text.Trim();
            settings.TargetLanguage = string.IsNullOrWhiteSpace(langBox.Text) ? "KO" : langBox.Text.Trim().ToUpperInvariant();
            settings.Save();
            onSaved();
            Close();
        };

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = "DeepL API 키 (비우면 제공자 번역만 사용)",
            FontWeight = FontWeights.SemiBold,
        });
        panel.Children.Add(apiKeyBox);
        panel.Children.Add(new TextBlock
        {
            Text = "번역 대상 언어 (DeepL target_lang, 기본 KO)",
            FontWeight = FontWeights.SemiBold,
        });
        panel.Children.Add(langBox);
        panel.Children.Add(fontHint);
        panel.Children.Add(saveButton);
        Content = panel;
    }
}
