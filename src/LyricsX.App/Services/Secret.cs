using System.Security.Cryptography;
using System.Text;

namespace LyricsX.App.Services;

/// <summary>
/// 로컬 비밀값(예: DeepL API 키) 보호. Windows DPAPI(CurrentUser)로 암호화한다.
/// - 같은 Windows 사용자·같은 PC에서만 복호화 가능(파일 열람·백업·타 PC 이동 시 복호 불가).
/// - 한계: 동일 사용자로 실행되는 코드는 복호 가능(로컬 앱 비밀의 본질적 한계).
/// </summary>
internal static class Secret
{
    // 앱 전용 엔트로피(추가 솔트) — 다른 앱의 DPAPI 데이터와 섞이지 않도록.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("LyricsX.DeepL.v1");

    /// <summary>평문 → base64 DPAPI 암호문. 실패/빈 값이면 null.</summary>
    public static string? Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return null;
        try
        {
            var cipher = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipher);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>base64 DPAPI 암호문 → 평문. 복호 실패(타 사용자/PC 등)면 null.</summary>
    public static string? Unprotect(string? cipherBase64)
    {
        if (string.IsNullOrEmpty(cipherBase64)) return null;
        try
        {
            var plain = ProtectedData.Unprotect(
                Convert.FromBase64String(cipherBase64), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }
}
