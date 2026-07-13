using System.Windows;
using System.Windows.Controls;
using LyricsX.App.Services;
using LyricsX.Core;
using LyricsX.Core.Search;

namespace LyricsX.App;

/// <summary>
/// 수동 가사 검색 창. 자동 매칭이 틀렸을 때 직접 검색해 교체한다.
/// 선택 → 적용 시 coordinator.UseLyricsAsync로 오버레이·캐시에 반영.
/// </summary>
public sealed class SearchWindow : Window
{
    private readonly LyricsCoordinator _coordinator;
    private readonly TextBox _titleBox;
    private readonly TextBox _artistBox;
    private readonly ListView _resultList;
    private readonly Button _searchButton;
    private readonly Button _applyButton;
    private readonly TextBlock _statusText;
    private CancellationTokenSource? _cts;

    private sealed record ResultRow(Lyrics Lyrics, string Service, string Title, string Artist,
        int LineCount, string Translated, string Quality);

    public SearchWindow(LyricsCoordinator coordinator)
    {
        _coordinator = coordinator;

        Title = "가사 검색";
        Width = 640;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _titleBox = new TextBox { Text = coordinator.CurrentTrack?.Title ?? "", MinWidth = 200, Margin = new Thickness(4, 0, 12, 0) };
        _artistBox = new TextBox { Text = coordinator.CurrentTrack?.Artist ?? "", MinWidth = 150, Margin = new Thickness(4, 0, 12, 0) };
        _searchButton = new Button { Content = "검색", Width = 80, IsDefault = true };
        _searchButton.Click += async (_, _) => await RunSearchAsync();

        var inputPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12) };
        inputPanel.Children.Add(new TextBlock { Text = "제목:", VerticalAlignment = VerticalAlignment.Center });
        inputPanel.Children.Add(_titleBox);
        inputPanel.Children.Add(new TextBlock { Text = "아티스트:", VerticalAlignment = VerticalAlignment.Center });
        inputPanel.Children.Add(_artistBox);
        inputPanel.Children.Add(_searchButton);

        var grid = new GridView();
        grid.Columns.Add(new GridViewColumn { Header = "소스", Width = 70, DisplayMemberBinding = new System.Windows.Data.Binding("Service") });
        grid.Columns.Add(new GridViewColumn { Header = "제목", Width = 190, DisplayMemberBinding = new System.Windows.Data.Binding("Title") });
        grid.Columns.Add(new GridViewColumn { Header = "아티스트", Width = 120, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
        grid.Columns.Add(new GridViewColumn { Header = "라인", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("LineCount") });
        grid.Columns.Add(new GridViewColumn { Header = "번역", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Translated") });
        grid.Columns.Add(new GridViewColumn { Header = "품질", Width = 55, DisplayMemberBinding = new System.Windows.Data.Binding("Quality") });

        _resultList = new ListView { View = grid, Margin = new Thickness(12, 0, 12, 0) };
        _resultList.MouseDoubleClick += async (_, _) => await ApplySelectedAsync();
        _resultList.SelectionChanged += (_, _) => _applyButton!.IsEnabled = _resultList.SelectedItem is not null;

        _statusText = new TextBlock { Margin = new Thickness(12, 6, 12, 0), Opacity = 0.7 };

        _applyButton = new Button
        {
            Content = "선택한 가사 적용",
            Width = 140,
            IsEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12),
        };
        _applyButton.Click += async (_, _) => await ApplySelectedAsync();

        var root = new DockPanel();
        DockPanel.SetDock(inputPanel, Dock.Top);
        DockPanel.SetDock(_statusText, Dock.Top);
        DockPanel.SetDock(_applyButton, Dock.Bottom);
        root.Children.Add(inputPanel);
        root.Children.Add(_statusText);
        root.Children.Add(_applyButton);
        root.Children.Add(_resultList);
        Content = root;

        Closed += (_, _) => _cts?.Cancel();
    }

    private async Task RunSearchAsync()
    {
        var title = _titleBox.Text.Trim();
        var artist = _artistBox.Text.Trim();
        if (title.Length == 0) return;

        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        _searchButton.IsEnabled = false;
        _statusText.Text = "검색 중…";
        _resultList.ItemsSource = null;

        try
        {
            var request = LyricsSearchRequest.ByInfo(
                title, artist, _coordinator.CurrentTrack?.Duration?.TotalSeconds ?? 0, limit: 6);
            var service = new LyricsSearchService();
            var results = await service.SearchAllAsync(request, cts.Token);

            _resultList.ItemsSource = results.Select(l => new ResultRow(
                l,
                l.Metadata.ServiceName ?? "?",
                l.IdTags.GetValueOrDefault(Lyrics.TagTitle) ?? "?",
                l.IdTags.GetValueOrDefault(Lyrics.TagArtist) ?? "?",
                l.Lines.Count,
                l.HasTranslation() ? "O" : "-",
                l.Quality().ToString("0.00"))).ToList();
            _statusText.Text = results.Count == 0 ? "결과 없음" : $"{results.Count}건 (품질 순)";
        }
        catch (OperationCanceledException)
        {
            // 창 닫힘/재검색
        }
        catch (Exception e)
        {
            _statusText.Text = $"검색 실패: {e.Message}";
        }
        finally
        {
            _searchButton.IsEnabled = true;
        }
    }

    private async Task ApplySelectedAsync()
    {
        if (_resultList.SelectedItem is not ResultRow row) return;
        await _coordinator.UseLyricsAsync(row.Lyrics);
        Close();
    }
}
