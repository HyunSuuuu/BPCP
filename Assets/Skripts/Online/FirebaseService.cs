using BurgerIdle.UI;
using UnityEngine;
#if BPCP_FIREBASE
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Messaging;
#endif

namespace BurgerIdle.Online
{
    /// <summary>
    /// Firebase 실습 연결. 스크립팅 정의 기호 BPCP_FIREBASE가 있을 때만 동작하고, 없으면 아무것도 하지 않는다
    /// (Firebase SDK가 없는 PC 빌드·에디터에서도 게임이 그대로 돌아감).
    ///
    /// 1. 시작: 의존성 확인 → 익명 로그인 (Console에 UID 출력)
    /// 2. DB: 데모 클리어 시 Firestore "clears" 컬렉션에 기록 1개 추가 (UID, 플레이 시간, 레벨, 판매 수, 서버 시각)
    /// 3. 푸시: FCM 토큰 출력, "all" 토픽 구독. 앱이 켜져 있을 때 받은 알림은 게임 알림 팝업으로 표시
    ///    (앱이 꺼져 있거나 백그라운드면 Android 알림창에 표시되고, 누르면 앱이 열린다)
    /// </summary>
    public static class FirebaseService
    {
        public const string ClearCollection = "clears";
        public const string Topic = "all";

        /// <summary>익명 로그인까지 끝났는지</summary>
        public static bool IsReady { get; private set; }

        static NotificationPopup popup;

#if BPCP_FIREBASE
        static string userId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsReady = false;
            popup = null;
            userId = null;
        }
#endif

        /// <summary>게임 시작 시 GameBootstrap이 호출한다.</summary>
        public static void Init(NotificationPopup notifications)
        {
            popup = notifications;
#if BPCP_FIREBASE
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[Firebase] 사용할 수 없습니다: {task.Result}");
                    return;
                }

                Debug.Log("[Firebase] 준비 완료");
                SetupMessaging();
                SignIn();
            });
#else
            Debug.Log("[Firebase] 꺼짐 (Scripting Define Symbols에 BPCP_FIREBASE 없음)");
#endif
        }

        /// <summary>데모 클리어 기록을 Firestore에 추가한다.</summary>
        public static void RecordClear(float playSeconds, int level, int sales)
        {
#if BPCP_FIREBASE
            if (!IsReady)
            {
                Debug.LogWarning("[Firebase] 로그인 전이라 클리어 기록을 저장하지 않았습니다.");
                return;
            }

            var record = new Dictionary<string, object>
            {
                ["uid"] = userId,
                ["playSeconds"] = Mathf.RoundToInt(playSeconds),
                ["level"] = level,
                ["sales"] = sales,
                ["platform"] = Application.platform.ToString(),
                ["clearedAt"] = FieldValue.ServerTimestamp,
            };

            FirebaseFirestore.DefaultInstance.Collection(ClearCollection).AddAsync(record).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError($"[Firebase] 클리어 기록 저장 실패: {task.Exception}");
                else
                    Debug.Log($"[Firebase] 클리어 기록 저장: {ClearCollection}/{task.Result.Id} (시간 {Mathf.RoundToInt(playSeconds)}초, Lv {level}, 판매 {sales})");
            });
#endif
        }

#if BPCP_FIREBASE
        static void SignIn()
        {
            FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[Firebase] 익명 로그인 실패: {task.Exception}");
                    return;
                }

                userId = task.Result.User.UserId;
                IsReady = true;
                Debug.Log($"[Firebase] 익명 로그인: {userId}");
            });
        }

        static void SetupMessaging()
        {
            FirebaseMessaging.TokenReceived += (_, e) => Debug.Log($"[Firebase] FCM 토큰: {e.Token}");
            FirebaseMessaging.MessageReceived += OnMessageReceived;
            FirebaseMessaging.SubscribeAsync(Topic).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError($"[Firebase] 토픽 구독 실패 ({Topic}): {task.Exception}");
                else
                    Debug.Log($"[Firebase] 토픽 구독: {Topic}");
            });
        }

        /// <summary>앱이 켜져 있을 때 받은 알림을 게임 알림 팝업으로 띄운다 (콜백은 메인 스레드가 아닐 수 있어 넘겨서 처리).</summary>
        static void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var notification = e.Message.Notification;
            if (notification == null)
                return;

            var text = string.IsNullOrEmpty(notification.Title) ? notification.Body : $"{notification.Title}\n{notification.Body}";
            Task.CompletedTask.ContinueWithOnMainThread(_ =>
            {
                Debug.Log($"[Firebase] 푸시 수신: {text}");
                if (popup != null)
                    popup.Show(text);
            });
        }
#endif
    }
}
