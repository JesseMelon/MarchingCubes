using Godot;
using System;


namespace Project;
[Tool]
public partial class ChunkManager : Node
{
    [ExportGroup("Size")]
    [Export] static int chunkWidth = 16;//# blocks wide (x and z), positive quadrant
    [Export] static int chunkHeight = 8;

    public override void _Ready()
    {
        
        AddChild(new Chunk(new Vector3I(0, 0, 0), chunkWidth, chunkHeight));
        AddChild(new Chunk(new Vector3I(0, 0, 16), chunkWidth, chunkHeight));
    }

}
