using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unitycoder.Demos
{
    public class SimpleMeshExploder : MonoBehaviour
    {
        // extra effects to be instantiated
        public Transform exploPrefab;
        public Transform smokePrefab;

        [Header("Settings")]
        public bool inheritRbProperties = false;

        Camera cam;
        Vector3 sdir;
        Vector3 tdir;
        List<Vector3> normals = new List<Vector3>();

        public static SimpleMeshExploder instance;

        private void Awake()
        {
            if (instance != null) DestroyImmediate(this);
            instance = this;
        }

        void Start()
        {
            cam = Camera.main;
        }

        void Update()
        {
            // for testing only, R to reset scene
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Scene Reload");
                SceneManager.LoadScene(0);
            }

            // use left mouse button to explode objects
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    if (hit.transform.CompareTag("Explodable"))
                    {
                        Explode(hit.transform);
                    }
                }
            }
        }


        public void Explode(Transform target)
        {
            // spawn explosion effect (particles)
            Transform clone = Instantiate(exploPrefab, target.position, Quaternion.identity) as Transform;
            Destroy(clone.gameObject, 5);

            clone = Instantiate(smokePrefab, target.position, Quaternion.identity) as Transform;
            Destroy(clone.gameObject, 10);

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            int index = 0;

            // grab target rb properties
            Vector3 velocity = Vector3.zero;
            Vector3 angularVelocity = Vector3.zero;
            float mass = 1;
            bool useGravity = true;
            if (inheritRbProperties == true)
            {
                Rigidbody rb;
                if (target.TryGetComponent<Rigidbody>(out rb))
                {
                    velocity = rb.velocity;
                    angularVelocity = rb.angularVelocity;
                    mass = rb.mass / (float)triangles.Length;
                    useGravity = rb.useGravity;
                }
            }


            // remove collider from original
            target.GetComponent<Collider>().enabled = false;

            // get each face
            for (int i = 0, tris = triangles.Length; i < tris; i += 3)
            {
                // TODO: inherit speed, spin, from rigidbody?
                Vector3 averageNormal = (normals[triangles[i]] + normals[triangles[i + 1]] + normals[triangles[i + 2]]).normalized;
                var targetRenderer = target.GetComponent<Renderer>();
                Vector3 s = targetRenderer.bounds.size;
                float extrudeSize = ((s.x + s.y + s.z) / 3) * 0.22f; // magic number, for nice size 0.3 causes explosions on cubes

                CreateMeshPiece(useGravity, mass, velocity, angularVelocity, extrudeSize, target.transform.rotation, target.transform.position, targetRenderer.material, index, averageNormal, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]], uvs[triangles[i]], uvs[triangles[i + 1]], uvs[triangles[i + 2]]);
                index++;
            }

            // destroy original
            Destroy(target.gameObject);
        }

        void CreateMeshPiece(bool useGravity, float mass, Vector3 velocity, Vector3 angularVelocity, float extrudeSize, Quaternion rot, Vector3 pos, Material mat, int index, Vector3 faceNormal, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            // TODO add object pooling to get rid of addcomponent slowness https://docs.unity3d.com/2021.2/Documentation/ScriptReference/Pool.ObjectPool_1.html

            GameObject go = new GameObject("piece");

            Mesh mesh = go.AddComponent<MeshFilter>().mesh;
            go.AddComponent<MeshRenderer>();

            go.tag = "Explodable"; // set this only if should be able to explode this smaller piece also
            go.GetComponent<Renderer>().material = mat;
            go.transform.position = pos;
            go.transform.rotation = rot;

            Vector3[] vertices = new Vector3[3 * 4];
            int[] triangles = new int[3 * 4];
            Vector2[] uvs = new Vector2[3 * 4];

            // get centroid
            Vector3 v4 = (v1 + v2 + v3) / 3;

            // extend to backwards
            v4 = v4 + (-faceNormal) * extrudeSize;

            // not shared vertices
            // orig face
            //vertices[0] = (v1);
            vertices[0] = (v1);
            vertices[1] = (v2);
            vertices[2] = (v3);
            // right face
            vertices[3] = (v1);
            vertices[4] = (v2);
            vertices[5] = (v4);
            // left face
            vertices[6] = (v1);
            vertices[7] = (v3);
            vertices[8] = (v4);
            // bottom face
            vertices[9] = (v2);
            vertices[10] = (v3);
            vertices[11] = (v4);

            // orig face
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            //  right face
            triangles[3] = 5;
            triangles[4] = 4;
            triangles[5] = 3;
            //  left face
            triangles[6] = 6;
            triangles[7] = 7;
            triangles[8] = 8;
            //  bottom face
            triangles[9] = 11;
            triangles[10] = 10;
            triangles[11] = 9;

            // orig face
            uvs[0] = uv1;
            uvs[1] = uv2;
            uvs[2] = uv3; // todo
                          // right face
            uvs[3] = uv1;
            uvs[4] = uv2;
            uvs[5] = uv3; // todo

            // left face
            uvs[6] = uv1;
            uvs[7] = uv3;
            uvs[8] = uv3;   // todo
                            // bottom face (mirror?) or custom color? or fixed from uv?
            uvs[9] = uv1;
            uvs[10] = uv2;
            uvs[11] = uv1; // todo

            //mesh.vertices = vertices;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            // TODO set normals manually?
            mesh.RecalculateNormals();
            mesh.GetNormals(normals);

            mesh.tangents = CalculateMeshTangents(vertices, uvs, normals, triangles);

            var rb = go.AddComponent<Rigidbody>();
            if (inheritRbProperties == true)
            {
                rb.velocity = velocity * 0.5f;
                rb.angularVelocity = angularVelocity * 0.5f;
                rb.mass = mass;
                rb.useGravity = useGravity;
            }
            MeshCollider mc = go.AddComponent<MeshCollider>();

            mc.sharedMesh = mesh;
            mc.convex = true;

            go.AddComponent<MeshFader>();
        }

        // source: http://answers.unity3d.com/questions/7789/calculating-tangents-vector4.html
        Vector4[] CalculateMeshTangents(Vector3[] vertices, Vector2[] uvs, List<Vector3> normals, int[] triangles)
        {
            //speed up math by copying the mesh arrays
            //int[] triangles = mesh.triangles;
            //Vector3[] vertices = mesh.vertices;
            //Vector2[] uv = mesh.uv;
            //Vector3[] normals = mesh.normals;

            //variable definitions
            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];
            Vector4[] tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = triangles[a + 0];
                long i2 = triangles[a + 1];
                long i3 = triangles[a + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uvs[i1];
                Vector2 w2 = uvs[i2];
                Vector2 w3 = uvs[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f / (s1 * t2 - s2 * t1);

                sdir.x = (t2 * x1 - t1 * x2) * r;
                sdir.y = (t2 * y1 - t1 * y2) * r;
                sdir.z = (t2 * z1 - t1 * z2) * r;

                tdir.x = (s1 * x2 - s2 * x1) * r;
                tdir.y = (s1 * y2 - s2 * y1) * r;
                tdir.z = (s1 * z2 - s2 * z1) * r;

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            for (int a = 0; a < vertexCount; ++a)
            {
                Vector3 n = normals[a];
                Vector3 t = tan1[a];
                Vector3.OrthoNormalize(ref n, ref t);
                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;
                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }
            return tangents;
        }
    }
}
