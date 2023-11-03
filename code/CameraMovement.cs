using Godot;
using System;

namespace Camera;
public partial class CameraMovement : Camera2D {
	private bool Dragging = false;

	private Vector2 MaxZoom = new Vector2(5.0f, 5.0f);
	private Vector2 MinZoom = new Vector2(0.85f, 0.85f);
	private Vector2 ZoomStep = new Vector2(0.2f, 0.2f);

	public override void _Process(double deltaTime) {
		if(Input.IsActionJustPressed("ZoomIn")){
			Vector2 NewZoom = Zoom + ZoomStep;
			if(NewZoom.X < MaxZoom.X){
				Zoom = NewZoom;
			}

			//GD.Print("Zoom: ", Zoom);
			//GD.Print("MaxZoom: ", MaxZoom);
		}

		if(Input.IsActionJustPressed("ZoomOut")){
			Vector2 NewZoom = Zoom - ZoomStep;

			if(NewZoom.X > MinZoom.X){
				Zoom = NewZoom;
			}

			//GD.Print("Zoom: ", Zoom);
			//GD.Print("MinZoom: ", MinZoom);
		}
	}

	public override void _Input(InputEvent @event) {
		if(@event is InputEventMouseMotion eventMouseMotion){
			if(eventMouseMotion.ButtonMask == MouseButtonMask.Middle){
				Position -= eventMouseMotion.Relative;
			}
		}
	}
}
