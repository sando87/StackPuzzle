using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

#if UNITY_EDITOR

public class LevelEditor : EditorWindow
{
    public Sprite NoBlockImage = null;

    //블럭 사이즈 정의
    private GUILayoutOption[] GridButtonSize = new GUILayoutOption[2] { GUILayout.Width(50), GUILayout.Height(50) };
    private GUILayoutOption[] GridButtonSizeHalf = new GUILayoutOption[2] { GUILayout.Width(25), GUILayout.Height(25) };
    private int mCountX = 5;
    private int mCountY = 5;

    private Vector2 ScrollPosition = Vector2.zero;
    private int SelectorIndex = 0;
    private string TextFieldLevel = "";
    private string TextFieldTurn = "";
    private string TextFieldBomb = "";
    GUIStyle BombCountTextStyle = null; //주변 폭탄 개수를 표시하는 Text 스타일

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
        //LoadLevel(1);

        //최초 실행시 비활성화 블럭이 선택된 상태로 시작
        SelectorIndex = 0;

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
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.BeginVertical(); //오른쪽에 블럭 배치 제어 Layout창
            {
                GUIBlockSelector();
                GUILayout.Label("=============================================================================================", EditorStyles.boldLabel);
                GUIGameGrid();
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
    void GUILevelController()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<< 이전레벨", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            // LoadLevel(prevNumber);
        }

        GUILayout.FlexibleSpace();

        TextFieldLevel = GUILayout.TextField(TextFieldLevel, new GUILayoutOption[] { GUILayout.Width(60) });
        if (int.TryParse(TextFieldLevel, out int userInputNumber))// && 0 < userInputNumber && userInputNumber <= LevelSpecs.Count)
        {
            // if (userInputNumber != DisplayInfo.LevelNumber)
            // {
            //     LoadLevel(userInputNumber);
            // }
        }
        else
        {
            // MapBlocks.Clear();
            // DisplayInfo = new LevelEditorBoard();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("다음레벨 >>", new GUILayoutOption[] { GUILayout.Width(90) }))
        {
            // int nextNumber = Mathf.Min(DisplayInfo.LevelNumber + 1, LevelSpecs.Count);
            // LoadLevel(nextNumber);
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
            GUI.color = SelectorIndex == 0 ? enabledColor : disabledColor;
            if (GUILayout.Button("X", GridButtonSize))
            {
                SelectorIndex = 0;
            }
            GUI.color = SelectorIndex == 1 ? enabledColor : disabledColor;
            if (GUILayout.Button(NoBlockImage.texture, GridButtonSize))
            {
                SelectorIndex = 1;
            }
            GUI.color = SelectorIndex == 2 ? enabledColor : disabledColor;
            if (GUILayout.Button(NoBlockImage.texture, GridButtonSize))
            {
                SelectorIndex = 2;
            }
            GUI.color = SelectorIndex == 3 ? enabledColor : disabledColor;
            if (GUILayout.Button(NoBlockImage.texture, GridButtonSize))
            {
                SelectorIndex = 3;
            }
            GUI.color = SelectorIndex == 4 ? enabledColor : disabledColor;
            if (GUILayout.Button(NoBlockImage.texture, GridButtonSize))
            {
                SelectorIndex = 4;
            }
            GUI.color = SelectorIndex == 5 ? enabledColor : disabledColor;
            if (GUILayout.Button(NoBlockImage.texture, GridButtonSize))
            {
                SelectorIndex = 5;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUI.color = new Color(1, 1, 1, 1f);
    }
    void GUIGameGrid()
    {
        GUI.color = Color.white;
        int countX = mCountX;
        int countY = mCountY;

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
        int countX = mCountX;
        int countY = mCountY;

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

        NoBlockImage = (Sprite)AssetDatabase.LoadAssetAtPath(imagePath + "blockStone_big.png", typeof(Sprite));
    }
    private void LoadLevel(int levelNumber)
    {
        // if(!LoadFromFile(levelNumber)) return;

        // InitDisplayInfo(levelNumber);
        // UpdateLevelSpecData();
    }
    private void DrawBlock(int idxX, int idxY)
    {
        GUILayout.Button("X", GridButtonSize);

        // BlockRawData block = MapBlocks[ToIndex(idxX, idxY)];
        // UpdateAroundBombCount(idxX, idxY);

        // if(block.IsDisabled)
        // {
        //     if (GUILayout.Button("X", GridButtonSize)) {
        //         OnClickBlock(idxX, idxY);
        //     }
        //     return;
        // }

        // // 블럭의 기본 베이스가 되는 이미지 먼저 그린다.
        // if (block.IsBroken) //깨진 블럭이 우선순위가 가장 높고
        // {
        //     if (GUILayout.Button(BrokenBlockImage.texture, GridButtonSize)) {
        //         OnClickBlock(idxX, idxY);
        //     }
        // }
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
        LOG.trace();
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
    private int ToIndex(int idxX, int idxY) { return idxY * mCountX + idxX; }


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
        // string stagePath = "Assets/GoldMineSweeper/Resources/" + CDefine.LEVEL_SPEC_TABLE + ".asset";
        // LevelSpecTable levelTable = AssetDatabase.LoadAssetAtPath<LevelSpecTable>(stagePath);
        // if(levelTable == null) return false;

        // LevelSpecs.Clear();
        // foreach(LevelSpec lv in levelTable.EnumLevels())
        // {
        //     LevelSpecs.Add(lv);
        // }

        // if(levelNum <= 0 || LevelSpecs.Count < levelNum) return false;

        // string mapPath = "Assets/GoldMineSweeper/Resources/" + CDefine.LEVEL_TARGET_PATH + "/" + LevelSpecs[levelNum - 1].MapFilename + ".asset";
        // LevelMapTable mapTable = AssetDatabase.LoadAssetAtPath<LevelMapTable>(mapPath);
        // if(mapTable == null)
        // {
        //     return false;
        // }

        // MapBlocks.Clear();
        // foreach (BlockRawData block in mapTable.EnumBlocks())
        // {
        //     MapBlocks.Add(block);
        // }
        // mCountX = mapTable.CountX;
        // mCountY = mapTable.CountY;
        return true;
    }
    private void SaveToFile()
    {
        // if (MapBlocks.Count <= 0 || LevelSpecs.Count <= 0) return;

        // string stagePath = "Assets/GoldMineSweeper/Resources/" + CDefine.LEVEL_SPEC_TABLE + ".asset";
        // LevelSpecTable levelTable = AssetDatabase.LoadAssetAtPath<LevelSpecTable>(stagePath);
        // levelTable.InvokePrivateMethod("UpdateSpecTable", new object[] { LevelSpecs.ToArray() });

        // int levelNum = DisplayInfo.LevelNumber;
        // string mapPath = "Assets/GoldMineSweeper/Resources/" + CDefine.LEVEL_TARGET_PATH + "/" + LevelSpecs[levelNum - 1].MapFilename + ".asset";
        // LevelMapTable mapTable = AssetDatabase.LoadAssetAtPath<LevelMapTable>(mapPath);
        // if(mapTable == null)
        // {
        //     mapTable = ScriptableObject.CreateInstance<LevelMapTable>();
        //     UnityEditor.AssetDatabase.CreateAsset(mapTable, mapPath);
        //     UnityEditor.AssetDatabase.Refresh();
        // }
        // mapTable.InvokePrivateMethod("UpdateMapTable", new object[] { MapBlocks.ToArray(), mCountX, mCountY });
    }
    private void AddRow()
    {
        // MapBlocks.AddRange(new BlockRawData[mCountX]);
        // mCountY++;
    }
    private void SubRow()
    {
        // MapBlocks.RemoveRange(MapBlocks.Count - mCountX, mCountX);
        // mCountY--;
    }
    private void AddColumn()
    {
        // List<BlockRawData> newBlocks = new List<BlockRawData>();
        // for (int y = 0; y < mCountY; ++y)
        // {
        //     newBlocks.AddRange(MapBlocks.GetRange(y * mCountX, mCountX));
        //     newBlocks.Add(new BlockRawData());
        // }
        // MapBlocks = newBlocks;
        // mCountX++;
    }
    private void SubColumn()
    {
        // List<BlockRawData> newBlocks = new List<BlockRawData>();
        // for (int y = 0; y < mCountY; ++y)
        // {
        //     newBlocks.AddRange(MapBlocks.GetRange(y * mCountX, mCountX - 1));
        // }
        // MapBlocks = newBlocks;
        // mCountX--;
    }
}
#endif