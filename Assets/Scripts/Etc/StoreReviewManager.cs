using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_ANDROID
using Google.Play.Review;
#elif UNITY_IPHONE
using System.Runtime.InteropServices;
#endif

public class StoreReviewManager : MonoBehaviour
{
    private static StoreReviewManager mInst = null;
    public static StoreReviewManager Inst { get { if (mInst == null) mInst = FindObjectOfType<StoreReviewManager>(); return mInst; } }

    public void RequestReview()
    {
#if UNITY_ANDROID
        RequestReviewGoogleStore();
#elif UNITY_IPHONE
        RequestReviewAppleStore();
#else
#endif
    }

#if UNITY_ANDROID
    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo;

    public void RequestReviewGoogleStore()
    {
        _reviewManager = new ReviewManager();

        // 리뷰 정보 요청
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        requestFlowOperation.Completed += op =>
        {
            if (op.Error != ReviewErrorCode.NoError)
            {
                LOG.trace("리뷰 요청 실패: " + op.Error.ToString());
                return;
            }

            _playReviewInfo = op.GetResult();

            // 리뷰 다이얼로그 실행
            var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
            launchFlowOperation.Completed += launchOp =>
            {
                _playReviewInfo = null; // 반드시 해제
                LOG.trace("리뷰 플로우 완료");
            };
        };
    }
#endif

#if UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern void RequestReview_iOS();

    void RequestReviewAppleStore()
    {
        RequestReview_iOS();
    }
#endif

}
