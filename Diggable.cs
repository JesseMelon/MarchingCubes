using Godot;
using System;

namespace Project
{


    public partial class Diggable : Node
    {
        [Export] public Terrain Terrain { get; set; }

        public void Add(Vector3 atPos)
        {
            if (Terrain == null) return;
            {
                GD.Print("Calling PlaceTerrain");
                Terrain.PlaceTerrain(atPos);
            }
        }
        public void Remove(Vector3 atPos)
        {
            if (Terrain == null) return;

            //Terrain.Remove(atPos);
        }

    }
}
