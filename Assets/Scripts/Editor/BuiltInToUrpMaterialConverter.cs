using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    public static class BuiltInToUrpMaterialConverter
    {
        private const string UrpConvertMenuPath = "Edit/Rendering/Materials/Convert Selected Built-in Materials to URP";
        private const string LogPrefix = "[URP]";

        private static readonly string[] SearchFolders = { "Assets" };

        private static readonly string[] ConvertibleShaderNames =
        {
            "Standard",
            "Standard (Specular setup)",
            "Standard (Roughness setup)",
            "Autodesk Interactive"
        };

        private static readonly string[] ConvertibleShaderPrefixes =
        {
            "Legacy Shaders/",
            "Mobile/",
            "Nature/",
            "Particles/"
        };

        [MenuItem("Tools/Zombie War/Convert Built-in Materials To URP")]
        private static void ConvertProjectMaterials()
        {
            List<UnityEngine.Object> convertible = CollectConvertibleMaterials();
            if (convertible.Count == 0)
            {
                Debug.Log($"{LogPrefix} No built-in material left to convert.");
                return;
            }

            UnityEngine.Object[] previousSelection = Selection.objects;
            Selection.objects = convertible.ToArray();
            bool executed = EditorApplication.ExecuteMenuItem(UrpConvertMenuPath);
            Selection.objects = previousSelection;

            if (!executed)
            {
                Debug.LogError($"{LogPrefix} Menu item not found: {UrpConvertMenuPath}");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Converted {convertible.Count} built-in materials to URP.");
        }

        private static List<UnityEngine.Object> CollectConvertibleMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", SearchFolders);
            List<UnityEngine.Object> materials = new List<UnityEngine.Object>(guids.Length);
            foreach (string guid in guids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader == null)
                {
                    continue;
                }

                if (IsConvertible(material.shader.name))
                {
                    materials.Add(material);
                }
            }

            return materials;
        }

        private static bool IsConvertible(string shaderName)
        {
            foreach (string convertibleName in ConvertibleShaderNames)
            {
                if (string.Equals(shaderName, convertibleName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (string prefix in ConvertibleShaderPrefixes)
            {
                if (shaderName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
