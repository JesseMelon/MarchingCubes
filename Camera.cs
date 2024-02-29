using Godot;
using System;
using System.Reflection;

public partial class Camera : Node3D
{
    const int RAY_LENGTH = 100;
    const float _MOUSE_SENSITIVITY = 0.005f;
    const float _SCROLL_SENSITIVITY = 0.05f;
    [Export(PropertyHint.Layers2DPhysics)] public uint ColliderLayers {  get; set; }

    Camera3D camera; //set in ready
    Node3D cameraPivot;

    public override void _Input(InputEvent theEvent)
    {

        if (theEvent is InputEventMouseMotion inputEventMouseMotion)
        {
            //middle mouse button
            if (Input.IsActionPressed("camera_rotate"))
            {
                //rotate camera
                RotateY(-inputEventMouseMotion.Relative.X * _MOUSE_SENSITIVITY);
                RotateObjectLocal(camera.Basis.X, -inputEventMouseMotion.Relative.Y * _MOUSE_SENSITIVITY);
                Rotation = new Vector3(Mathf.Clamp(Rotation.X, -Mathf.Pi / 2, Mathf.Pi / 2), Rotation.Y, Rotation.Z);
            }


        }
        if (theEvent is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
        {

            switch (mouseButtonEvent.ButtonIndex)
            {
                case MouseButton.Left:
                    RayFromMouse(GetViewport().GetMousePosition());
                    break;
                case MouseButton.Right:
                    break;
                case MouseButton.WheelUp:
                    Scale -= new Vector3(_SCROLL_SENSITIVITY, _SCROLL_SENSITIVITY, _SCROLL_SENSITIVITY);
                    break;
                case MouseButton.WheelDown:
                    Scale += new Vector3(_SCROLL_SENSITIVITY, _SCROLL_SENSITIVITY, _SCROLL_SENSITIVITY);
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

        GD.Print(collisionData["position"],collisionData["collider"]);

        if (collider?.GetNodeOrNull("Diggable") is Diggable diggableThing)
        {
            GD.Print("Recognized collider and diggable");
            diggableThing.Add(position);
            //dig cases
        }


        
    }
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //nodes
        camera = GetNode<Camera3D>("Camera3D");
        
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
    }
}
