using UnityEngine;
using System.IO;

public class RawTerrainImporter : MonoBehaviour
{
    public string filePath = "C:/Users/LENOVO/Downloads/NairobiTruckS/nairobi_1025.raw";
    public int resolution = 1025;

    [ContextMenu("Import Terrain Now")]
    public void ImportTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) { Debug.LogError("No Terrain component found!"); return; }

        if (!File.Exists(filePath)) { Debug.LogError("File not found at: " + filePath); return; }

        byte[] data = File.ReadAllBytes(filePath);
        float[,] heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int index = (y * resolution + x) * 2;
                ushort value = (ushort)(data[index] | (data[index + 1] << 8)); // Windows LSB
                heights[y, x] = (float)value / 65535f; 
            }
        }

        terrain.terrainData.SetHeights(0, 0, heights);
        Debug.Log("Terrain imported successfully!");
    }
}