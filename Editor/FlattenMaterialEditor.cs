using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEditor.Formats.Fbx.Exporter;

namespace UnityEditor.FlattenMaterial
{
    public class FlattenMaterialEditor : EditorWindow
    {
        string inputFolder = "";
        string outputFolder = "";
        int shaderIndex;
        string[] shaderOptions;
        string materialName = "Default";
        string textureName = "_combinedTexture";
        bool showOptions = true;
        float uvPadding = 0;
        int textureSizeIndex = 0;
        int[] textureSizes = new int[0];
        int[] totalTextureSizes = new int[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048 };
        Texture2D combinedTexture;
        GameObject[] gameObjects = new GameObject[0];
        Material[] materials = new Material[0];

        int power = 1;

        private void OnEnable()
        {
            shaderOptions = ShaderUtil.GetAllShaderInfo().Select(x => x.name).ToArray();
            shaderIndex = Array.IndexOf(shaderOptions, "Standard");
        }

        [MenuItem("Tools/Flatten Materials")]
        public static void ShowWindow()
        {
            var window = GetWindow<FlattenMaterialEditor>();
            window.titleContent = new GUIContent("Flatten Material Editor");
            window.Show();
        }
        private void OnGUI()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 100;

            if (SelectFolder("Input Folder", ref inputFolder))
                GetObjects(inputFolder);

            EditorGUILayout.HelpBox(CanExport() ? $"{gameObjects.Length} GameObjects with {materials.Length} unique Materials" : "Select a folder with one or more FBX files", MessageType.Info);
            if (CanExport())
            {
                EditorGUILayout.Space();
                showOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showOptions, "Options");
                if (showOptions)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.LabelField("Texture");
                    EditorGUI.indentLevel++;
                    textureName = EditorGUILayout.TextField("Name", textureName);
                    textureSizeIndex = EditorGUILayout.Popup("Size", textureSizeIndex, textureSizes.Select(x => x.ToString()).ToArray());
                    uvPadding = EditorGUILayout.Slider("Padding", uvPadding, 0, 0.5f);
                    EditorGUI.indentLevel--;

                    EditorGUILayout.LabelField("Material");
                    EditorGUI.indentLevel++;
                    materialName = EditorGUILayout.TextField("Name", materialName);
                    shaderIndex = EditorGUILayout.Popup("Shader", shaderIndex, shaderOptions);
                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space();

                if (GUILayout.Button("Convert Objects"))
                {
                    outputFolder = EditorUtility.OpenFolderPanel("Output Folder", "", "");
                    if (!string.IsNullOrEmpty(outputFolder))
                    {
                        if (outputFolder != inputFolder)
                            Export(outputFolder);
                    }
                }
                if (outputFolder == inputFolder)
                    EditorGUILayout.HelpBox("You must choose a seperate output folder", MessageType.Error);
            }
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Tool by @willenders_", EditorStyles.centeredGreyMiniLabel);

            bool SelectFolder(string label, ref string path)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.TextField(label, path.Length > 0 ? path.Remove(0, Application.dataPath.Length - 6) : "", EditorStyles.textArea);
                GUI.enabled = true;
                EditorGUIUtility.SetIconSize(Vector2.one * 16);
                if (GUILayout.Button(EditorGUIUtility.IconContent("FolderOpened Icon"), GUILayout.Width(30)))
                {
                    path = EditorUtility.OpenFolderPanel(label, "", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        EditorGUILayout.EndHorizontal();
                        return true;
                    }
                }
                EditorGUIUtility.SetIconSize(Vector2.zero);
                EditorGUILayout.EndHorizontal();
                return false;
            }



        }

        void Export(string outputFolder)
        {

            outputFolder = outputFolder.Remove(0, Application.dataPath.Length - 6);

            // Create Combined Texture from Materials
            combinedTexture = materials.CombineToTexture(power, textureSizes[textureSizeIndex], textureName, outputFolder);

            //Create Combined Material from Combined Texture
            Material combinedMaterial = new Material(Shader.Find(shaderOptions[shaderIndex]));

            if (!string.IsNullOrEmpty(materialName))
                combinedMaterial.name = materialName;

            combinedMaterial.mainTexture = combinedTexture;

            foreach (GameObject gameObject in gameObjects)
                ExportGameObject(gameObject, combinedMaterial, outputFolder);
        }

        void GetObjects(string folderPath)
        {
            gameObjects = Utilities.GatherGameObjects(folderPath);
            materials = Utilities.GatherMaterials(gameObjects);

            power = 1;
            while (power * 2 < materials.Length)
                power *= 2;

            textureSizes = totalTextureSizes.Where(x => x >= power).ToArray();
        }

        void ExportGameObject(GameObject gameObject, Material combinedMaterial, string outputFolder)
        {
            outputFolder = outputFolder + "/" + gameObject.name + ".fbx";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(gameObject), outputFolder);
            gameObject = AssetDatabase.LoadMainAssetAtPath(outputFolder) as GameObject;
            float multiplier = 1f / power;

            FlattenObject(gameObject);

            foreach (Transform child in gameObject.transform)
                FlattenObject(child.gameObject);

            ModelExporter.ExportObject(outputFolder, gameObject);

            void FlattenObject(GameObject gameObject)
            {
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                Mesh mesh = Utilities.UniqueVertices(meshFilter.sharedMesh);
                Vector2[] uvs = mesh.uv;

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int materialIndex = Array.FindIndex(materials, x => x.name == meshRenderer.sharedMaterials[i].name);
                    Vector2 paddingSize = Vector2.one * uvPadding * multiplier;
                    Vector2 uvMin = new Vector2(materialIndex % power, materialIndex / power);
                    Vector2 uvMax = uvMin + Vector2.one;
                    uvMin *= multiplier;
                    uvMax *= multiplier;
                    uvMin += paddingSize;
                    uvMax -= paddingSize;


                    int[] subMeshTris = new HashSet<int>(mesh.GetTriangles(i)).ToArray();
                    Vector2[] subMeshUVs = uvs.Where((uv, index) => subMeshTris.Contains(index)).ToArray().Normalize(uvMin, uvMax);
                    for (int j = 0; j < subMeshTris.Length; j++)
                    {
                        uvs[subMeshTris[j]] = subMeshUVs[j];
                    }



                }

                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(mesh.triangles, 0);
                mesh.subMeshCount = 1;

                meshRenderer.sharedMaterials = new Material[] { combinedMaterial };
            }
        }
        bool CanExport()
        {
            return gameObjects.Length > 0 && materials.Length > 0 && materials.Length < 256;
        }
    }
}
