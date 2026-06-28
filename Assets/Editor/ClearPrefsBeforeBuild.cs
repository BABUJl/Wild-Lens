using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class ClearPrefsBeforeBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared before build.");
    }
}
