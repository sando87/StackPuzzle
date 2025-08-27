using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

public class LogToGoogleForms : MonoBehaviour 
{
    public static LogToGoogleForms Instance;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 1. 구글Forms를 새로 생성 후 필드 작성한다.
    // 2. 필드 작성 후 게시 버튼을 누르고 "응답자 링크" 주소로 이동.
    // 3. 웹페이지에서 응답자 URL로 진입 후 html 다운받기
    // 4. html소스에서 각 필드별로 entry ID정보를 찾아서 기입
    // 5. 아래 URL은 실제 폼 데이터를 임의로 작성 후 제출 후 나오는 페이지의 주소를 기입
    private const string GoogleFormURL = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSfV19QwTj4wwMMYcJ93BKAeGz8spe6557YrdjHSIcJ1_rXohA/formResponse";
    // 아래 주소에서 구글시트로 로그데이터들을 확인 가능
    // https://docs.google.com/spreadsheets/d/1OaxrMeuce_hF56Xcru70Lnx4WkbQM1IK93ggWjqgebY/edit?resourcekey=&gid=1768019821#gid=1768019821
    

    private const string Field_UTCTime = "entry.698905010";
    private const string Field_DeviceID = "entry.1163829077";
    private const string Field_SessionID = "entry.1232917968";
    private const string Field_Country = "entry.637400885";
    private const string Field_Platform = "entry.2033189331";
    private const string Field_Version = "entry.1119977436";
    private const string Field_UserPk = "entry.688885666";
    private const string Field_EventName = "entry.1351188991";
    private const string Field_StageNumber = "entry.1248161747";
    private const string Field_Result = "entry.239448342";
    private const string Field_UsedItem = "entry.1854947726";
    private const string Field_PurchaseInfo = "entry.818488764";
    private const string Field_PvpType = "entry.1453024953";
    private const string Field_PvpOppUserPk = "entry.49676338";
    private const string Field_Comments = "entry.1097264941";

    void SendLogToGoogleForm(GoogleFormsData data)
    {
        StartCoroutine(CoSendLog(data));
    }

    IEnumerator CoSendLog(GoogleFormsData data)
    {
        WWWForm form = new WWWForm();

        form.AddField(Field_UTCTime, data.utcTime);
        form.AddField(Field_DeviceID, data.deviceID);
        form.AddField(Field_SessionID, data.sessionID);
        form.AddField(Field_Country, data.userCountry);
        form.AddField(Field_Platform, data.platform);
        form.AddField(Field_Version, data.version);
        form.AddField(Field_UserPk, data.userPk);
        form.AddField(Field_EventName, data.eventName);
        form.AddField(Field_StageNumber, data.stageNumber);
        form.AddField(Field_Result, data.result);
        form.AddField(Field_UsedItem, data.usedItem);
        form.AddField(Field_PurchaseInfo, data.purchaseInfo);
        form.AddField(Field_PvpType, data.pvpType);
        form.AddField(Field_PvpOppUserPk, data.pvpOppUserPk);
        form.AddField(Field_Comments, data.comments);

        using (UnityWebRequest www = UnityWebRequest.Post(GoogleFormURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                LOG.error(www.error);
        }
    }


    public void LogTermsAgree() 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.TermsAgree.ToString();
        SendLogToGoogleForm(data);
    }
    public void LogGameStart(string sessionID) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.GameStart.ToString();
        data.sessionID = sessionID;
        SendLogToGoogleForm(data);
    }
    public void LogStageStart(string sessionID, int stageNumber) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.StageStart.ToString();
        data.sessionID = sessionID;
        data.stageNumber = stageNumber.ToString();
        SendLogToGoogleForm(data);
    }
    public void LogStageEnd(int stageNumber, string result) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.StageEnd.ToString();
        data.stageNumber = stageNumber.ToString();
        data.result = result;
        SendLogToGoogleForm(data);
    }
    public void LogItemUse(string usedItem, int stageNumber) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.ItemUse.ToString();
        data.usedItem = usedItem;
        data.stageNumber = stageNumber.ToString();
        SendLogToGoogleForm(data);
    }
    public void LogPurchaseRequest(string purchaseInfo) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.PurchaseRequest.ToString();
        data.purchaseInfo = purchaseInfo;
        SendLogToGoogleForm(data);
    }
    public void LogPurchaseConfirm(string purchaseInfo, string result) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.PurchaseConfirm.ToString();
        data.purchaseInfo = purchaseInfo;
        data.result = result;
        SendLogToGoogleForm(data);
    }
    public void LogPVPMatchingStart(string pvpType) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.PVPMatchingStart.ToString();
        data.pvpType = pvpType;
        SendLogToGoogleForm(data);
    }
    public void LogPVPMatchingCancel(string pvpType) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.PVPMatchingCancel.ToString();
        data.pvpType = pvpType;
        SendLogToGoogleForm(data);
    }
    public void LogPVPStart(string pvpType, string pvpOppUserPk) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.PVPStart.ToString();
        data.pvpType = pvpType;
        data.pvpOppUserPk = pvpOppUserPk;
        SendLogToGoogleForm(data);
    }
    public void LogPVPEnd(string pvpType, string pvpOppUserPk, string result) 
    {
        GoogleFormsData data = new GoogleFormsData();
        InitFormsCommonData(data);
        data.eventName = GoogleFormsEventName.PVPEnd.ToString();
        data.pvpType = pvpType;
        data.pvpOppUserPk = pvpOppUserPk;
        data.result = result;
        SendLogToGoogleForm(data);
    }

    void InitFormsCommonData(GoogleFormsData data)
    {
        data.utcTime = DateTime.UtcNow.ToString("O");
        data.deviceID = SystemInfo.deviceUniqueIdentifier;
        // data.sessionID = GlobalGameSystemData.newInstance.GameSessionID.ToString();
        data.userCountry = Application.systemLanguage.ToString();
        data.platform = Application.platform.ToString();
        data.version = Application.version;
        // data.userPk = userPK.ToString();
    }
}

public class GoogleFormsData
{
    public string utcTime = "";
    public string deviceID = "";
    public string sessionID = "";
    public string userCountry = "";
    public string platform = "";
    public string version = "";
    public string userPk = "";

    public string eventName = ""; 
    public string stageNumber = "";
    public string result = "";
    public string usedItem = "";
    public string purchaseInfo = "";
    public string pvpType = "";
    public string pvpOppUserPk = "";
    public string comments = "";
}


public enum GoogleFormsEventName
{
    TermsAgree,
    GameStart,
    StageStart,
    StageEnd,
    ItemUse,
    PurchaseRequest,
    PurchaseConfirm,
    PVPMatchingStart,
    PVPMatchingCancel,
    PVPStart,
    PVPEnd,
}
