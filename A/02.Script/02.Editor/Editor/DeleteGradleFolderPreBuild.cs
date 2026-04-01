using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

public class DeleteGradleFolderPreBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            string gradleFolderPath = Path.Combine("Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle");

            if (Directory.Exists(gradleFolderPath))
            {
                try
                {
                    Directory.Delete(gradleFolderPath, true);
                    UnityEngine.Debug.Log($"[PreBuild] Gradle 폴더 삭제 완료: {gradleFolderPath}");
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[PreBuild] Gradle 폴더 삭제 실패: {e.Message}");
                }
            }
        }
    }
}
