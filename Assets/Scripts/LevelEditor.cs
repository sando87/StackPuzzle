using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

#if UNITY_EDITOR

public class LevelEditor : EditorWindow
{
    enum SelectType { NoBlock, CapProduct, IceProduct, BushFrame, RopeFrame }

    public Sprite[] IceImages = null;
    public Sprite[] CapImages = null;
    public Sprite[] BushImages = null;
    public Sprite[] RopeImages = null;

    //블럭 사이즈 정의
    private GUILayoutOption[] GridButtonSize = new GUILayoutOption[2] { GUILayout.Width(50), GUILayout.Height(50) };
    private GUILayoutOption[] GridButtonSizeHalf = new GUILayoutOption[2] { GUILayout.Width(25), GUILayout.Height(25) };

    private Vector2 ScrollPosition = Vector2.zero;
    private SelectType CurrentSelection = SelectType.NoBlock;
    private string TextFieldLevel = "";
    private int GoalTypeIndex = 0;
    string[] GoalTypeList = new string[] { "Score", "Cap", "Ice", "Bush", "Rope" };

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
        if (GUILayout.Button("New", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            CreateNewStage();
        }
        if (GUILayout.Button("Save", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            SaveToFile();
        }
        if (GUILayout.Button("Refresh", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            RefreshStage();
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

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Label("StarPoint", EditorStyles.label);
        GUILayout.FlexibleSpace();
        mStageInfo.StarPoint = EditorGUILayout.IntField(mStageInfo.StarPoint, new GUILayoutOption[1] { GUILayout.Width(100) });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("RandomSeed", EditorStyles.label);
        GUILayout.FlexibleSpace();
        mStageInfo.RandomSeed = EditorGUILayout.IntField(mStageInfo.RandomSeed, new GUILayoutOption[1] { GUILayout.Width(100) });
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

        if (block.CapCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), CapImages[block.CapCount - 1].texture);
        }
        if (block.ChocoCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), IceImages[block.ChocoCount - 1].texture);
        }
        if (block.BushCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), BushImages[block.BushCount - 1].texture);
        }
        if (block.CoverCount > 0)
        {
            GUI.Box(GUILayoutUtility.GetLastRect(), RopeImages[block.CoverCount - 1].texture);
        }
    }
    private void OnClickBlock(int idxX, int idxY)
    {
        StageInfoCell block = ToCell(idxX, idxY);
        switch(CurrentSelection)
        {
            case SelectType.NoBlock:
                {
                    block.IsDisabled = !block.IsDisabled;
                    break;
                }
            case SelectType.CapProduct:
                {
                    block.CapCount++;
                    block.CapCount %= CapImages.Length + 1;
                    break;
                }
            case SelectType.IceProduct:
                {
                    block.ChocoCount++;
                    block.ChocoCount %= IceImages.Length + 1;
                    break;
                }
            case SelectType.BushFrame:
                {
                    block.BushCount++;
                    block.BushCount %= BushImages.Length + 1;
                    break;
                }
            case SelectType.RopeFrame:
                {
                    block.CoverCount++;
                    block.CoverCount %= RopeImages.Length + 1;
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

        IceImages = new Sprite[1];
        IceImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockIce_big.png", typeof(Sprite));

        BushImages = new Sprite[1];
        BushImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "bushFront.png", typeof(Sprite));

        RopeImages = new Sprite[4];
        RopeImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross1.png", typeof(Sprite));
        RopeImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross2.png", typeof(Sprite));
        RopeImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross3.png", typeof(Sprite));
        RopeImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "cross4.png", typeof(Sprite));
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
        return true;
    }
    private void SaveToFile()
    {
        if(mStageInfo == null) return;

        mStageInfo.SaveToFile();
    }

    private void UpdateGoalTypeIndex()
    {
        switch (mStageInfo.GoalTypeEnum)
        {
            case StageGoalType.Score: GoalTypeIndex = 0; return;
            case StageGoalType.Cap: GoalTypeIndex = 1; return;
            case StageGoalType.Choco: GoalTypeIndex = 2; return;
            case StageGoalType.Bush: GoalTypeIndex = 3; return;
            case StageGoalType.Cover: GoalTypeIndex = 4; return;
        }
    }
    private void ChangeGoalTypeTo(int index)
    {
        string goalTypeString = "Score";
        switch(index)
        {
            case 0: goalTypeString = "Score"; break;
            case 1: goalTypeString = "Cap"; break;
            case 2: goalTypeString = "Choco"; break;
            case 3: goalTypeString = "Bush"; break;
            case 4: goalTypeString = "Cover"; break;
        }

        mStageInfo.GoalType = goalTypeString;
        mStageInfo.UpdateGoalInfo();
    }
    private int GetGoalCount()
    {
        switch (mStageInfo.GoalTypeEnum)
        {
            case StageGoalType.Cap: return mStageInfo.GetCapCount();
            case StageGoalType.Choco: return mStageInfo.GetChocoCount();
            case StageGoalType.Bush: return mStageInfo.GetBushCount();
            case StageGoalType.Cover: return mStageInfo.GetCoverCount();
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
        mStageInfo.Num = newStageNum;
        TextFieldLevel = newStageNum.ToString();
    }
    private void RefreshStage()
    {
        if(mStageInfo == null) return;

        mStageInfo = StageInfo.Load(mStageInfo.Num);
    }
}
#endif