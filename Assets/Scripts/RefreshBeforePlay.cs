#if UNITY_EDITOR

using UnityEditor;


[InitializeOnLoadAttribute]
public static class RefreshBeforePlay
{
    static RefreshBeforePlay()
    {
        EditorApplication.playModeStateChanged += PlayRefresh;
    }
    private static void PlayRefresh(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            AssetDatabase.Refresh();
        }
    }
}

#endif