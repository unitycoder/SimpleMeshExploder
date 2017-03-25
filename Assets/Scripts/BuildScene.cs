using UnityEngine;
using System.Collections;

// builds stack of boxes

public class BuildScene : MonoBehaviour
{
    public Transform prefab;

    public int width = 3;
    public int heigth = 3;
    public int depth = 3;

    void Start()
    {
        Vector3 pos = Vector3.zero;
        Vector3 o = prefab.GetComponent<Renderer>().bounds.size + new Vector3(0.03f, 0.03f, 0.03f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < heigth; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    pos = new Vector3(x * o.x, y * o.y, z * o.z);
                    Instantiate(prefab, pos, Quaternion.identity);
                }
            }
        }

    }
}
