using Godot;
using System;
using System.Collections.Generic;


namespace Project;
public partial class ChunkManager : Node
{
    [Export] BaseMaterial3D material;
    [ExportGroup("Size")]
    [Export] int chunksWide = 10;
    [Export] int chunksLong = 10;
    [Export] int chunkWidth = 16;
    [Export] int chunkHeight = 8;
    [Export] static readonly float baseSurfaceHeight = 5f;
    [Export] static readonly float surfaceHeightRange = 5f;

    static readonly FastNoiseLite fastNoise = new();

    readonly Godot.Collections.Dictionary<Vector3I, Chunk> chunks = new();

    Signals signals;

    public void OnTerrainModifed(Vector3 position, bool isAdding)
    {
        if (isAdding)
        {
            ChunkFromVector3(position).PlaceTerrain(position);
        }
        else
        {
            ChunkFromVector3(position).RemoveTerrain(position);
        }
    }
    private Chunk ChunkFromVector3(Vector3 position)
    {
        int x = (int)position.X - (int)(position.X % chunkWidth);
        int z = (int)position.Z - (int)(position.Z % chunkWidth);
        return chunks[new Vector3I(x, 0, z)];
    }
    public static float GetTerrainHeight(int x, int y, int z)
    {
        return (surfaceHeightRange * fastNoise.GetNoise2D((float)x, (float)z)) + baseSurfaceHeight;
    }

    //public static Chunk GetChunkFromVector3I(Vector3I v3I)
    //{
    //    return 
    //}
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
}
