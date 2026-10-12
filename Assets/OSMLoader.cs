using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

public class OSMLoader : MonoBehaviour
{
    [Header("Input")]
    public string osmFilePath = "Assets/OSM/nairobi_roads.osm";
    public Material roadMaterial;

    [Header("Geo Reference")]
    public double centerLat = -1.2921;
    public double centerLon = 36.8219;

    [Header("Scale and Offset")]
    public float scale = 1f;
    public Vector3 offset = Vector3.zero;

    [Header("Line Settings")]
    public float defaultLineWidth = 8f;
    public float lineY = 0f;

    [Header("Terrain Alignment")]
    public Terrain terrain;
    public float heightOffset = 0.5f;

    void Start() { Build(); }

    [ContextMenu("Rebuild Roads")]
    public void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        string fullPath = Path.Combine(Application.dataPath, "..", osmFilePath);
        if (!File.Exists(fullPath)) { Debug.LogError("OSM file not found: " + fullPath); return; }

        Dictionary<long, Vector2> nodes = new Dictionary<long, Vector2>();
        List<List<long>> ways = new List<List<long>>();
        List<string> wayNames = new List<string>();
        List<string> wayHighwayTypes = new List<string>();

        XmlDocument doc = new XmlDocument();
        doc.Load(fullPath);

        foreach (XmlNode n in doc.SelectNodes("//node"))
        {
            long id = long.Parse(n.Attributes["id"].Value);
            double lat = double.Parse(n.Attributes["lat"].Value, CultureInfo.InvariantCulture);
            double lon = double.Parse(n.Attributes["lon"].Value, CultureInfo.InvariantCulture);
            nodes[id] = ConvertGeo(lat, lon);
        }

        foreach (XmlNode w in doc.SelectNodes("//way"))
        {
            List<long> refs = new List<long>();
            string gpxDesc = null;
            string gpxName = null;

            foreach (XmlNode child in w.ChildNodes)
            {
                if (child.Name == "nd")
                    refs.Add(long.Parse(child.Attributes["ref"].Value));
                else if (child.Name == "tag")
                {
                    string k = child.Attributes["k"].Value;
                    if (k == "gpx:desc") gpxDesc = child.Attributes["v"].Value;
                    else if (k == "gpx:name") gpxName = child.Attributes["v"].Value;
                }
            }

            bool isHighway = gpxDesc != null && gpxDesc.Contains("highway=");
            if (isHighway && refs.Count >= 2)
            {
                ways.Add(refs);
                wayNames.Add(gpxName ?? "Road");
                wayHighwayTypes.Add(ExtractValue(gpxDesc, "highway"));
            }
        }

        Debug.Log("OSM: " + nodes.Count + " nodes, " + ways.Count + " highway ways");

        for (int w = 0; w < ways.Count; w++)
        {
            var way = ways[w];
            GameObject go = new GameObject(wayNames[w] + "_" + w);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            float width = WidthFor(wayHighwayTypes[w]) * scale;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.numCapVertices = 2;
            lr.positionCount = way.Count;
            if (roadMaterial != null) lr.material = roadMaterial;

            for (int i = 0; i < way.Count; i++)
            {
                Vector2 p = nodes[way[i]];
                Vector3 localPos = new Vector3(p.x + offset.x, 0f, p.y + offset.z);
                Vector3 worldPos = transform.TransformPoint(localPos);

                float finalY;
                if (terrain != null)
                {
                    float terrainY = terrain.SampleHeight(worldPos);
                    finalY = terrainY + heightOffset;
                }
                else
                {
                    finalY = lineY + offset.y;
                }

                worldPos.y = finalY;
                lr.SetPosition(i, transform.InverseTransformPoint(worldPos));
            }
        }
    }

    float WidthFor(string highwayType)
    {
        switch (highwayType)
        {
            case "motorway": return 20f;
            case "trunk": return 16f;
            case "primary": return 12f;
            case "secondary": return 10f;
            case "tertiary": return 8f;
            case "residential": return 6f;
            default: return defaultLineWidth;
        }
    }

    string ExtractValue(string desc, string key)
    {
        if (string.IsNullOrEmpty(desc)) return "";
        string search = key + "=";
        int idx = desc.IndexOf(search);
        if (idx < 0) return "";
        int start = idx + search.Length;
        int end = desc.IndexOf('\n', start);
        if (end < 0) end = desc.Length;
        return desc.Substring(start, end - start).Trim();
    }

    Vector2 ConvertGeo(double lat, double lon)
    {
        double mPerLat = 111320.0;
        double mPerLon = 111320.0 * System.Math.Cos(centerLat * System.Math.PI / 180.0);
        float x = (float)((lon - centerLon) * mPerLon * scale);
        float z = (float)((lat - centerLat) * mPerLat * scale);
        return new Vector2(x, z);
    }
}
