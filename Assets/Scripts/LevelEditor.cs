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
    GUIStyle BombCountTextStyle = null; //주변 폭탄 개수를 표시하는 Text 스타일

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
        LoadLevel(1);

        //최초 실행시 비활성화 블럭이 선택된 상태로 시작
        CurrentSelection = SelectType.NoBlock;

        BombCountTextStyle = new GUIStyle(); //GUI.skin.GetStyle("Label");
        BombCountTextStyle.alignment = TextAnchor.MiddleCenter;
        BombCountTextStyle.fontSize = 16;
        BombCountTextStyle.fontStyle = FontStyle.Bold;
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
                LoadLevel(mStageInfo.Num - 1);
            }
        }

        GUILayout.FlexibleSpace();

        TextFieldLevel = GUILayout.TextField(TextFieldLevel, new GUILayoutOption[] { GUILayout.Width(60) });
        if (int.TryParse(TextFieldLevel, out int userInputNumber))// && 0 < userInputNumber && userInputNumber <= LevelSpecs.Count)
        {
            LoadLevel(userInputNumber);
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
                LoadLevel(mStageInfo.Num + 1);
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    void GUILevelStageMeta()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("New", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
        }
        if (GUILayout.Button("Save", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
        }
        if (GUILayout.Button("Refresh", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
        }

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
            if (GUILayout.Button(IceImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.CapProduct;
            }
            GUI.color = CurrentSelection == SelectType.IceProduct ? enabledColor : disabledColor;
            if (GUILayout.Button(IceImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.IceProduct;
            }
            GUI.color = CurrentSelection == SelectType.BushFrame ? enabledColor : disabledColor;
            if (GUILayout.Button(IceImages[0].texture, GridButtonSize))
            {
                CurrentSelection = SelectType.BushFrame;
            }
            GUI.color = CurrentSelection == SelectType.RopeFrame ? enabledColor : disabledColor;
            if (GUILayout.Button(IceImages[0].texture, GridButtonSize))
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

                DrawBlock(col - 1, row);

            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        CheckFieldDownSizing();
    }
    void CheckFieldDownSizing()
    {
        // int countX = mCountX;
        // int countY = mCountY;

        // bool isLastRowAllDisbled = true;
        // for (int x = 0; x < countX; ++x)
        // {
        //     if(!MapBlocks[ToIndex(x, countY - 1)].IsDisabled)
        //     {
        //         isLastRowAllDisbled = false;
        //         break;
        //     }
        // }

        // bool isLastColumnAllDisbled = true;
        // for (int y = 0; y < countY; ++y)
        // {
        //     if (!MapBlocks[ToIndex(countX - 1, y)].IsDisabled)
        //     {
        //         isLastColumnAllDisbled = false;
        //         break;
        //     }
        // }

        // if(isLastRowAllDisbled)
        //     SubRow();

        // if (isLastColumnAllDisbled)
        //     SubColumn();
    }


    private void LoadResources()
    {
        string imagePath = "Assets/Images/";

        IceImages = new Sprite[4];
        IceImages[0] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockStone_big.png", typeof(Sprite));
        IceImages[1] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockStone_big.png", typeof(Sprite));
        IceImages[2] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockStone_big.png", typeof(Sprite));
        IceImages[3] = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockStone_big.png", typeof(Sprite));
    }
    private void LoadLevel(int levelNumber)
    {
        LoadFromFile(levelNumber);
    }
    
    private void DrawBlock(int idxX, int idxY)
    {
        StageInfoCell block = ToCell(idxX, idxY);
        if(block.IsDisabled)
        {
            if (GUILayout.Button("x", GridButtonSize)) {
                OnClickBlock(idxX, idxY);
            }
            return;
        }

        // 블럭의 기본 베이스가 되는 이미지 먼저 그린다.
        if (GUILayout.Button("o", GridButtonSize)) {
            OnClickBlock(idxX, idxY);
        }

        // else if(block.Gimmick == FileBlcokType.UnBreakable) //다음으로 깨질수없는 기믹 블럭
        // {
        //     if (GUILayout.Button(UnBreakableImage.texture, GridButtonSize)) {
        //         OnClickBlock(idxX, idxY);
        //     }
        // }
        // else //다음으로 기본 색상이 반영된다.
        // {
        //     Texture colorBlockTex = BlockImgaes[(int)block.BlockColor].texture;
        //     if (GUILayout.Button(colorBlockTex, GridButtonSize)) {
        //         OnClickBlock(idxX, idxY);
        //     }
        // }

        // // Overlap되어 겹쳐 그려져야 하는 폭탄이나 이동 기믹 블럭들을 그려준다
        // if (block.IsJelly)
        // {
        //     GUI.Box(GUILayoutUtility.GetLastRect(), JellyImage.texture);
        // }
        // if (block.Gimmick == FileBlcokType.Returnable)
        // {
        //     GUI.Box(GUILayoutUtility.GetLastRect(), ReturnableImage.texture);
        // }
        // if (block.Gimmick == FileBlcokType.Movable)
        // {
        //     Texture arrowTexture = null;
        //     switch (block.MoveDirection)
        //     {
        //         case OutlinePosition.Right: arrowTexture = ArrowImages[0].texture; break;
        //         case OutlinePosition.Left: arrowTexture = ArrowImages[1].texture; break;
        //         case OutlinePosition.Top: arrowTexture = ArrowImages[2].texture; break;
        //         default: arrowTexture = ArrowImages[3].texture; break;
        //     }
        //     GUI.Box(GUILayoutUtility.GetLastRect(), arrowTexture);
        // }
        // if (block.IsBomb)
        // {
        //     Rect bombRect = GUILayoutUtility.GetLastRect();
        //     bombRect.center += new Vector2(12.5f, 12.5f);
        //     GUIStyle bombSize = new GUIStyle();
        //     bombSize.fixedWidth = 25;
        //     bombSize.fixedHeight = 25;
        //     GUI.Box(bombRect, BombImage.texture, bombSize);
        // }
        // if (block.PreventBomb)
        // {
        //     Rect noBombRect = GUILayoutUtility.GetLastRect();
        //     noBombRect.center += new Vector2(12.5f, 12.5f);
        //     GUIStyle noBombSize = new GUIStyle();
        //     noBombSize.fixedWidth = 25;
        //     noBombSize.fixedHeight = 25;
        //     GUI.Box(noBombRect, NoBombImage.texture, noBombSize);
        // }
        // if (block.Number > 0 && block.IsBroken)
        // {
        //     BombCountTextStyle.normal.textColor = BlockUnit.TextCountToColor(block.Number);
        //     GUI.Label(GUILayoutUtility.GetLastRect(), block.Number.ToString(), BombCountTextStyle);
        // }
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
                    block.CapCount %= 4;
                    break;
                }
            case SelectType.IceProduct:
                {
                    block.ChocoCount++;
                    block.ChocoCount %= 4;
                    break;
                }
            case SelectType.BushFrame:
                {
                    block.BushCount++;
                    block.BushCount %= 4;
                    break;
                }
            case SelectType.RopeFrame:
                {
                    block.CoverCount++;
                    block.CoverCount %= 4;
                    break;
                }
        }
    }
    // private int AddNewLevel()
    // {
    //     LevelSpec newData = new LevelSpec();
    //     newData.Number = LevelSpecs.Count + 1;
    //     newData.MaxMoves = 8;
    //     newData.GoldTargetCount = 0;
    //     newData.MapFilename = CDefine.LEVEL_MAP_SUFFIX + DateTime.Now.ToString("MMdd_hhmmss");
    //     LevelSpecs.Add(newData);

    //     MapBlocks.Clear();
    //     MapBlocks.AddRange(new BlockRawData[25]);
    //     mCountX = 5;
    //     mCountY = 5;

    //     return newData.Number;
    // }
    
    private StageInfoCell ToCell(int idxX, int idxY) 
    { 
        return mStageInfo.BoardInfo[idxY][idxX];
    }


    public static void ReArrangeLevels()
    {
        // string stagePath = "Assets/GoldMineSweeper/Resources/" + CDefine.LEVEL_SPEC_TABLE + ".asset";
        // LevelSpecTable levelTable = AssetDatabase.LoadAssetAtPath<LevelSpecTable>(stagePath);

        // List<LevelSpec> tmpLevels = new List<LevelSpec>();
        // foreach (LevelSpec lv in levelTable.EnumLevels())
        // {
        //     LevelSpec spec = lv;
        //     spec.Number = tmpLevels.Count + 1;
        //     tmpLevels.Add(spec);
        // }

        // levelTable.InvokePrivateMethod("UpdateSpecTable", new object[] { tmpLevels.ToArray() });
    }

    private bool LoadFromFile(int levelNum)
    {
        mStageInfo = StageInfo.Load(levelNum);
        if(mStageInfo == null)
        {
            return false;
        }
        TextFieldLevel = levelNum.ToString();
        return true;
    }
    private void SaveToFile()
    {
        if(mStageInfo == null) return;

        mStageInfo.SaveToFile();
    }
    
    private void AddRow()
    {
        mStageInfo.BoardInfo.Add(new StageInfoCell[mStageInfo.XCount]);
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
        int stageCount = StageInfo.GetStageCount();
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