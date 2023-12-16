using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Plane : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(4, 3);
    [SerializeField] private Vector2Int segment = new Vector2Int(4, 3);

    private void Awake()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[(segment.x + 1) * (segment.y + 1)];
        int[] triangles = new int[segment.x * segment.y * 2 * 3];


        Vector2 halfSize = size * 0.5f;
        Vector2 sizeStep = size / segment;
        int vi = 0;
        for (int y = 0; y < segment.y + 1; y++)
        {
            for (int x = 0; x < segment.x + 1; x++)
            {
                vertices[vi++] = new Vector3(sizeStep.x * x - halfSize.x, sizeStep.y * y - halfSize.y, 0);
            }
        }
        int ti = 0;
        for (int y = 0; y < segment.y; y++)
        {
            for (int x = 0; x < segment.x; x++)
            {
                triangles[ti] = x + y * (segment.x + 1);
                triangles[ti + 1] = triangles[ti + 5] = x + (y + 1) * (segment.x + 1);
                triangles[ti + 2] = triangles[ti + 4] = (x + 1) + y * (segment.x + 1);
                triangles[ti + 3] = (x + 1) + (y + 1) * (segment.x + 1);
                ti += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}