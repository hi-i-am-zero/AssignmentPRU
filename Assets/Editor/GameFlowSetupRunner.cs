#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Batchmode entry: Unity -batchmode -executeMethod GameFlowSetupRunner.Run
/// </summary>
public static class GameFlowSetupRunner
{
    public static void Run()
    {
        GameFlowSceneBuilder.SetupAll();
        EditorApplication.Exit(0);
    }
}
#endif
