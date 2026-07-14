using LyricsX.Core;
using Xunit;

namespace LyricsX.Core.Tests;

public class EditExportTests
{
    private static Lyrics MakeBilingualWithKaraoke()
    {
        var tt = new InlineTimeTags([new(0, 0.0), new(5, 0.5)], 1.0).ToString();
        var line0 = new LyricsLine("Hello", 1.0, new LineAttachments
        {
            [LineAttachments.TranslationTag()] = "안녕",
            [LineAttachments.TagTimeTag] = tt,
        });
        var line1 = new LyricsLine("World", 5.0, new LineAttachments
        {
            [LineAttachments.TranslationTag()] = "세계",
        });
        var lyrics = new Lyrics([line0, line1]);
        lyrics.IdTags[Lyrics.TagTitle] = "Song";
        return lyrics;
    }

    [Fact]
    public void ExtendedString_RoundTripsContentTranslationAndKaraoke()
    {
        var original = MakeBilingualWithKaraoke();

        // 편집 창이 쓰는 경로: ToString() → 편집 → Parse()
        var reparsed = Lyrics.Parse(original.ToString());

        Assert.NotNull(reparsed);
        Assert.Equal("Song", reparsed!.IdTags[Lyrics.TagTitle]);
        Assert.Equal(2, reparsed.Lines.Count);
        Assert.Equal("Hello", reparsed.Lines[0].Content);
        Assert.Equal("안녕", reparsed.Lines[0].Attachments.Translation());
        Assert.Equal("세계", reparsed.Lines[1].Attachments.Translation());
        // 글자 단위 노래방(tt)까지 보존
        Assert.NotNull(reparsed.Lines[0].Attachments.GetInlineTimeTags());
    }

    [Fact]
    public void LegacyString_EmitsBilingualInlineTranslation()
    {
        var legacy = MakeBilingualWithKaraoke().ToLegacyString();

        Assert.Contains("Hello【안녕】", legacy);
        Assert.Contains("World【세계】", legacy);
        // 내보내기는 표준 [mm:ss.fff] 타임태그 사용
        Assert.Contains("[00:01.000]", legacy);
    }
}
