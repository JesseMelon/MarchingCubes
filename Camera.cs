using Godot;
using System;
using System.Reflection;

public partial class Camera : Node3D
{
    const int RAY_LENGTH = 100;
    const float _MOUSE_SENSITIVITY = 0.005f;
    const float _SCROLL_SENSITIVITY = 0.05f;

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

        PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;
        Vector3 from = camera.ProjectRayOrigin(mousepos);
        Vector3 to = camera.ProjectRayNormal(mousepos) * RAY_LENGTH;
        var query = PhysicsRayQueryParameters3D.Create(from,to);
        var collision = space.IntersectRay(query);
        if (!collision.ContainsKey("collider"))return;
        
        GD.Print(collision["position"],collision["collider"]);
        var target = collision["collider"];


        
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
