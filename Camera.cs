using Godot;
using System;


namespace Project
{
    public partial class Camera : Node3D
    {
        [Export(PropertyHint.Layers2DPhysics)] public uint ColliderLayers { get; set; }

        const int RAY_LENGTH = 100;
        const float MOUSE_SENSITIVITY = 0.005f;
        const float SCROLL_SENSITIVITY = 0.5f;
        const float PAN_SENSITIVITY = 0.5f;

        bool isAddingTerrain;

        Camera3D camera; //set in ready
        Node3D cameraPivot;
        Signals signals;

        public override void _Input(InputEvent theEvent)
        {

            if (theEvent is InputEventMouseMotion inputEventMouseMotion)
            {
                //middle mouse button
                if (Input.IsActionPressed("camera_rotate"))
                {                   
                    if (Input.IsKeyPressed(Key.Shift)) 
                    {
                        //pan camera
                        if (Input.IsKeyPressed(Key.Alt))
                        {
                            Translate(new(-inputEventMouseMotion.Relative.X * PAN_SENSITIVITY, 0, inputEventMouseMotion.Relative.Y * PAN_SENSITIVITY));
                        }
                        else
                        {
                            Translate(new(-inputEventMouseMotion.Relative.X * PAN_SENSITIVITY, inputEventMouseMotion.Relative.Y * PAN_SENSITIVITY, 0));
                        }
                    }
                    else
                    {
                        //rotate camera
                        RotateY(-inputEventMouseMotion.Relative.X * MOUSE_SENSITIVITY);
                        RotateObjectLocal(camera.Basis.X, -inputEventMouseMotion.Relative.Y * MOUSE_SENSITIVITY);
                        Rotation = new Vector3(Mathf.Clamp(Rotation.X, -Mathf.Pi / 2, Mathf.Pi / 2), Rotation.Y, Rotation.Z);
                    }
                }


            }
            if (theEvent is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
            {

                switch (mouseButtonEvent.ButtonIndex)
                {
                    case MouseButton.Left:
                        isAddingTerrain = true;
                        RayFromMouse(GetViewport().GetMousePosition());
                        break;
                    case MouseButton.Right:
                        isAddingTerrain = false;
                        RayFromMouse(GetViewport().GetMousePosition());
                        break;
                    case MouseButton.WheelUp:
                        Scale -= new Vector3(SCROLL_SENSITIVITY, SCROLL_SENSITIVITY, SCROLL_SENSITIVITY);
                        break;
                    case MouseButton.WheelDown:
                        Scale += new Vector3(SCROLL_SENSITIVITY, SCROLL_SENSITIVITY, SCROLL_SENSITIVITY);
                        break;
                }
            }

        }
        void RayFromMouse(Vector2 mousepos)
        {

            PhysicsRayQueryParameters3D query = new()
            {
                From = camera.ProjectRayOrigin(mousepos),
                To = camera.ProjectPosition(mousepos, RAY_LENGTH),
                CollideWithAreas = false,
                CollideWithBodies = true,
                CollisionMask = ColliderLayers
            };

            var collisionData = GetWorld3D().DirectSpaceState.IntersectRay(query);


            if (collisionData.Count == 0) return;

            Node collider = collisionData["collider"].Obj as Node;
            Vector3 position = collisionData["position"].AsVector3();

            foreach (string group in collider.GetGroups())
            {
                if (group == "Terrain")
                {
                    signals.EmitSignal(nameof(signals.TerrainModified), position, isAddingTerrain);
                }
            }
        }
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            //nodes
            signals = GetNode<Signals>("/root/World");
            camera = GetNode<Camera3D>("Camera3D");
        }
    }
}