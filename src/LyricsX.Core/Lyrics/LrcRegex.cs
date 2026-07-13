using System.Text.RegularExpressions;

namespace LyricsX.Core;

/// <summary>
/// LRC 파싱용 정규식. LyricsKit의 RegexPattern.swift 포팅.
/// </summary>
internal static partial class LrcRegex
{
    /// <summary>[mm:ss.xx] 타임태그 (부호 허용)</summary>
    [GeneratedRegex(@"\[([-+]?\d+):(\d+(?:\.\d+)?)\]")]
    public static partial Regex TimeTag();

    /// <summary>[key:value] ID 태그 라인 (타임태그 라인 제외)</summary>
    [GeneratedRegex(@"^(?!\[[+-]?\d+:\d+(?:\.\d+)?\])\[(.+?):(.+)\]$", RegexOptions.Multiline)]
    public static partial Regex Id3Tag();

    /// <summary>가사 라인: 타임태그 1개 이상 + 본문 + 선택적 【번역】</summary>
    [GeneratedRegex(@"^((?:\[[+-]?\d+:\d+(?:\.\d+)?\])+)(?!\[)([^【\n\r]*)(?:【(.*)】)?", RegexOptions.Multiline)]
    public static partial Regex LyricsLine();

    /// <summary>첨부 라인: 타임태그 + [태그]내용 (예: [00:01.00][tr:ko]번역)</summary>
    [GeneratedRegex(@"^((?:\[[+-]?\d+:\d+(?:\.\d+)?\])+)\[(.+?)\](.*)", RegexOptions.Multiline)]
    public static partial Regex LineAttachment();

    /// <summary>(mm:)ss.xx 형식 길이 표기</summary>
    [GeneratedRegex(@"^\s*(?:(\d+):)?(\d+(?:\.\d+)?)\s*$")]
    public static partial Regex Base60Time();

    /// <summary>인라인 타임태그 &lt;msec,index&gt;</summary>
    [GeneratedRegex(@"<(\d+,\d+)>")]
    public static partial Regex InlineTimeTag();

    /// <summary>인라인 지속시간 &lt;msec&gt;</summary>
    [GeneratedRegex(@"<(\d+)>")]
    public static partial Regex InlineDuration();

    /// <summary>타임태그 문자열에서 초 단위 위치 목록을 해석한다.</summary>
    public static List<double> ResolveTimeTags(string str)
    {
        var result = new List<double>();
        foreach (Match m in TimeTag().Matches(str))
        {
            var min = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var sec = double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            result.Add(min * 60 + sec);
        }
        return result;
    }
}
