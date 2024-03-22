using Godot;
using System;


namespace Project;
public partial class Signals : Node
{

    [Signal] public delegate void TerrainModifiedEventHandler(Vector3 position, bool isAdding);

}
