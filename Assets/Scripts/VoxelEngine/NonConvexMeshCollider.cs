/*
    LICENSE
    The code is provided under the MIT License which is basically just to provide myself some basic legal security.
    I definitely ALLOW you to use it in your own sofware (even commercial ones) without attributing me.
    But I totally DISALLOW to sell the NonConvexMeshCollider itself (or any derivative work) if you sell it as a solution for 'Non-Convex Mesh Colliding'.
    Also please dont repost the code somewhere else withour my approval, place a link to this page instead.
    The point is that I don't want anybody to just rip or modify my algorithm and sell it on the unity asset store or anywhere else! 
    If (ever) anybody should be allowed to sell this algorithm for what it is, it should be me, don't you think? ;)
    Anyhow: if you want to use it to provide non-convex mesh colliding in your own unity project, even if its a commercial one, go ahead and enjoy. :)
*/

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;

[ExecuteInEditMode]
public class NonConvexMeshCollider : MonoBehaviour
{
    public VoxelContainer vc;
    #pragma warning disable 0649
    [SerializeField] private bool profile;
    [SerializeField] private bool shouldMerge = true;
    [SerializeField] private bool createColliderChildGameObject = true;
    [Range(1, 100)]
    [SerializeField] private int boxesPerEdge = 20;
    #pragma warning restore 0649

    private void Awake()
    {
        vc = GetComponent<VoxelContainer>();
    }

    public void Generate()
    {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        if (boxesPerEdge > 100)
        {
            boxesPerEdge = 100;
        }
        if (boxesPerEdge < 1)
        {
            boxesPerEdge = 3;
        }

        MeshFilter meshFilter = vc.meshFilter;


        Rigidbody rbdy = vc.rb;

        bool hadNonKinematicRigidbody = rbdy ?? !rbdy.isKinematic;
        if(hadNonKinematicRigidbody) rbdy.isKinematic = true;

        bool hadRigidbodyWithGravityUse = rbdy ?? !rbdy.useGravity;
        if(hadRigidbodyWithGravityUse) rbdy.useGravity = false;

        if (!createColliderChildGameObject)
        {
            foreach (BoxCollider bc in gameObject.GetComponents<BoxCollider>())
            {
                if(Application.isPlaying)
                {
                    Destroy(bc);
                }
                else
                {
                    DestroyImmediate(bc);
                }
            }
        }

        Transform parentGo = gameObject.transform.parent;
        GameObject tempParent = new GameObject("Temp_CompoundColliderParent");
        gameObject.transform.parent = tempParent.transform;

        Vector3 localPos = gameObject.transform.localPosition;
        gameObject.transform.localPosition = Vector3.zero;
        Quaternion localRot = gameObject.transform.localRotation;
        gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
        Vector3 localScale = gameObject.transform.localScale;
        gameObject.transform.localScale = Vector3.one;

        GameObject collidersGo = CreateColliderChildGameObject(gameObject, meshFilter);

        //create compound colliders
        var boxes = CreateMeshIntersectingBoxes(collidersGo).ToArray();



        //merge boxes to create bigger and less ones
        var mergedBoxes =
            shouldMerge
                ? MergeBoxes(boxes.ToArray())
                : boxes;


        for(int i = 0; i < mergedBoxes.Length; i++)
        {
            BoxCollider bc = (createColliderChildGameObject ? collidersGo : gameObject).AddComponent<BoxCollider>();
            bc.size = mergedBoxes[i].Size;
            bc.center = mergedBoxes[i].Center;
        }

        //cleanup stuff not needed anymore on collider child obj
        if(Application.isPlaying)
        {
            Destroy(collidersGo.GetComponent<MeshFilter>());
            Destroy(collidersGo.GetComponent<MeshCollider>());
            Destroy(collidersGo.GetComponent<Rigidbody>());
            if (!createColliderChildGameObject)
                Destroy(collidersGo);
        }
        else
        {
            DestroyImmediate(collidersGo.GetComponent<MeshFilter>());
            DestroyImmediate(collidersGo.GetComponent<MeshCollider>());
            DestroyImmediate(collidersGo.GetComponent<Rigidbody>());
            if (!createColliderChildGameObject)
                DestroyImmediate(collidersGo);
        }

        //reset original state of root object
        gameObject.transform.parent = parentGo;
        gameObject.transform.localPosition = localPos;
        gameObject.transform.localRotation = localRot;
        gameObject.transform.localScale = localScale;
        if (hadNonKinematicRigidbody)
            rbdy.isKinematic = false;
        if (hadRigidbodyWithGravityUse)
            rbdy.useGravity = true;
        
        if(Application.isPlaying)
        {
            Destroy(tempParent);
        }
        else
        {
            DestroyImmediate(tempParent);
        }

        watch.Stop();
        // if(profile)
        // Debug.Log(watch.ElapsedMilliseconds);
    }

    private Box[] MergeBoxes(Box[] boxes)
    {
        Vector3Int[] mergeDirections = new Vector3Int[] {
            new Vector3Int(1,0,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,0,1),
            new Vector3Int(-1,0,0),
            new Vector3Int(0,-1,0),
            new Vector3Int(0,0,-1),
        };

        bool foundSomethingToMerge = false;
        do
        {
            for(int i = 0; i < mergeDirections.Length; i++)
            {
                foundSomethingToMerge = false;
                foreach (var box in boxes)
                {
                    var merged = box.TryMerge(mergeDirections[i]);
                    if (merged)
                        foundSomethingToMerge = true;
                }
                boxes = boxes.Select(b => b.Root).Distinct().ToArray();
            }
        } while (foundSomethingToMerge);

        return boxes.Select(b => b.Root).Distinct().ToArray();
    }

    private static GameObject CreateColliderChildGameObject(GameObject go, MeshFilter meshFilter)
    {
        //ensure collider child gameobject exists
        Transform collidersTransform = go.transform.Find("Colliders");
        GameObject collidersGo;
        if (collidersTransform != null)
            collidersGo = collidersTransform.gameObject;
        else
        {
            collidersGo = new GameObject("Colliders");
            collidersGo.transform.parent = go.transform;
            collidersGo.transform.localRotation = Quaternion.Euler(Vector3.zero);
            collidersGo.transform.localPosition = Vector3.zero;
        }

        //reset collider child gameobject
        foreach (var bc in collidersGo.GetComponents<BoxCollider>())
        {
            Destroy(bc);
        }
        var mf = collidersGo.GetComponent<MeshFilter>();
        if (mf != null) Destroy(mf);
        var mc = collidersGo.GetComponent<MeshCollider>();
        if (mc != null) Destroy(mc);
        var rd = collidersGo.GetComponent<Rigidbody>();
        if (rd != null) Destroy(rd);
        rd = collidersGo.AddComponent<Rigidbody>();
        rd.isKinematic = true;
        rd.useGravity = false;

        //setup collider child gameobject
        mf = collidersGo.AddComponent<MeshFilter>();
        mf.sharedMesh = meshFilter.sharedMesh;
        mc = collidersGo.AddComponent<MeshCollider>();
        mc.convex = false;
        return collidersGo;
    }

    // Performance hit - GO OFF OF VOXEL DATA.
    private IEnumerable<Box> CreateMeshIntersectingBoxes(GameObject colliderGo)
    {
        GameObject go = colliderGo.transform.parent.gameObject;
        Bounds bounds = CalculateLocalBounds(go);

        var boxes = new Box[boxesPerEdge, boxesPerEdge, boxesPerEdge];
        var boxColliderPositions = new bool[boxesPerEdge, boxesPerEdge, boxesPerEdge];
        var s = bounds.size / boxesPerEdge;
        var halfExtent = s / 2;

        Vector3 v = new Vector3();

        for (int x = 0; x < boxesPerEdge; x++)
        {
            for (int y = 0; y < boxesPerEdge; y++)
            {
                for (int z = 0; z < boxesPerEdge; z++)
                {
                    v.x = bounds.center.x - bounds.size.x / 2 + s.x * x + s.x / 2;
                    v.y = bounds.center.y - bounds.size.y / 2 + s.y * y + s.y / 2;
                    v.z = bounds.center.z - bounds.size.z / 2 + s.z * z + s.z / 2;

                    var colliders = Physics.OverlapBox(v, halfExtent);

                    if (colliders.Length > 0 && colliders.Any(c => c.gameObject == colliderGo))
                        boxColliderPositions[x, y, z] = true;
                }
            }
        }

        for (int x = 0; x < boxesPerEdge; x++)
        {
            for (int y = 0; y < boxesPerEdge; y++)
            {
                for (int z = 0; z < boxesPerEdge; z++)
                {
                    if (!boxColliderPositions[x, y, z]) continue;

                    Vector3 center = new Vector3(
                        bounds.center.x - bounds.size.x / 2 + s.x * x + s.x / 2,
                        bounds.center.y - bounds.size.y / 2 + s.y * y + s.y / 2,
                        bounds.center.z - bounds.size.z / 2 + s.z * z + s.z / 2);

                    Box b = new Box(boxes, center, s, new Vector3Int(x, y, z));

                    boxes[x, y, z] = b;

                    yield return b;
                }
            }
        }
    }

    private static Bounds CalculateLocalBounds(GameObject go)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(renderer.bounds);
        var localCenter = bounds.center - go.transform.position;
        bounds.center = localCenter;
        return bounds;
    }

    public class Box
    {
        private readonly Box[,,] boxes;
        private readonly Vector3Int lastLevelGridPos;

        public Box(Box[,,] boxes, Vector3? center = null, Vector3? size = null, Vector3Int lastLevelGridPos = null)
        {
            this.boxes = boxes;
            this.lastLevelGridPos = lastLevelGridPos;
            this.center = center;
            this.size = size;
        }

        public Vector3 Center
        {
            get
            {
                if (center == null)
                {
                    if (Children == null) throw new Exception("Last level child box needs a center position");
                    var v = Vector3.zero;
                    foreach (var b in LastLevelBoxes)
                        v += b.Center;
                    v = v / LastLevelBoxes.Length;
                    center = v;
                }
                return center.Value;
            }
        }

        public Vector3 Size
        {
            get
            {
                if (size == null)
                {
                    if (Children == null) throw new Exception("Last level child box needs a size");
                    var singleBoxSize = LastLevelBoxes[0].Size;
                    size = new Vector3(GridSize.X * singleBoxSize.x, GridSize.Y * singleBoxSize.y, GridSize.Z * singleBoxSize.z);
                }
                return size.Value;
            }
        }

        private void MergeWith(Box other)
        {
            var b = new Box(boxes);
            foreach (var child in new[] { this, other })
                child.Parent = b;
            b.Children = new[] { this, other };
            Box temp = b;
        }

        public Box Parent { get; set; }
        public Box[] Children { get; set; }

        public IEnumerable<Box> Parents
        {
            get
            {
                var b = this;
                while (b.Parent != null)
                {
                    yield return b.Parent;
                    b = b.Parent;
                }
            }
        }

        public IEnumerable<Box> SelfAndParents
        {
            get
            {
                yield return this;
                foreach (var parent in Parents)
                    yield return parent;
            }
        }

        public Box Root
        {
            get
            {
                return Parent == null ? this : Parent.Root;
            }
        }

        public bool TryMerge(Vector3Int direction)
        {
            if (Parent != null) return false;
            foreach (var p in CoveredGridPositions)
            {
                var pos = new Vector3Int(p.X + direction.X, p.Y + direction.Y, p.Z + direction.Z);
                if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
                    continue;
                if (pos.X >= boxes.GetLength(0) || pos.Y >= boxes.GetLength(1) || pos.Z >= boxes.GetLength(2))
                    continue;
                var b = boxes[pos.X, pos.Y, pos.Z];
                if (b == null)
                    continue;
                b = b.Root;
                if (b == this)
                    continue;
                if (direction.X == 0 && b.GridSize.X != GridSize.X)
                    continue;
                if (direction.Y == 0 && b.GridSize.Y != GridSize.Y)
                    continue;
                if (direction.Z == 0 && b.GridSize.Z != GridSize.Z)
                    continue;
                if (direction.X == 0 && MinGridPos.X != b.MinGridPos.X)
                    continue;
                if (direction.Y == 0 && MinGridPos.Y != b.MinGridPos.Y)
                    continue;
                if (direction.Z == 0 && MinGridPos.Z != b.MinGridPos.Z)
                    continue;
                MergeWith(b);
                return true;
            }
            return false;
        }

        public IEnumerable<Box> ChildrenRecursive
        {
            get
            {
                if (Children == null) yield break;
                foreach (var c in Children)
                {
                    yield return c;
                    foreach (var cc in c.ChildrenRecursive)
                        yield return cc;
                }
            }
        }

        public IEnumerable<Box> SelfAndChildrenRecursive
        {
            get
            {
                yield return this;
                foreach (var c in ChildrenRecursive)
                    yield return c;
            }
        }

        private Box[] lastLevelBoxes;
        public Box[] LastLevelBoxes
        {
            get
            {
                if (lastLevelBoxes == null)
                    lastLevelBoxes = SelfAndChildrenRecursive.Where(c => c.Children == null).ToArray();
                return lastLevelBoxes;
            }
        }

        private IEnumerable<Vector3Int> CoveredGridPositions
        {
            get { return LastLevelBoxes.Select(c => c.lastLevelGridPos); }
        }

        private int MinGridPosX
        {
            get { return Children == null ? lastLevelGridPos.X : CoveredGridPositions.Min(p => p.X); }
        }
        
        private int MinGridPosY
        {
            get { return Children == null ? lastLevelGridPos.Y : CoveredGridPositions.Min(p => p.Y); }
        }

        private int MinGridPosZ
        {
            get { return Children == null ? lastLevelGridPos.Z : CoveredGridPositions.Min(p => p.Z); }
        }

        private int MaxGridPosX
        {
            get { return Children == null ? lastLevelGridPos.X : CoveredGridPositions.Max(p => p.X); }
        }

        private int MaxGridPosY
        {
            get { return Children == null ? lastLevelGridPos.Y : CoveredGridPositions.Max(p => p.Y); }
        }

        private int MaxGridPosZ
        {
            get { return Children == null ? lastLevelGridPos.Z : CoveredGridPositions.Max(p => p.Z); }
        }

        private Vector3Int minGridPos;
        private Vector3Int MinGridPos
        {
            get { return minGridPos ?? (minGridPos = new Vector3Int(MinGridPosX, MinGridPosY, MinGridPosZ)); }
        }

        private Vector3Int maxGridPos;
        private Vector3Int MaxGridPos
        {
            get { return maxGridPos ?? (maxGridPos = new Vector3Int(MaxGridPosX, MaxGridPosY, MaxGridPosZ)); }
        }

        private Vector3Int gridSize;
        private Vector3? center;
        private Vector3? size;

        private Vector3Int GridSize
        {
            get
            {
                if (gridSize == null)
                    gridSize = Children == null
                        ? Vector3Int.One
                        : new Vector3Int(
                            MaxGridPos.X - MinGridPos.X + 1,
                            MaxGridPos.Y - MinGridPos.Y + 1,
                            MaxGridPos.Z - MinGridPos.Z + 1);
                return gridSize;
            }
        }
    }

    public class Vector3Int
    {
        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public static readonly Vector3Int One = new Vector3Int(1, 1, 1);

        protected bool Equals(Vector3Int other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Vector3Int)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Z;
                return hashCode;
            }
        }
        
        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", X, Y, Z);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(NonConvexMeshCollider))]
public class NonConvexMeshColliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NonConvexMeshCollider nonConvexMeshCollider = (NonConvexMeshCollider)target;
        if (GUILayout.Button("Generate"))
            nonConvexMeshCollider.Generate();
    }
}
#endif