using Android.App;
using Android.Service.Notification;

namespace Musebase.Android.Services;

/// <summary>
/// 알림 접근(notification access) 권한의 앵커가 되는 <see cref="NotificationListenerService"/>.
///
/// Android에서 <c>MediaSessionManager.GetActiveSessions()</c>는 알림 접근이 허용된
/// NotificationListenerService의 ComponentName을 요구한다. 즉 이 서비스가 하는 일은
/// "권한의 근거"가 전부다 — 알림 자체를 파싱하지 않으며, 사용자가 설정에서 알림 접근을
/// 켜면 시스템이 이 서비스를 바인드하고, 그때부터 임의 컨텍스트에서
/// GetActiveSessions(component)가 미디어 세션 목록을 반환한다.
///
/// 매니페스트의 service 선언(BIND_NOTIFICATION_LISTENER_SERVICE 권한 + 인텐트 필터)은
/// 아래 특성에서 생성된다. Name을 고정해 ACW(Java 래퍼) 클래스명이 빌드마다
/// 흔들리지 않게 한다(설정 화면에서 사용자가 켠 토글이 유지되도록).
/// </summary>
[Service(
    Label = "Musebase",
    Name = "com.countnine.musebase.MediaListenerService",
    Exported = true,
    Permission = global::Android.Manifest.Permission.BindNotificationListenerService)]
[IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
public sealed class MediaListenerService : NotificationListenerService
{
    /// <summary>시스템이 리스너를 바인드했는지(알림 접근 허용 + 연결 완료).</summary>
    public static bool IsConnected { get; private set; }

    public override void OnListenerConnected()
    {
        base.OnListenerConnected();
        IsConnected = true;
        global::Android.Util.Log.Info("Musebase", "MediaListenerService connected (notification access granted).");
    }

    public override void OnListenerDisconnected()
    {
        base.OnListenerDisconnected();
        IsConnected = false;
        global::Android.Util.Log.Info("Musebase", "MediaListenerService disconnected.");
    }

    // 알림 내용은 사용하지 않는다 — 재생 정보는 MediaSessionManager 경유(AndroidNowPlayingSource).
    public override void OnNotificationPosted(StatusBarNotification? sbn) { }
    public override void OnNotificationRemoved(StatusBarNotification? sbn) { }
}
