using Godot;

namespace Terrain;
public partial class Signals : Node
{

    [Signal] public delegate void TerrainModifiedEventHandler(Vector3 position, float radius, float speed);

}
