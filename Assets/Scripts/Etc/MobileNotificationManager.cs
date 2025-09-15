using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class MobileNotificationManager : MonoBehaviour
{
    private const string CHANNEL_ID = "oneday_channel";

    void Start()
    {
        Init();
    }

    void Init()
    {
#if UNITY_ANDROID
        // 기존 알림 제거
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        // 알림 채널 등록
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,              // 고유 ID
            Name = "One-Day Event",            // 설정 화면에 표시될 이름
            Importance = Importance.Default,     // 중요도 (소리/진동 여부)
            Description = "Basic Alarm Channel",      // 설명
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        // 2. 알림 생성
        var notification = new AndroidNotification();
        notification.Title = "게임에 복귀하세요!";
        notification.Text = "하루가 지났습니다. 다시 접속해 보세요!";
        notification.SmallIcon = "icon_0";      // Assets/Plugins/Android/Res/drawalbe/icon_0.png 24x24~96x96 작은 아이콘 (필수), 배경 투명, 아이콘 단색
        notification.LargeIcon = "icon_1";      // Assets/Plugins/Android/Res/drawalbe/icon_1.png 128x128 큰 아이콘 (선택)
        notification.FireTime = System.DateTime.Now.AddDays(1); // 하루 뒤 실행

        // 3. 알림 발송
        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
#endif
    }
}
