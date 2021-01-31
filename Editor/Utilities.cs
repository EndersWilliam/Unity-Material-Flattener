using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityEditor.FlattenMaterial
{
    public static class Utilities
    {

        public static GameObject[] GatherGameObjects(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);
            List<GameObject> gameObjects = new List<GameObject>();

            foreach (string file in files)
            {
                if (file.EndsWith(".fbx"))
                {
                    string fileSubPath = file.Remove(0, Application.dataPath.Length - 6);
                    gameObjects.Add(AssetDatabase.LoadMainAssetAtPath(fileSubPath) as GameObject);
                }
            }
            return gameObjects.ToArray();
        }
        public static Material[] GatherMaterials(GameObject[] gameObjects)
        {
            List<Material> materials = new List<Material>();
            foreach (GameObject gameObject in gameObjects)
            {
                MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenderers)
                    foreach (Material material in meshRenderer.sharedMaterials)
                        if (!materials.Exists((x) => x.name == material.name))
                            materials.Add(material);

            }
            return materials.ToArray();
        }
        public static Vector2[] Normalize(this Vector2[] uv, Vector2 outMin, Vector2 outMax)
        {
            Vector2 min = Vector2.zero, max = Vector2.one;
            foreach (Vector2 point in uv)
            {
                min.Set(point.x < min.x ? point.x : min.x, point.y < min.y ? point.y : min.y);
                max.Set(point.x > max.x ? point.x : max.x, point.y > max.y ? point.y : max.y);
            }
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] = RemapVector2(uv[i], min, max);
            }
            return uv;

            Vector2 RemapVector2(Vector2 value, Vector2 min, Vector2 max)
            {
                value.x = outMin.x + (value.x - min.x) * (outMax.x - outMin.x) / (max.x - min.x);
                value.y = outMin.y + (value.y - min.y) * (outMax.y - outMin.y) / (max.y - min.y);
                return value;
            }
        }

        public static Texture2D CombineToTexture(this Material[] materials, int power, int scale, string fileName, string outputPath)
        {

            Texture2D texture = new Texture2D(power, power, TextureFormat.ARGB32, true);

            for (int i = 0; i < materials.Length; i++)
                texture.SetPixel(i % power, i / power, materials[i].color);

            texture.Apply();

            TextureScale.Point(texture, scale, scale);

            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(outputPath + $"/{fileName}.png", bytes);
            AssetDatabase.Refresh();
            texture = AssetDatabase.LoadAssetAtPath(outputPath + $"/{fileName}.png", typeof(Texture2D)) as Texture2D;

            return texture;
        }

        public static Mesh UniqueVertices(Mesh mesh)
        {
            int subMeshCnt = mesh.subMeshCount;
            int triCnt = mesh.triangles.Length;
            Vector3[] sourceVerts = mesh.vertices;
            Vector3[] sourceNorms = mesh.normals;
            Vector2[] sourceUVs = mesh.uv;

            Vector3[] newVertices = new Vector3[triCnt];
            Vector3[] newNorms = new Vector3[triCnt];
            Vector2[] newUVs = new Vector2[triCnt];

            int offsetVal = 0;

            for (int k = 0; k < subMeshCnt; k++)
            {
                int[] sourceIndices = mesh.GetTriangles(k);
                int[] newIndices = new int[sourceIndices.Length];

                // Create a unique vertex for every index in the original Mesh:
                for (int i = 0; i < sourceIndices.Length; i++)
                {
                    int newIndex = sourceIndices[i];
                    int iOffset = i + offsetVal;
                    newIndices[i] = iOffset;
                    newVertices[iOffset] = sourceVerts[newIndex];
                    newNorms[iOffset] = sourceNorms[newIndex];
                    newUVs[iOffset] = sourceUVs[newIndex];
                }
                offsetVal += sourceIndices.Length;

                mesh.vertices = newVertices;
                mesh.normals = newNorms;
                mesh.uv = newUVs;

                mesh.SetTriangles(newIndices, k);
            }
            mesh.RecalculateBounds();
            mesh.Optimize();
            return mesh;
        }
    }
}