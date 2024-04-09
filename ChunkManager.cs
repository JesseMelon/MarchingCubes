using Godot;
using GodotVectorOps;

//TODO send bounding shape along with values to update terrain map then have necessary chunks regenerate
//send bounding box & use to get relevant positions in terrain, then get distance (radius parameter) from origin to filter changes to a round area. 
//TODO incorporate textures
//TODO incorporate material brushes into volume brushes

namespace Terrain;

/// <summary>
/// Recieves signals, and manages data among terrain chunks
/// </summary>
public partial class ChunkManager : Node
{
    Signals signals;

    [Export] BaseMaterial3D material;
    [ExportGroup("Size")]
    [Export] private int chunksWide = 10;
    [Export] private int chunksLong = 10;
    [Export] private int chunkWidth = 16;
    [Export] private int chunkHeight = 16;
    [Export] private static readonly float baseSurfaceHeight = 10f;
    [Export] private static readonly float surfaceHeightRange = 5f;

    private static readonly FastNoiseLite fastNoise = new();
    //dictionary of chunks indexed by int position
    readonly Godot.Collections.Dictionary<Vector3I, Chunk> chunks = new();


    public void OnTerrainModifed(Vector3 position, float radius, float speed)
    {
        EditTerrain(position, radius, speed);



        //if (isAdding)
        //{
        //    chunks[ChunkPosFromVector3(position)].PlaceTerrain(position);
        //}
        //else
        //{
        //    chunks[ChunkPosFromVector3(position)].RemoveTerrain(position);
        //}
    }
    private Vector3I ChunkPosFromVector3(Vector3 position)
    {
        int x = (int)position.X - (int)(position.X % chunkWidth);
        int y = (int)position.Y - (int)(position.Y % chunkHeight);
        int z = (int)position.Z - (int)(position.Z % chunkWidth);
        return new Vector3I(x, y, z);
    }
    private Vector3I ChunkPosFromVector3I(Vector3I position)
    {
        int x = position.X - (position.X % chunkWidth);
        int y = position.Y - (position.Y % chunkHeight);
        int z = position.Z - (position.Z % chunkWidth);
        return new Vector3I(x, y, z);
    }
    public static float GetTerrainHeight(int x, int y, int z)
    {
        return (surfaceHeightRange * fastNoise.GetNoise2D((float)x, (float)z)) + baseSurfaceHeight;
    }

    public override void _Ready()
    {
        signals = GetNode<Signals>("/root/World");
        signals.TerrainModified += OnTerrainModifed;
        GenerateTerrain();
        //GD.Print(chunks[new Vector3I(0,0,0)]);
    }

    private void GenerateTerrain()
    {
        for (int x = 0; x < chunksWide; x++)
        {
            for(int z = 0; z < chunksLong; z++)
            {
                Vector3I chunkPosition = new(x * chunkWidth, 0, z * chunkWidth);
                Chunk newChunk = new(chunkPosition, chunkWidth, chunkHeight, material);
                AddChild(newChunk);
                chunks.Add(chunkPosition, newChunk);

            }
        }
    }

    /// <summary>
    /// Change shape of necessary chunks in a sphere for now
    /// </summary>
    /// <param name="position">point of interest</param>
    /// <param name="radius">negative indicates digging</param>
    /// <param name="speed">0-1 where 1 is instant</param>
    private void EditTerrain(Vector3 position, float radius, float speed)
    {
        //create bounding cube from point
        //even if position is near enough to another chunk considering radius, we only must involve another chunk if we overlap deeply enough that we catch some integer points of definition.
        //therefore we must get the bounding box as integers. The operation may involve up to 8 chunks, therefore each point must be checked separately

        //     6--------7   wrapping pattern for bounds
        //    /|       /|
        //   2-+------3 |     y
        //   | |      | |     |
        //   | 4------|-5     0---x
        //   |/       |/     /
        //   0--------1     -z

        BoundsI bounds = new(position,new Vector3(radius,radius,radius),true);
        Chunk[] AffectedChunks = new Chunk[8];

        //check if entire box is within chunk
        Vector3I chunkPos = ChunkPosFromVector3I(bounds.GetCornerFromIndex(7));
        int i = 0;

        //if there is a seam across an axis, must sample the opposite axis. 
        if(chunkPos.X > bounds.Position.X){ i += 1; }
        if(chunkPos.Y > bounds.Position.Y){ i += 2; }
        if(chunkPos.Z > bounds.Position.Z){ i += 4; }

        switch (i)
        {
            case 0:
                //no seems, edit original probe
                chunks[chunkPos].EditTerrain();
                break;  
            case 1:
                //seam along X.
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(6))].EditTerrain();
                break;
            case 2:
                //seam along Y.
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(5))].EditTerrain();
                break;
            case 3:
                //seam along X and Y.
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(4))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(5))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(6))].EditTerrain();
                break;
            case 4:
                //seam along Z.
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(3))].EditTerrain();
                break;
            case 5:
                //seam along X and Z.
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(2))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(3))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(6))].EditTerrain();
                break;
            case 6:
                //seam along Y and Z
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(1))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(3))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(5))].EditTerrain();
                break;
            case 7:
                //seam along X, Y, and Z
                chunks[chunkPos].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(0))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(1))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(2))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(3))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(4))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(5))].EditTerrain();
                chunks[ChunkPosFromVector3I(bounds.GetCornerFromIndex(6))].EditTerrain();
                break;
        }

    }
    private void GatherChunks()
    {

    }

    /// <param name="point">The point to modify the terrain around</param>
    /// <param name="addTerrain">Should terrain be added or removed</param>
    /// <param name="deformSpeed">How fast the terrain should be deformed</param>
    /// <param name="range">How far the deformation can reach</param>
    //private void EditTerrain(Vector3 point, bool addTerrain, float deformSpeed, float range)
    //{
    //    int buildModifier = addTerrain ? 1 : -1;

    //    int hitX = Mathf.RoundToInt(point.x);
    //    int hitY = Mathf.RoundToInt(point.y);
    //    int hitZ = Mathf.RoundToInt(point.z);
    //    int3 hitPoint = new int3(hitX, hitY, hitZ);

    //    int intRange = Mathf.CeilToInt(range);
    //    int3 rangeInt3 = new int3(intRange, intRange, intRange);
    //    BoundsInt queryBounds = new BoundsInt((hitPoint - rangeInt3).ToVectorInt(), new int3(intRange * 2).ToVectorInt());

    //    voxelWorld.VoxelDataStore.SetVoxelDataCustom(queryBounds, (voxelDataWorldPosition, voxelData) =>
    //    {
    //        float distance = math.distance(voxelDataWorldPosition, point);
    //        if (distance <= range)
    //        {
    //            float modificationAmount = deformSpeed / distance * buildModifier;
    //            float oldVoxelData = voxelData / 255f;
    //            return (byte)math.clamp((oldVoxelData - modificationAmount) * 255, 0, 255);
    //        }

    //        return voxelData;
    //    });
}
