using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[UnityEditor.AssetImporters.ScriptedImporter(1, "vox")]
public class VoxProcessor : UnityEditor.AssetImporters.ScriptedImporter
{
    public override async void OnImportAsset(AssetImportContext ctx)
    {
        // Figure out where to save generate assets, and what their filenames should be
        string filepath = Application.dataPath.Replace("Assets", "");
        filepath += ctx.assetPath;

        string filenameOfVoxFile = Path.GetFileNameWithoutExtension(filepath);
        string assetDirectory = Path.GetDirectoryName(filepath);

        // Get the name of the folder containing the .vox file - if the file is already in a folder with the same name, it's been processed already and we can don't have to create a new subdirectory for organization
        string nameOfFolder = Path.GetFileName(assetDirectory);
        // SaveAsPrefabAsset needs the path from the root of the project folder.
        string projectPath = Application.dataPath.Replace("/Assets", "").Replace("/","\\"); 

        // The final normal path for unity stuff
        string cutOffPath = assetDirectory.Replace(projectPath + "\\", "");


        // If it's not already in a directory of the same name, create one for organization
        //if(!(nameOfFolder == filenameOfVoxFile))
        //{
        //    assetDirectory += Path.DirectorySeparatorChar + filenameOfVoxFile;
        //}
        //Directory.CreateDirectory(assetDirectory);

        string prefabPathWithoutExtension = Path.Combine(cutOffPath, $"{filenameOfVoxFile}Prefab");
        string prefabPath = prefabPathWithoutExtension + ".prefab";

        GameObject existingPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

        bool prefabAlreadyExists = existingPrefab != null;

        // Note - if there's only one chunk, gameObject will contain the one and only chunk,
        //as opposed to being the parent gameobject for a bunch of chunks.
        GameObject containerObject = null;

        // If it already exists, use the existing, if not just leave it as an empty new gameobject
        if(prefabAlreadyExists)
        {
            containerObject = PrefabUtility.LoadPrefabContents(prefabPath);
            //(GameObject)PrefabUtility.InstantiatePrefab(existingPrefab);
        } 
        else
        {
            containerObject = new GameObject();
        }
        
        string meshesDirectory = Path.Combine(cutOffPath, "GeneratedMeshes");
        ConvertMagicaVoxel.Convert(containerObject, ctx.assetPath, meshesDirectory, prefabAlreadyExists);

        PrefabUtility.SaveAsPrefabAsset(containerObject, prefabPath);
        AssetDatabase.SaveAssets();

        if(prefabAlreadyExists)
        {
            PrefabUtility.UnloadPrefabContents(containerObject);
        }

        // Destroy so it's not left in the scene
        DestroyImmediate(containerObject);
    }
}
