using Godot;
using System;
using CellManager;
using Camera;

namespace GUI;
public partial class Controls : Panel {
	private Button PlayPauseButton;
	private Slider GenPerSecond;
	private Label GenPerSecondLabel;
	private ColorPickerButton AliveColorButton;
	private ColorPickerButton DeadColorButton;
	private Timer GenerationTimer;
	private Manager Parent;
	
	public override void _Ready() {
		// Get all the relevant nodes
		PlayPauseButton = GetNode<Button>("Play_Pause");
		GenPerSecond = GetNode<Slider>("GenPerSecond");
		GenPerSecondLabel = GenPerSecond.GetNode<Label>("Value");
		AliveColorButton = GetNode<ColorPickerButton>("AliveColor");
		DeadColorButton = GetNode<ColorPickerButton>("DeadColor");
		Parent = GetParent<CanvasLayer>().GetParent<CameraMovement>().GetParent<Manager>();
		GenerationTimer = Parent.GetNode<Timer>("GenerationTimer");

		GenPerSecondLabel.Text = GenPerSecond.Value.ToString();
	}

	public void OnPlayPausedToggled(bool Toggled) {
		if (Toggled) {
			PlayPauseButton.Text = "Pause";
			GenerationTimer.Start();
			Parent.SimulationPaused = false;
		} else {
			PlayPauseButton.Text = "Play";
			GenerationTimer.Stop();
			Parent.SimulationPaused = true;
		}
	}

	public void OnGenPerSecondChanged(float value) {
		GenPerSecondLabel.Text = value.ToString();
		GenerationTimer.WaitTime = 1 / value;
	}

	public void OnAliveColorChanged(Color color) {
		Parent.ActiveColor = color;
	}

	public void OnDeadColorChanged(Color color) {
		Parent.InactiveColor = color;
	}
}
