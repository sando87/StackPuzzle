using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

#if UNITY_EDITOR

public class LevelEditor : EditorWindow
{
    enum SelectType { NoBlock, CapProduct, IceProduct, BushFrame, RopeFrame, SkillProduct }

    public Sprite[] IceImages = null;
    public Sprite[] CapImages = null;
    public Sprite[] BushImages = null;
    public Sprite[] RopeImages = null;
    public Sprite[] SkillImages = null;

    //블럭 사이즈 정의
    private GUILayoutOption[] GridButtonSize = new GUILayoutOption[2] { GUILayout.Width(15), GUILayout.Height(15) };
    private GUILayoutOption[] GridButtonSizeHalf = new GUILayoutOption[2] { GUILayout.Width(25), GUILayout.Height(25) };

    private Vector2 ScrollPosition = Vector2.zero;
    private SelectType CurrentSelection = SelectType.NoBlock;
    private string TextFieldLevel = "";
    private int GoalTypeIndex = 0;
    string[] GoalTypeList = new string[] { "Score", "Cap", "Ice", "Bush", "Rope" };
    string[] RewardTypeList = new string[] { "None", "gold", "dia", "life", "ExtendLimit", "RemoveIce", "MakeSkill1", "KeepCombo", "MakeSkill2", "Meteor" };
    int RewardTypeA = 0;
    int RewardCountA = 0;
    int RewardTypeB = 0;
    int RewardCountB = 0;
    string RewardChest = "None";

    private StageInfo mStageInfo = null;

    [MenuItem("/Users/LevelEditor")]
    private static void ShowWind기ow()
    {
        var window = GetWindow<LevelEditor>();
        window.titleContent = new GUIContent("Level Editor");
        window.Initialze();
        window.Show();
    }

    private void Initialze()
    {
        LOG.LogWriterConsole += (msg) => {
            Debug.Log(msg);
        };
        LoadResources();
        LoadFromFile(1);

        //최초 실행시 비활성화 블럭이 선택된 상태로 시작
        CurrentSelection = SelectType.NoBlock;
    }

    private void OnGUI()
    {
        ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition, GUIStyle.none);

        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginVertical(GUILayout.Width(300)); //왼쪽에 모니터링 정보 표기 Layout창
            {
                GUILevelLoader();
                GUILayout.Label("========================================", EditorStyles.boldLabel);
                GUILevelController();
                GUILayout.Label("========================================", EditorStyles.boldLabel);
                
                if(mStageInfo != null)
                {
                    GUILevelStageMeta();
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.BeginVertical(); //오른쪽에 블럭 배치 제어 Layout창
            {
                GUIBlockSelector();
                GUILayout.Label("=============================================================================================", EditorStyles.boldLabel);

                if(mStageInfo != null)
                {
                    GUIGameGrid();
                }
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
            
        GUILayout.EndScrollView();
    }

    void GUILevelLoader()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Auto", new GUILayoutOption[] { GUILayout.Width(50) }))
        {
            AutoCreateNewStageAndSave();
        }
        if (GUILayout.Button("New", new GUILayoutOption[] { GUILayout.Width(60) }))
        {
            CreateNewStage();
        }
        if (GUILayout.Button("Save", new GUILayoutOption[] { GUILayout.Width(60) }))
        {
            SaveToFile();
        }
        if (GUILayout.Button("Refresh", new GUILayoutOption[] { GUILayout.Width(60) }))
        {
            RefreshStage();
        }
        if (GUILayout.Button("GoLast", new GUILayoutOption[] { GUILayout.Width(60) }))
        {
            int lastStageNum = StageInfo.GetMaxStageNum();
            TextFieldLevel = lastStageNum.ToString();
            LoadFromFile(lastStageNum);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    void GUILevelController()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<< 이전레벨", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            if(mStageInfo != null && mStageInfo.Num > 1)
            {
                LoadFromFile(mStageInfo.Num - 1);
            }
        }

        GUILayout.FlexibleSpace();

        TextFieldLevel = GUILayout.TextField(TextFieldLevel, new GUILayoutOption[] { GUILayout.Width(60) });
        if (int.TryParse(TextFieldLevel, out int userInputNumber))// && 0 < userInputNumber && userInputNumber <= LevelSpecs.Count)
        {
            if(mStageInfo == null || userInputNumber != mStageInfo.Num)
            {
                LoadFromFile(userInputNumber);
            }
        }
        else
        {
            mStageInfo = null;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("다음레벨 >>", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            if (mStageInfo != null)
            {
                LoadFromFile(mStageInfo.Num + 1);
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    void GUILevelStageMeta()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Version", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.Label(StageInfo.Version.ToString(), EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("LevelNumber", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.Label(mStageInfo.Num.ToString(), EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        int preIndex = GoalTypeIndex;
        GoalTypeIndex = EditorGUILayout.Popup("GoalType", GoalTypeIndex, GoalTypeList);
        if(preIndex != GoalTypeIndex)
        {
            ChangeGoalTypeTo(GoalTypeIndex);
        }
        GUILayout.EndHorizontal();

        if(mStageInfo.GoalTypeEnum == StageGoalType.Score)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("GoalValue", EditorStyles.label);
            GUILayout.FlexibleSpace();
            mStageInfo.GoalValue = EditorGUILayout.IntField(mStageInfo.GoalValue, new GUILayoutOption[1] { GUILayout.Width(100) });
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("GoalValue", EditorStyles.label);
            GUILayout.FlexibleSpace();
            int goalCount = GetGoalCount();
            GUILayout.Label(goalCount.ToString(), EditorStyles.label);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Label("MoveLimit", EditorStyles.label);
        GUILayout.FlexibleSpace();
        mStageInfo.MoveLimit = EditorGUILayout.IntField(mStageInfo.MoveLimit, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("TimeLimit", EditorStyles.label);
        GUILayout.FlexibleSpace();
        mStageInfo.TimeLimit = EditorGUILayout.IntField(mStageInfo.TimeLimit, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("ColorCount", EditorStyles.label);
        GUILayout.FlexibleSpace();
        mStageInfo.ColorCount = EditorGUILayout.FloatField(mStageInfo.ColorCount, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        // GUILayout.Space(10);

        // GUILayout.BeginHorizontal();
        // GUILayout.Label("StarPoint", EditorStyles.label);
        // GUILayout.FlexibleSpace();
        // mStageInfo.StarPoint = EditorGUILayout.IntField(mStageInfo.StarPoint, new GUILayoutOption[1] { GUILayout.Width(100) });
        // GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("RandomSeed", EditorStyles.label);
        GUILayout.FlexibleSpace();
        mStageInfo.RandomSeed = EditorGUILayout.IntField(mStageInfo.RandomSeed, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        RewardTypeA = EditorGUILayout.Popup("RewardTypeA", RewardTypeA, RewardTypeList);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("RewardCountA", EditorStyles.label);
        GUILayout.FlexibleSpace();
        RewardCountA = EditorGUILayout.IntField(RewardCountA, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        RewardTypeB = EditorGUILayout.Popup("RewardTypeB", RewardTypeB, RewardTypeList);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("RewardCountB", EditorStyles.label);
        GUILayout.FlexibleSpace();
        RewardCountB = EditorGUILayout.IntField(RewardCountB, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Reward(1/1 2/1 3/1)", EditorStyles.label);
        GUILayout.FlexibleSpace();
        RewardChest = EditorGUILayout.TextField(RewardChest, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();


        GUILayout.EndVertical();
    }

    void GUIBlockSelector()
    {
        Color enabledColor = Color.white;
        Color disabledColor = Color.gray;

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        {
            GUI.color = CurrentSelection == SelectType.NoBlock ? enabledColor : disabledColor;
            if (GUILayout.Button("X", GridButtonSize))
            {
                CurrentSelection = SelectType.NoBlock;
            }
            GUI.color = CurrentSelection == SelectType.CapProduct ? enabledColor : disabledColor;
            if (GUILayout.Button(CapImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.CapProduct;
            }
            GUI.color = CurrentSelection == SelectType.IceProduct ? enabledColor : disabledColor;
            if (GUILayout.Button(IceImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.IceProduct;
            }
            GUI.color = CurrentSelection == SelectType.BushFrame ? enabledColor : disabledColor;
            if (GUILayout.Button(BushImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.BushFrame;
            }
            GUI.color = CurrentSelection == SelectType.RopeFrame ? enabledColor : disabledColor;
            if (GUILayout.Button(RopeImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.RopeFrame;
            }
            GUI.color = CurrentSelection == SelectType.SkillProduct ? enabledColor : disabledColor;
            if (GUILayout.Button(SkillImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.SkillProduct;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUI.color = new Color(1, 1, 1, 1f);
    }
    void GUIGameGrid()
    {
        GUI.color = Color.white;
        int countX = mStageInfo.XCount;
        int countY = mStageInfo.YCount;

        GUILayout.BeginVertical();

        //가장 윗줄(전체 선택, 열 선택, 열 추가를 위한 기능 부분)
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("V", GridButtonSize)) //전체 블럭 변경
        {
            for (int row = 0; row < countY; ++row)
            {
                for (int col = 0; col < countX; ++col)
                {
                    OnClickBlock(col, row);
                }
            }
        }

        for (int col = 0; col < countX; col++)  //선태 열의 전체 블럭 변경
        {
            if (GUILayout.Button("C" + (col + 1), GridButtonSize))
            {
                for (int row = 0; row < countY; ++row)
                {
                    OnClickBlock(col, row);
                }
            }
        }

        if (GUILayout.Button("+", GridButtonSize)) //열 추가
        {
            AddColumn();
        }

        GUILayout.EndHorizontal();

        //실제 게임 블럭 필드 제어 부분
        GUIGameBlockField();

        //마지막 줄은 행 추가 기능 부분
        if (GUILayout.Button("+", GridButtonSize)) //행 추가
        {
            AddRow();
        }
        
        GUILayout.EndVertical();
    }
    void GUIGameBlockField()
    {
        int countX = mStageInfo.XCount;
        int countY = mStageInfo.YCount;

        GUILayout.BeginVertical();
        for (int row = 0; row < countY; row++)
        {
            GUILayout.BeginHorizontal();

            for (int col = 0; col < countX + 1; col++)
            {
                GUI.color = Color.white;
                if(col == 0)
                {
                    if (GUILayout.Button("R" + (row + 1), GridButtonSize))
                    {
                        for (int x = 0; x < countX; ++x)
                        {
                            OnClickBlock(x, row);
                        }
                    }
                    continue;
                }
                else
                {
                    DrawBlock(col - 1, row);
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        CheckFieldDownSizing();
    }
    void CheckFieldDownSizing()
    {
        int countX = mStageInfo.XCount;
        int countY = mStageInfo.YCount;

        bool isLastRowAllDisbled = true;
        for (int x = 0; x < countX; ++x)
        {
            if(!ToCell(x, countY - 1).IsDisabled)
            {
                isLastRowAllDisbled = false;
                break;
            }
        }

        bool isLastColumnAllDisbled = true;
        for (int y = 0; y < countY; ++y)
        {
            if (!ToCell(countX - 1, y).IsDisabled)
            {
                isLastColumnAllDisbled = false;
                break;
            }
        }

        if(isLastRowAllDisbled)
            SubRow();

        if (isLastColumnAllDisbled)
            SubColumn();
    }
    private void DrawBlock(int idxX, int idxY)
    {
        StageInfoCell block = ToCell(idxX, idxY);
        if(block.IsDisabled)
        {
            Color oriColor = GUI.color;
            GUI.color = Color.gray;
            if (GUILayout.Button("x", GridButtonSize)) {
                OnClickBlock(idxX, idxY);
            }
            GUI.color = oriColor;
            return;
        }

        // 블럭의 기본 베이스가 되는 이미지 먼저 그린다.
        if (GUILayout.Button("o", GridButtonSize)) {
            OnClickBlock(idxX, idxY);
        }
        
        if (block.ProductType > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), SkillImages[block.ProductType - 1].texture);
        }
        if (block.CapCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), CapImages[block.CapCount - 1].texture);
        }
        if (block.IceCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), IceImages[block.IceCount - 1].texture);
        }
        if (block.BushCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), BushImages[block.BushCount - 1].texture);
        }
        if (block.RopeCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), RopeImages[block.RopeCount - 1].texture);
        }
    }
    private void OnClickBlock(int idxX, int idxY)
    {
        Event e = Event.current;
        int addCount = e.button == 0 ? 1 : -1;

        StageInfoCell block = ToCell(idxX, idxY);
        switch(CurrentSelection)
        {
            case SelectType.NoBlock:
                {
                    block.ProductType = block.IsDisabled ? 0 : -1;
                    block.CapCount = 0;
                    block.IceCount = 0;
                    block.BushCount = 0;
                    block.RopeCount = 0;
                    break;
                }
            case SelectType.CapProduct:
                {
                    int nextCount = block.CapCount + addCount;
                    block.CapCount = nextCount < 0 ? CapImages.Length : nextCount % (CapImages.Length + 1);
                    break;
                }
            case SelectType.IceProduct:
                {
                    int nextCount = block.IceCount + addCount;
                    block.IceCount = nextCount < 0 ? IceImages.Length : nextCount % (IceImages.Length + 1);
                    break;
                }
            case SelectType.BushFrame:
                {
                    int nextCount = block.BushCount + addCount;
                    block.BushCount = nextCount < 0 ? BushImages.Length : nextCount % (BushImages.Length + 1);
                    break;
                }
            case SelectType.RopeFrame:
                {
                    int nextCount = block.RopeCount + addCount;
                    block.RopeCount = nextCount < 0 ? RopeImages.Length : nextCount % (RopeImages.Length + 1);
                    break;
                }
            case SelectType.SkillProduct:
                {
                    int nextCount = block.ProductType + addCount;
                    block.ProductType = nextCount < 0 ? SkillImages.Length : nextCount % (SkillImages.Length + 1);
                    break;
                }
        }
    }

    private void LoadResources()
    {
        string imagePath = "Assets/Images/";

        CapImages = new Sprite[5];
        CapImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "block.png", typeof(Sprite));
        CapImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "block2.png", typeof(Sprite));
        CapImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "block3.png", typeof(Sprite));
        CapImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "block4.png", typeof(Sprite));
        CapImages[4] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "block5.png", typeof(Sprite));

        IceImages = new Sprite[4];
        IceImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockIce_big.png", typeof(Sprite));
        IceImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockIce_big2.png", typeof(Sprite));
        IceImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockIce_big3.png", typeof(Sprite));
        IceImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockIce_big4.png", typeof(Sprite));

        BushImages = new Sprite[5];
        BushImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "bushFront.png", typeof(Sprite));
        BushImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "bushFront2.png", typeof(Sprite));
        BushImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "bushFront3.png", typeof(Sprite));
        BushImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "bushFront4.png", typeof(Sprite));
        BushImages[4] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "bushFront5.png", typeof(Sprite));

        RopeImages = new Sprite[4];
        RopeImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross1.png", typeof(Sprite));
        RopeImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross2.png", typeof(Sprite));
        RopeImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross3.png", typeof(Sprite));
        RopeImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross4.png", typeof(Sprite));

        SkillImages = new Sprite[6];
        SkillImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "fruitsPack/specials/128x128/sf_specials_d_02.png", typeof(Sprite));
        SkillImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "fruitsPack/specials/128x128/sf_specials_d_01.png", typeof(Sprite));
        SkillImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "skillBomb.png", typeof(Sprite));
        SkillImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "Items/productRainbow.png", typeof(Sprite));
        SkillImages[4] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "fruitsPack/specials/128x128/sf_specials_f_02.png", typeof(Sprite));
        SkillImages[5] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "fruitsPack/specials/128x128/sf_specials_g_01.png", typeof(Sprite));
    }
    private bool LoadFromFile(int levelNum)
    {
        mStageInfo = StageInfo.Load(levelNum);
        if(mStageInfo == null)
        {
            return false;
        }
        TextFieldLevel = levelNum.ToString();
        UpdateGoalTypeIndex();

        RewardTypeA = 0;
        RewardCountA = 0;
        RewardTypeB = 0;
        RewardCountB = 0;
        RewardChest = "None";

        foreach (string reward in mStageInfo.Rewards)
        {
            if(reward.Split(' ').Length > 1)
            {
                RewardChest = reward;
            }
            else if(RewardTypeA == 0)
            {
                string[] rewardParts = reward.Split('/');
                if(rewardParts.Length == 2)
                {
                    RewardTypeA = rewardParts[0] == "gold" ? 1 : rewardParts[0] == "dia" ? 2 : rewardParts[0] == "life" ? 3 : rewardParts[0] == "1" ? 4 : rewardParts[0] == "2" ? 5 : rewardParts[0] == "3" ? 6 : rewardParts[0] == "4" ? 7 : rewardParts[0] == "5" ? 8 : 9;
                    RewardCountA = int.Parse(rewardParts[1]);
                }
            }
            else if(RewardTypeB == 0)
            {
                string[] rewardParts = reward.Split('/');
                if(rewardParts.Length == 2)
                {
                    RewardTypeB = rewardParts[0] == "gold" ? 1 : rewardParts[0] == "dia" ? 2 : rewardParts[0] == "life" ? 3 : rewardParts[0] == "1" ? 4 : rewardParts[0] == "2" ? 5 : rewardParts[0] == "3" ? 6 : rewardParts[0] == "4" ? 7 : rewardParts[0] == "5" ? 8 : 9;
                    RewardCountB = int.Parse(rewardParts[1]);
                }
            }
        }
        return true;
    }
    private void SaveToFile()
    {
        if(mStageInfo == null) return;

        mStageInfo.Rewards.Clear();
        switch (RewardTypeA)
        {
            case 0: break;
            case 1: mStageInfo.Rewards.Add("gold/" + RewardCountA); break;
            case 2: mStageInfo.Rewards.Add("dia/" + RewardCountA); break;
            case 3: mStageInfo.Rewards.Add("life/" + RewardCountA); break;
            case 4: mStageInfo.Rewards.Add("1/" + RewardCountA); break;
            case 5: mStageInfo.Rewards.Add("2/" + RewardCountA); break;
            case 6: mStageInfo.Rewards.Add("3/" + RewardCountA); break;
            case 7: mStageInfo.Rewards.Add("4/" + RewardCountA); break;
            case 8: mStageInfo.Rewards.Add("5/" + RewardCountA); break;
            case 9: mStageInfo.Rewards.Add("6/" + RewardCountA); break;
        }

        switch (RewardTypeB)
        {
            case 0: break;
            case 1: mStageInfo.Rewards.Add("gold/" + RewardCountB); break;
            case 2: mStageInfo.Rewards.Add("dia/" + RewardCountB); break;
            case 3: mStageInfo.Rewards.Add("life/" + RewardCountB); break;
            case 4: mStageInfo.Rewards.Add("1/" + RewardCountB); break;
            case 5: mStageInfo.Rewards.Add("2/" + RewardCountB); break;
            case 6: mStageInfo.Rewards.Add("3/" + RewardCountB); break;
            case 7: mStageInfo.Rewards.Add("4/" + RewardCountB); break;
            case 8: mStageInfo.Rewards.Add("5/" + RewardCountB); break;
            case 9: mStageInfo.Rewards.Add("6/" + RewardCountB); break;
        }

        if(RewardChest != "" && RewardChest != "None" && RewardChest != "0")
        {
            mStageInfo.Rewards.Add(RewardChest);
        }

        mStageInfo.SaveToFile();
    }

    private void UpdateGoalTypeIndex()
    {
        switch (mStageInfo.GoalTypeEnum)
        {
            case StageGoalType.Score: GoalTypeIndex = 0; return;
            case StageGoalType.Cap: GoalTypeIndex = 1; return;
            case StageGoalType.Ice: GoalTypeIndex = 2; return;
            case StageGoalType.Bush: GoalTypeIndex = 3; return;
            case StageGoalType.Rope: GoalTypeIndex = 4; return;
        }
    }
    private void ChangeGoalTypeTo(int index)
    {
        string goalTypeString = "Score";
        switch(index)
        {
            case 0: goalTypeString = "Score"; break;
            case 1: goalTypeString = "Cap"; break;
            case 2: goalTypeString = "Ice"; break;
            case 3: goalTypeString = "Bush"; break;
            case 4: goalTypeString = "Rope"; break;
        }

        mStageInfo.GoalType = goalTypeString;
        mStageInfo.UpdateGoalInfo();
    }
    private int GetGoalCount()
    {
        switch (mStageInfo.GoalTypeEnum)
        {
            case StageGoalType.Cap: return mStageInfo.GetCapCount();
            case StageGoalType.Ice: return mStageInfo.GetIceCount();
            case StageGoalType.Bush: return mStageInfo.GetBushCount();
            case StageGoalType.Rope: return mStageInfo.GetRopeCount();
        }
        return 0;
    }
    private StageInfoCell ToCell(int idxX, int idxY)
    {
        return mStageInfo.BoardInfo[idxY][idxX];
    }
    
    private void AddRow()
    {
        List<StageInfoCell> row = new List<StageInfoCell>();
        for (int i = 0; i < mStageInfo.XCount; ++i)
            row.Add(new StageInfoCell());

        mStageInfo.BoardInfo.Add(row.ToArray());
    }
    private void SubRow()
    {
        mStageInfo.BoardInfo.RemoveAt(mStageInfo.YCount - 1);
    }
    private void AddColumn()
    {
        int rowCount = mStageInfo.YCount;
        for (int y = 0; y < rowCount; ++y)
        {
            List<StageInfoCell> cells = new List<StageInfoCell>();
            cells.AddRange(mStageInfo.BoardInfo[y]);
            cells.Add(new StageInfoCell());
            mStageInfo.BoardInfo[y] = cells.ToArray();
        }
    }
    private void SubColumn()
    {
        int rowCount = mStageInfo.YCount;
        int columCount = mStageInfo.XCount;
        for (int y = 0; y < rowCount; ++y)
        {
            List<StageInfoCell> cells = new List<StageInfoCell>();
            cells.AddRange(mStageInfo.BoardInfo[y]);
            cells.RemoveAt(columCount - 1);
            mStageInfo.BoardInfo[y] = cells.ToArray();
        }
    }
    private void CreateNewStage()
    {
        int stageCount = StageInfo.GetMaxStageNum();
        int newStageNum = stageCount + 1;
        mStageInfo = StageInfo.Load(1);
        StageInfo prevStageInfo = StageInfo.Load(stageCount);
        mStageInfo.Num = newStageNum;
        mStageInfo.GoalType = prevStageInfo.GoalType;
        mStageInfo.GoalValue = 0;
        mStageInfo.GoalTypeEnum = prevStageInfo.GoalTypeEnum;
        mStageInfo.MoveLimit = prevStageInfo.MoveLimit;
        mStageInfo.TimeLimit = prevStageInfo.TimeLimit;
        mStageInfo.ColorCount = prevStageInfo.ColorCount;
        mStageInfo.StarPoint = prevStageInfo.StarPoint;
        mStageInfo.RandomSeed = 0;
        RewardTypeA = 0;
        RewardCountA = 0;
        RewardTypeB = 0;
        RewardCountB = 0;
        RewardChest = "None";
        TextFieldLevel = newStageNum.ToString();
    }
    private void AutoCreateNewStageAndSave()
    {
        mStageInfo = new StageInfo();
        int newStageNum = StageInfo.GetMaxStageNum() + 1;
        mStageInfo.Num = newStageNum;
        mStageInfo.GoalType = "Score";
        int goalValue = (UnityEngine.Random.Range(450, 700) / 20) * 20;
        mStageInfo.GoalValue = goalValue;
        mStageInfo.GoalTypeEnum = StageGoalType.Score;
        int moveLimit = (UnityEngine.Random.Range(25, 45) / 2) * 2;
        mStageInfo.MoveLimit = moveLimit;
        mStageInfo.TimeLimit = 0;
        mStageInfo.StarPoint = 0;
        mStageInfo.RandomSeed = 0;

        int countX = UnityEngine.Random.Range(5, 9);
        int countY = UnityEngine.Random.Range(6, 12);
        for (int y = 0; y < countY; ++y)
        {
            List<StageInfoCell> row = new List<StageInfoCell>();
            for (int x = 0; x < countX; ++x)
            {
                row.Add(new StageInfoCell());
            }
            mStageInfo.BoardInfo.Add(row.ToArray());
        }

        mStageInfo.ColorCount = countX * countY < 45 ? 4 : 5;

        RewardTypeA = 0;
        RewardCountA = 0;
        RewardTypeB = 0;
        RewardCountB = 0;
        RewardChest = "None";
        GoalTypeIndex = 0;
        TextFieldLevel = newStageNum.ToString();

        SaveToFile();
    }
    private void RefreshStage()
    {
        if(mStageInfo == null) return;

        mStageInfo = StageInfo.Load(mStageInfo.Num);
    }
}
#endif