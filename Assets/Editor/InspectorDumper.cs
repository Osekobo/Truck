using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class InspectorDumper
{
    [MenuItem("Tools/Dump All Inspector Values")]
    public static void DumpAll()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== INSPECTOR VALUE DUMP ===");
        sb.AppendLine("Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        sb.AppendLine();

        MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        System.Array.Sort(all, (a, b) => a.GetType().Name.CompareTo(b.GetType().Name));

        foreach (MonoBehaviour mb in all)
        {
            if (mb == null) continue;
            sb.AppendLine("--- " + mb.GetType().FullName + " on " + GetPath(mb.gameObject) + " ---");

            FieldInfo[] fields = mb.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
            {
                object val = f.GetValue(mb);
                string display = FormatValue(val);
                sb.AppendLine("  " + f.FieldType.Name + " " + f.Name + " = " + display);
            }
            sb.AppendLine();
        }

        string path = Path.Combine(Application.dataPath, "..", "InspectorDump.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log("Dumped inspector values to: " + path);
        AssetDatabase.Refresh();
    }

    static string FormatValue(object val)
    {
        if (val == null) return "null";
        if (val is Object uo) return uo == null ? "null" : uo.name;
        if (val is Vector3 v) return v.ToString("F3");
        if (val is Vector2 v2) return v2.ToString("F3");
        if (val is Quaternion q) return q.eulerAngles.ToString("F1");
        if (val is Color c) return c.ToString();
        return val.ToString();
    }

    static string GetPath(GameObject go)
    {
        string p = go.name;
        Transform t = go.transform.parent;
        while (t != null) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }
}
