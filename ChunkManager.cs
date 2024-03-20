using Godot;
using System;
using System.Collections.Generic;


namespace Project;
[Tool]
public partial class ChunkManager : Node
{
    [Export] BaseMaterial3D material;
    [ExportGroup("Size")]
    [Export] int chunksWide = 10;
    [Export] int chunksLong = 10;
    [Export] int chunkWidth = 16;
    [Export] int chunkHeight = 8;
    [Export] static readonly float baseSurfaceHeight = 0f;
    [Export] static readonly float surfaceHeightRange = 5f;

    static readonly FastNoiseLite fastNoise = new();

    readonly Dictionary<Vector3I, Chunk> chunks = new();

    public static float GetTerrainHeight(int x, int y, int z)
    {
        return surfaceHeightRange * fastNoise.GetNoise3D((float)x, (float)y, (float)z) + baseSurfaceHeight + surfaceHeightRange;
    }
    public override void _Ready()
    {
       GenerateTerrain();
    }

    void GenerateTerrain()
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
}
