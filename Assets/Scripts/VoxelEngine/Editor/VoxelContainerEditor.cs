using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using Object = UnityEngine.Object;

[CustomEditor(typeof(VoxelContainer))]
[CanEditMultipleObjects]
public class VoxelContainerEditor : Editor
{
    private SerializedProperty _materials;
    bool expanded;

    protected void OnEnable()
    {
        // Fetch the objects from the GameObject script to display in the inspector
        _materials = serializedObject.FindProperty("materials");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector(); 

        EditorGUILayout.Separator();
        // R_EditorGUIUtility.Separator(4);

        GUILayout.Label("Materials");
        GUILayout.Label("List Of Materials");
        // R_EditorGUIUtility.ShurikenFoldoutHeader("List Of Materials", TextTitleStyle, ref expanded);

        EditorGUI.BeginChangeCheck();
        
        if(expanded)
        {
            // _materials.arraySize = EditorGUILayout.IntField("Size"s, _materials.arraySize);
            for (int i = 0; i < _materials.arraySize; i++)
            {

                SerializedProperty materialDataProperty = _materials.GetArrayElementAtIndex(i);
                SerializedProperty materialId = materialDataProperty.FindPropertyRelative("materialId");
                SerializedProperty advancedMaterialId = materialDataProperty.FindPropertyRelative("advancedMaterialDataId");

                if(materialId.intValue != 0)
                {
                    EditorGUILayout.Separator();
					// R_EditorGUIUtility.Separator(4);

                    // Plan - make this big and expandable. The AdvancedMaterialId should be summarized, and there should be a little dropdown thing like an enum field almost showing all of the available ones - starting with the item represented, or if that's null the other item fields (scraps, pieces)

                    MaterialsDatabase materialsDatabase = ((VoxelContainer)(target)).materialsDatabase;
                    VoxelMaterial materialHere = materialsDatabase.GetMaterialById((byte)materialId.intValue);
                    AdvancedMaterialData advancedMaterialDataHere = new AdvancedMaterialData();

                    bool amdExists = false;

                    if(materialHere.advancedMaterialData.Length != 0 && materialHere.advancedMaterialData.Length > advancedMaterialId.intValue)
                    {
                        amdExists = true;
                        advancedMaterialDataHere = materialHere.advancedMaterialData[advancedMaterialId.intValue];
                    }

                    string itemNameOfRepresentedItem = "";

                    if(amdExists)
                    {
                        if(advancedMaterialDataHere.hasScrapsAndPieces)
                        {
                            itemNameOfRepresentedItem = advancedMaterialDataHere.scrapItem.name + ", " + advancedMaterialDataHere.pieceItem.name;
                        }
                        else if(advancedMaterialDataHere.itemRepresented != null)
                        {
                            itemNameOfRepresentedItem = advancedMaterialDataHere.itemRepresented.name;
                        }

                        if(String.IsNullOrEmpty(itemNameOfRepresentedItem))
                        {
                            itemNameOfRepresentedItem = "No items represented.";
                        }
                    }
                    
                    GUILayout.Label(materialHere.name);
                    // R_EditorGUIUtility.ShurikenHeader(materialHere.name, TextSectionStyle, 20);

                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(materialId, true);
                    GUI.enabled = true;

                    EditorGUILayout.PropertyField(advancedMaterialId, true);

                    if(!amdExists)
                    {
                        EditorGUILayout.HelpBox("This advanced material data does not exist.", MessageType.Warning);

                        if(GUILayout.Button("Fix"))
                        {
                            for(int j = 0; j < materialsDatabase.materials.Count; j++)
                            {
                                VoxelMaterial mat = materialsDatabase.materials[j];
                                
                                if(mat.id == (byte)materialId.intValue)
                                {
                                    
                                    List<AdvancedMaterialData> data = mat.advancedMaterialData.ToList();
                                    int indexToInsert = advancedMaterialId.intValue;

                                    int numberToAdd = data.Count + indexToInsert;
                                    for(int k = data.Count; k < numberToAdd; k++)
                                    {
                                        AdvancedMaterialData newAdvancedMaterialData = new AdvancedMaterialData();
                                        newAdvancedMaterialData.id = (byte)k;
                                        data.Add(newAdvancedMaterialData);
                                    }

                                    mat.advancedMaterialData = data.ToArray();
                                    materialsDatabase.materials[j] = mat;
                                    throw new NotImplementedException("Implemented, but not correctly...");
                      
                                }
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Item Represented: " + itemNameOfRepresentedItem);
                    }
                    EditorGUILayout.Separator();
					// R_EditorGUIUtility.Separator(4);
                }

    
            }
        }

        EditorGUILayout.Separator();
        // R_EditorGUIUtility.Separator(4);
        
        if(EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        // materialDataProperty.isExpanded = EditorGUILayout.Foldout(materialDataProperty.isExpanded, "Element " + i);
        // if(materialDataProperty.isExpanded)
        // {
        // }

        // EditorGUI.BeginChangeCheck();
        // blah
        // if (EditorGUI.EndChangeCheck())
        // {
        // }

        if(GUILayout.Button("Update Render"))
        {
            ((VoxelContainer)(target)).UpdateRender(generateCollider: false, sync: true);
        }

        if(GUILayout.Button("Save Voxel Container to Assets"))
        {
            SaveVc();
        }
        if(GUILayout.Button("Prune"))
        {
            if(EditorUtility.DisplayDialog("Prune VoxelContainer", "Are you sure? This will delete voxels in the air.", "Yes", "No"))
            {
                PruneVc();
            }
        }

        if(GUILayout.Button("Cleanup VoxelContainer"))
        {
            if(EditorUtility.DisplayDialog("Cleanup VoxelContainer", "Are you sure?", "Yes", "No"))
            {
                VoxelContainer vc = (VoxelContainer)target;
                vc.Initialize(vc.Dimensions);
                vc.Cleanup();
            }

        }

        if(GUILayout.Button("Setup References"))
        {
            Object[] vcs = (Object[])targets;
            foreach(Object v in vcs)
            {
                ((VoxelContainer)v).SetupReferences();
            }
        }
        if(GUILayout.Button("Optimize Bounds"))
        {
            Object[] vcs = (Object[])targets;
            foreach(Object v in vcs)
            {
                if(Application.isPlaying)
                {
                    ((VoxelContainer)v).meshFilter.mesh.RecalculateBounds();
                }
                else
                {
                    ((VoxelContainer)v).meshFilter.sharedMesh.RecalculateBounds();
                }

                ((VoxelContainer)v).OptimizeBounds();
            }
        }
        if(GUILayout.Button("Test LZ4"))
        {
            VoxelContainer vc = (VoxelContainer)target;
            vc.TestLZ4();
        }
        if(GUILayout.Button("Compress Voxels"))
        {
            Object[] vcs = (Object[])targets;
            foreach(Object v in vcs)
            {
                ((VoxelContainer)v).CompressVoxels();
            }
            AssetDatabase.SaveAssets();
        }
        if(GUILayout.Button("Decompress Voxels"))
        {
            VoxelContainer vc = (VoxelContainer)target;
            vc.DecompressVoxels();
            AssetDatabase.SaveAssets();
        }
    }

    private void PruneVc()
    {
        VoxelContainer vc = (VoxelContainer)target;

        List<System.Tuple<Vector3Int[], List<Color32>, List<MaterialData>>> pieces = vc.GetPiecesOfObject();
        pieces = pieces.OrderByDescending(x => x.Item1.Length).ToList();

        Vector3Int[] piece = pieces[0].Item1;
        List<Color32> colorsForPiece = pieces[0].Item2;
        List<MaterialData> materialsForPiece = pieces[0].Item3;
        
        vc.ApplyPieceToVc(vc, piece, colorsForPiece, materialsForPiece);
        vc.UpdateRender();
        AssetDatabase.SaveAssets(); 

    }

    private void SaveVc()
    {
        VoxelContainer vc = (VoxelContainer)target;
        vc.UpdateRender();
        
        // Commented this out, so now the method assumes that there's already an asset representing the object, and this just updates it.

        //AssetDatabase.CreateAsset(vc.gameObject.GetComponent<MeshFilter>().sharedMesh, "Assets/GeneratedVoxelContainerMesh.asset");
        //PrefabUtility.SaveAsPrefabAsset(vc.gameObject, "Assets/GeneratedVoxelContainer.prefab");
        AssetDatabase.SaveAssets(); 
    }
}
