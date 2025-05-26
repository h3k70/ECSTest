using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.UIElements;

public class TestSpawn : MonoBehaviour
{
    public GameObject Pref;
    public int columns = 99;
    public int rows = 99;
    public int spacing = 1;

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        // Спавним префабы в сетке
        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 position = new Vector3(x * spacing, 0, z * spacing);
                GameObject instance = Instantiate(Pref);
                instance.transform.SetParent(transform);
                instance.transform.localPosition = position;
            }
        }
    }

    [ContextMenu("UnSpawn")]
    public void ClearChildren()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}
