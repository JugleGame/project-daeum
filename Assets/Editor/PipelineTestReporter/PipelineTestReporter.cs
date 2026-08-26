// Copied from Game-Develop-Orchestration/templates/unity-editor.
// Editor-only named-test evidence reporter; excluded from player builds by its asmdef.

using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class PipelineTestReporter
{
    public const string StatusKey = "pipeline.tests.status";
    public const string ResultsKey = "pipeline.tests.results";
    public const string CountKey = "pipeline.tests.count";

    private static readonly TestRunnerApi Api;
    private static readonly StringBuilder Collected = new StringBuilder();
    private static int collectedCount;

    static PipelineTestReporter()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new Callbacks());
    }

    public static void Reset()
    {
        Collected.Length = 0;
        collectedCount = 0;
        EditorPrefs.SetString(StatusKey, "starting");
        EditorPrefs.SetString(ResultsKey, "");
        EditorPrefs.SetInt(CountKey, 0);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value
            .Replace("\\", "/")
            .Replace("\"", "'")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Collected.Length = 0;
            collectedCount = 0;
            EditorPrefs.SetString(StatusKey, "running");
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite)
            {
                return;
            }

            if (collectedCount > 0)
            {
                Collected.Append(",");
            }

            Collected.Append("{\"name\":\"").Append(Escape(result.Test.Name))
                .Append("\",\"fullName\":\"").Append(Escape(result.Test.FullName))
                .Append("\",\"status\":\"").Append(result.TestStatus)
                .Append("\",\"durationSeconds\":").Append(result.Duration.ToString("F3"))
                .Append(",\"message\":\"").Append(Escape(result.Message))
                .Append("\"}");
            collectedCount++;

            EditorPrefs.SetString(ResultsKey, "[" + Collected + "]");
            EditorPrefs.SetInt(CountKey, collectedCount);
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            EditorPrefs.SetString(ResultsKey, "[" + Collected + "]");
            EditorPrefs.SetInt(CountKey, collectedCount);
            EditorPrefs.SetString(StatusKey, "completed");
        }
    }
}
