#if UNITY_EDITOR

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Threading.Tasks;
using System.Linq;

/*  
    Here is the specification: https://github.com/ephtracy/voxel-model/blob/master/MagicaVoxel-file-format-vox.txt
    and here is some more information, which is more useful https://github.com/ephtracy/voxel-model/blob/master/MagicaVoxel-file-format-vox-extension.txt

    I also referenced this C++ implementation a lot https://github.com/jpaver/opengametools/blob/master/src/ogt_vox.h

    This script doesn't do rotation at the object level.
*/
public static class ConvertMagicaVoxel
{
    static readonly float Resolution = 0.1f;
    static readonly byte GlassAlphaValue = 30;
    static readonly byte dirtMaterialId = 1;
    static readonly byte rockMaterialId = 2;
    static readonly byte woodMaterialId = 3;
    static readonly byte grassyMaterialId = 4;
    static readonly byte glassMaterialId = 5;
    
    /// <summary>
    /// Convert a GameObject into a voxel representation of a MagicaVoxel file
    /// </summary>
    /// <param name="obj">The GameObject that will be used.</param>
    /// <param name="filePath">The path of the MagicaVoxel file.</param>
    /// <param name="saveMeshesPath">The path that the individual generated meshes will be saved at.</param>
    /// <param name="prefabAlreadyExists">Whether or not the prefab already exists.</param>
    /// <returns>Task</returns>
    public static void Convert(GameObject obj, string filePath, string saveMeshesPath, bool prefabAlreadyExists)
    {
        Directory.CreateDirectory(saveMeshesPath);
        // Read the magicavoxel file into the GameObject
        using (BinaryReader stream = new BinaryReader(new FileStream(filePath, FileMode.Open)))
        {
            FromMagica(stream, obj, prefabAlreadyExists);
        }
        
        // Save the new prefab's meshes and stuff properly
        int i = 0;
        // Process the generated voxelcontainers, save their meshes and whatnot.
        foreach(VoxelContainer vc in obj.GetComponentsInChildren<VoxelContainer>())
        {
            vc.UpdateRender(true, true);

            string assetPath = Path.Combine(saveMeshesPath, $"GeneratedMesh{i}.asset");

            // To avoid broken reference, copy mesh to old mesh if it exists

            Mesh newMesh = vc.gameObject.GetComponent<MeshFilter>().sharedMesh;
            Mesh meshToSave =  AssetDatabase.LoadMainAssetAtPath(assetPath) as Mesh;

            if (meshToSave != null) {
                EditorUtility.CopySerialized (newMesh, meshToSave);
                AssetDatabase.SaveAssets();
            }
            else {
                meshToSave = new Mesh();
                EditorUtility.CopySerialized (newMesh, meshToSave);
                AssetDatabase.CreateAsset (meshToSave, assetPath);
            }

            vc.gameObject.GetComponent<MeshFilter>().sharedMesh = meshToSave;

            // Have to do this twice for some reason
            vc.UpdateRender(true, true);

            AssetDatabase.SaveAssets();

            i++;
        }
    }
 
    public struct Chunk
    {
        public VoxelData[] voxelData;
        public Vector3Int dimensions;
        public TransformNode transformNode;
    }

    public struct TransformNode
    {
        public Vector3 position;
        public Quaternion rotation;
        public int childNodeId;
    }

    public struct VoxelData
    {
        public Vector3Int position;
        public byte color;
    
        public VoxelData(BinaryReader stream)
        {
            int x = stream.ReadByte();
            int y = stream.ReadByte();
            int z = stream.ReadByte();

            // X is vertical in magicavoxel, but y is vertical in Unity
            position = ProcessCoordinate(x, y, z);
            color = stream.ReadByte();
        }
    }
    
    /// <summary>
    /// Feed a voxel container with data parsed from a MagicaVoxel .vox file
    /// </summary>
    /// <param name="stream">An open BinaryReader stream that is the .vox file.</param>
    /// <returns>The voxel chunk data for the MagicaVoxel .vox file.</returns>

    private struct ColorMaterialData
    {
        public Color32 color;
        public MaterialData material;
    }

    public static void FromMagica(BinaryReader stream, GameObject parentSceneObject, bool prefabAlreadyExists)
    {
        ColorMaterialData[] data = new ColorMaterialData[256];

        string magic = new string(stream.ReadChars(4));

        // VERSION
        stream.ReadInt32();

        if (!(magic == "VOX "))
        {
            throw new InvalidOperationException("Not a valid .vox file.");
        }

        int chunkIndex = 0;
        List<Chunk> chunks = new List<Chunk>();
        List<Chunk> outputChunks = new List<Chunk>();

        int transformIndex = 0;
        List<TransformNode> transforms = new List<TransformNode>();

        while (stream.BaseStream.Position < stream.BaseStream.Length)
        {
            // each chunk has an ID, size and child chunks
            // Fields common to every chunk

            char[] chunkId = stream.ReadChars(4);
            string chunkName = new string(chunkId);
            int chunkSize = stream.ReadInt32();

            // chunkChildSize
            stream.ReadInt32();

            switch (chunkName)
            {
                case "SIZE":
                    ReadSizeChunk(stream, chunks, chunkSize);
                    break;
                case "XYZI":
                    ReadVoxelDataChunk(stream, chunks, chunkIndex);
                    chunkIndex += 1;
                    break;
                case "RGBA":
                    data = new ColorMaterialData[256];

                    for (int i = 0; i < 256; i++)
                    {
                        byte r = stream.ReadByte();
                        byte g = stream.ReadByte();
                        byte b = stream.ReadByte();
                        byte a = stream.ReadByte();

                        data[i].color = new Color32(r, g, b, a);
                        data[i].material = FigureOutMaterial(i);
                    }

                    break;
                case "nTRN":
                    ReadTransformNodeChunk(stream, transforms);
                    break;
                case "nGRP":
                    int nodeId = stream.ReadInt32();

                    Dictionary<string, string> nodeAttributes = ReadDictionary(stream);

                    int numberOfChildrenNodes = stream.ReadInt32();

                    for(int i = 0; i < numberOfChildrenNodes; i++)
                    {
                        int childNodeId = stream.ReadInt32();
                    }
                    
                    break;
                case "nSHP":
                    ReadShapeNodeChunk(stream, chunks, outputChunks, transforms);
                    transformIndex++;
                    break;
                default:
                    // Read leftover bytes            
                    stream.ReadBytes(chunkSize);   
                    break;            
            }
        }
        ApplyGenerateVoxelData(outputChunks, parentSceneObject, data, prefabAlreadyExists);
    }

    private static MaterialData FigureOutMaterial(int index)
    {
        // MagicaVoxel starts the index at one so this is more clear
        index += 1;
        MaterialData m = new MaterialData();

        if(index >= 1 && index <= 16)
        {
            // Dirt
            m.materialId = dirtMaterialId;
        }
        else if(index >= 17 && index <= 32)
        {
            // Rock
            m.materialId = rockMaterialId;
        }
        else if(index >= 33 && index <= 48)
        {
            // Wood
            m.materialId = woodMaterialId;
        }
        else if(index >= 49 && index <= 64)
        {
            // Grassy
            m.materialId = grassyMaterialId;
        }
        else if(index >= 65 && index <= 80)
        {
            // Glass
            m.materialId = glassMaterialId;
        }

        return m;
    }

    private static void ApplyGenerateVoxelData(List<Chunk> chunks, GameObject parentSceneObject, ColorMaterialData[] colorData, bool prefabAlreadyExists)
    {
        List<VoxelContainer> vcsToUse = SetupVoxelContainers(parentSceneObject, chunks);

        if(vcsToUse.Count != chunks.Count) throw new InvalidOperationException($"vcsToUse is of length ${vcsToUse.Count} but chunks is of length ${chunks.Count}");

        for(int i = 0; i < vcsToUse.Count; i++)
        {
            ApplyVoxelDataToVoxelContainer(vcsToUse[i], chunks[i], colorData);
        }
    }

    // Called on existing prefabs, to make sure the number of indexed VoxelContainers matches the number of objects in MagicaVoxel.
    // This could directly return the VoxelContainers to be used.
    static List<VoxelContainer> SetupVoxelContainers(GameObject parentSceneObject, List<Chunk> chunks)
    {
        // An indexOfObject of less than one means it's not counted - this way we can have other VCs in an imported object
        //that don't get messed up
        List<VoxelContainer> validVcs = parentSceneObject.GetComponentsInChildren<VoxelContainer>()
                                                            .Where(x => x.indexInObject >= 0).ToList();

        validVcs = validVcs.OrderBy(x => x.indexInObject).ToList();

        for(int i = 0; i < validVcs.Count; i++)
        {
            // give them all an index, but allow one extra spot for each for GLASS/TRANSPARENT vcs
            validVcs[i].indexInObject = i + 1;
        }

        // NOTE - change this, cleanup is still necessary if moving from child object to parent object
        if(validVcs.Count == chunks.Count)
        {
            return validVcs;
        }

        if(chunks.Count == 1)
        {
            if(validVcs.Any(x => x.indexInObject == 0) == false)
            {
                GameObject newObj = new GameObject("GeneratedVoxelContainer");
                newObj.transform.parent = parentSceneObject.transform;

                VoxelContainer vc = newObj.AddComponent<VoxelContainer>();
                vc.indexInObject = 0;

                validVcs.Add(vc);

                return validVcs;
            }

            // We don't want any child objects in this situation. Destroy all but the one with index zero.
            for (int i = validVcs.Count - 1; i >= 0; i--)
            {                
                if(validVcs[i].indexInObject != 0)
                {
                    GameObject.DestroyImmediate(validVcs[i].gameObject);
                    validVcs.RemoveAt(i);
                }
            }

            return validVcs.OrderBy(x => x.indexInObject).ToList();;
        }

        if(chunks.Count > 1)
        {
            // Verify that all voxelcontainers are not on the parent scene object.
            parentSceneObject.transform.position = Vector3.zero;
            VoxelContainer oldVc = parentSceneObject.GetComponent<VoxelContainer>();
            if(oldVc != null)
            {
                int vcIndex = oldVc.indexInObject;

                if(vcIndex >= 0)
                {
                    validVcs.Remove(oldVc);
                    CleanupVcAndReferences(oldVc);

                    GameObject newObj = new GameObject("GeneratedVoxelContainer");
                    VoxelContainer newVc = newObj.AddComponent<VoxelContainer>();
                    newVc.indexInObject =vcIndex;
                }
            }
        }

        if (validVcs.Count > chunks.Count)
        {
            Debug.Log("Destroying VCs");
            // Time to destroy some VCs, there's too many.
            for (int i = validVcs.Count - 1; i >= 0; i--)
            {
                if(validVcs[i].indexInObject > chunks.Count)
                {
                    UnityEngine.GameObject.DestroyImmediate(validVcs[i].gameObject);
                    validVcs.RemoveAt(i);
                } 
            }

            return validVcs.OrderBy(x => x.indexInObject).ToList();
        }
        else if (validVcs.Count < chunks.Count)
        {
            Debug.Log("Creating VCs.");
            // Time to create some VCs.
            int amountOfNewChildGameObjectsToCreate = chunks.Count - validVcs.Count;

            for(int i = 0; i < amountOfNewChildGameObjectsToCreate; i++)
            {
                GameObject newObj = new GameObject("VoxelContainer");
                newObj.transform.parent = parentSceneObject.transform;

                VoxelContainer vc = newObj.AddComponent<VoxelContainer>();
                vc.indexInObject = validVcs.Count + i;
                validVcs.Add(vc);
            }
            return validVcs.OrderBy(x => x.indexInObject).ToList();
        }

        throw new InvalidOperationException();
    }

    static void CleanupVcAndReferences(VoxelContainer vc)
    {
        GameObject obj = vc.gameObject;
        GameObject.DestroyImmediate(vc);
        obj.gameObject.DestroyIfExists<MeshCollider>();
        obj.gameObject.DestroyIfExists<NonConvexMeshCollider>();
        obj.gameObject.DestroyIfExists<MeshRenderer>();
        obj.gameObject.DestroyIfExists<MeshFilter>();
    }

    static void ApplyVoxelDataToVoxelContainer(VoxelContainer vc, Chunk chunk, ColorMaterialData[] colorData)
    {
        vc.gameObject.transform.position = chunk.transformNode.position;
        vc.gameObject.transform.rotation = chunk.transformNode.rotation;
        
        vc.SetupReferences();
        vc.Initialize(chunk.dimensions);
        
        vc.gameObject.GetComponent<MeshRenderer>().sharedMaterial = vc.genericVoxelsMaterial;
        
        VoxelContainer glassVoxelVc = null;

        for (int i = 0; i < chunk.voxelData.Length; i++)
        {
            Vector3Int voxelPos = chunk.voxelData[i].position;

            Color32 color = colorData[chunk.voxelData[i].color -1].color;
            MaterialData material = colorData[chunk.voxelData[i].color -1].material;

            // Currently hardcoding the glass alpha
            if(material.materialId == glassMaterialId)
            {
                color.a = GlassAlphaValue;

                if(glassVoxelVc == null)
                {
                    GameObject newObj = new GameObject("TransparentVoxelContainer");
                    newObj.transform.parent = vc.transform;
                    newObj.transform.position = chunk.transformNode.position;
                    newObj.transform.rotation = chunk.transformNode.rotation;

                    glassVoxelVc = newObj.AddComponent<VoxelContainer>();
                    glassVoxelVc.SetupReferences();
                    glassVoxelVc.gameObject.GetComponent<MeshRenderer>().sharedMaterial = glassVoxelVc.transparentMaterial;
                    glassVoxelVc.Initialize(vc.Dimensions);
                    glassVoxelVc.indexInObject = vc.indexInObject + 1;
                    glassVoxelVc.SetVoxel(voxelPos, color, material);
                    newObj.GetComponent<MeshRenderer>().material = glassVoxelVc.transparentMaterial;
                }
                else
                {
                    glassVoxelVc.SetVoxel(voxelPos, color, material);
                }
                continue;
            }
            else
            {
                color.a = 255;
                vc.SetVoxel(voxelPos, color, material);
            }
        }
        }

    #region Chunks
    static void ReadSizeChunk(BinaryReader stream, List<Chunk> chunks, int chunkSize)
    {
        // Setup dimensions of voxel object
        int x = stream.ReadInt32();
        int y = stream.ReadInt32();
        int z = stream.ReadInt32();

        Vector3Int dimensions = ProcessCoordinate(x, y, z);

        Chunk chunk = new Chunk 
        { 
            voxelData = null, 
            dimensions = dimensions
        };
        chunks.Add(chunk);

        stream.ReadBytes(chunkSize - 4 * 3);
    }

    static void ReadVoxelDataChunk(BinaryReader stream, List<Chunk> chunks, int chunkIndex)
    {
        int numVoxels = stream.ReadInt32();

        Chunk chunk = chunks[chunkIndex];
        chunk.voxelData = new VoxelData[numVoxels];

        for (int i = 0; i < chunk.voxelData.Length; i++)
        {
            chunk.voxelData[i] = new VoxelData(stream);
        }

        chunks[chunkIndex] = chunk;
    }

    // Read in the transform chunk, return the data about it and the ID of it's shape node, which has necessary data about it.
    static bool ReadTransformNodeChunk(BinaryReader stream, List<TransformNode> transforms)
    {
        // Node ID
        int nodeId = stream.ReadInt32();

        // node attribues including name and hidden
        Dictionary<string, string> stuff = ReadDictionary(stream);

        // child node id
        int childNodeId = stream.ReadInt32();
        //layer id
        stream.ReadInt32();

        int reservedId = stream.ReadInt32();
        // Should be 1
        int frames = stream.ReadInt32();

        if(!(frames == 1))
            throw new InvalidOperationException("The amount of frames should always be one.");
        
        // This one has the good stuff including translation and rotation
        Dictionary<string, string> frameAttributes = ReadDictionary(stream);

        TransformNode transformNode = new TransformNode();

        transformNode.childNodeId = childNodeId;

        bool hasTransform = false;
                
        if(frameAttributes.TryGetValue("_t", out string translation))
        {
            hasTransform = true;

            string[] parts = translation.Split(' ');
            
            float x = float.Parse(parts[0]);
            float y = float.Parse(parts[1]);
            float z = float.Parse(parts[2]);

            // Will have to set this up later.
            Vector3 chunkPosition = new Vector3(x, z, y) * Resolution;
            transformNode.position = chunkPosition;
        }
        
        transforms.Add(transformNode);

        return hasTransform;
    }

    static Vector3 NegateVector3(Vector3 v) 
    { 
        Vector3 r; 
        r.x = -v.x;  
        r.y = -v.y; 
        r.z = -v.z; 
        return r;
    }

    static void ReadShapeNodeChunk(BinaryReader stream, List<Chunk> chunks, List<Chunk> newChunks, List<TransformNode> transforms)                   
    {
        int nodeId = stream.ReadInt32();

        TransformNode transformForThisChunk = transforms.FirstOrDefault(t => t.childNodeId == nodeId);
        
        Dictionary<string, string> nodeAttributes = ReadDictionary(stream);

        int numberOfModels = stream.ReadInt32();

        if(numberOfModels != 1)
            throw new InvalidOperationException("Number of models must equal one.");

        // foreach model (1), dictionary

        // model id is just the index of the model in the stored order
        int modelId = stream.ReadInt32();
        Dictionary<string, string> modelAttributes = ReadDictionary(stream);

        Chunk chunk = new Chunk();
        chunk = chunks[modelId];
        
        // Calculate offset stuff - might be able to move this somewhere else.
        Vector3 offset = (Vector3)(chunk.dimensions / 2) * Resolution;
        transformForThisChunk.position = transformForThisChunk.position - offset;
        chunk.transformNode = transformForThisChunk; //transforms[index];

        //problem - can't just add because it might add on top of existing chunks. Solution - use a different list.        
        newChunks.Add(chunk);

        /*
        if(index >= chunks.Count)
        {
        }
        else
        {
            chunks[modelId] = chunk;
        }
        */
    }
    #endregion


    #region FileReadingHelpers
    public static string ReadString(BinaryReader stream)
    {
        int numberOfBytes = stream.ReadInt32();

        Byte[] bytes = stream.ReadBytes(numberOfBytes);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }

    public static Dictionary<string, string> ReadDictionary(BinaryReader stream)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();

        int numberOfKeyValuePairs = stream.ReadInt32();

        if(numberOfKeyValuePairs <= 0) return dict;

        for(int i = 0; i < numberOfKeyValuePairs; i++)
        {
            string key = ReadString(stream);
            string value = ReadString(stream);

            dict.Add(key, value);
        }

        return dict;
    }

    #endregion

    static Vector3Int ProcessCoordinate(int x, int y, int z)
    {
        return new Vector3Int(x, z, y);
    }
}

#endif
