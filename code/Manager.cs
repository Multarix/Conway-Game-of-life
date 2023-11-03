using Godot;
using Godot.Collections;
using System;

namespace CellManager;

public partial class Manager : Node {

	private MultiMeshInstance2D MultiMeshInstance;
	private MultiMesh Multimesh;
	private Rid MultiMeshRid;

	private RenderingDevice RD;
	private Rid Shader;
	private Rid UniformSet;
	private Rid Pipeline;

	private Rid CurrentGenerationBuffer;
	private Rid NextGenerationBuffer;
	private Rid GlobalsBuffer;

	public bool SimulationPaused = true;
	private int LastHoveredCell = -1;
	

	private byte[] CurrentGeneration;
	private byte[] NextGeneration;
	private float[] Globals;


	[ExportCategory("World Settings")]
	[Export]
	private Vector2I GridSize = Vector2I.One;
	private int TotalCells;


	[ExportCategory("Cell Settings")]
	[Export(PropertyHint.Range, "4,15,1")]
	private int CellSize = 10;
	[Export(PropertyHint.Range, "1,5,1")]
	private int GapBetweenCells = 1;
	[Export(PropertyHint.ColorNoAlpha)]
	public Color ActiveColor = new Color(1, 1, 1, 1);
	[Export(PropertyHint.ColorNoAlpha)]
	public Color InactiveColor = new Color(0.3f, 0.3f, 0.3f, 1);

	private Color HoverColor = Color.FromHtml("#00FF00");


	


	void BuildMesh() {
		// Make a square mesh
		Vector3[] points = new Vector3[]{
			new Vector3(0, 0, 0),
			new Vector3(1 * CellSize, 0, 0),
			new Vector3(1 * CellSize, 1 * CellSize, 0),
			new Vector3(0, 1 * CellSize, 0)
		};

		ArrayMesh arrayMesh = new ArrayMesh();

		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = points;

		int[] indices = new int[]{
			0, 1, 3,
			1, 2, 3
		};

		arrays[(int)Mesh.ArrayType.Index] = indices;
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		Multimesh.Mesh = arrayMesh;
	}



	public override void _Ready() {
		TotalCells = GridSize.X * GridSize.Y;

		CurrentGeneration = new byte[TotalCells * 4 * 16];
		NextGeneration = new byte[TotalCells * 4 * 16];
		Globals = new float[8];

		MultiMeshInstance = GetNode<MultiMeshInstance2D>("MultiMeshInstance2D");
		Multimesh = MultiMeshInstance.Multimesh;
		MultiMeshRid = Multimesh.GetRid();

		BuildMesh();

		Multimesh.InstanceCount = TotalCells;

		CreateMultiMeshInstanceData();
	}



	private void CreateMultiMeshInstanceData() {
		// Left to right, top to bottom...
		for (int row = 0; row < GridSize.Y; row++) {
			for (int col = 0; col < GridSize.X; col++) {
				int CellID = (row * GridSize.X) + col;

				float XPos = (CellSize + GapBetweenCells) * col;
				float YPos = (CellSize + GapBetweenCells) * row;

				Vector2 CellLocation = new Vector2(XPos, YPos);

				Transform2D CellTransform = new Transform2D(0, CellLocation);
				Multimesh.SetInstanceTransform2D(CellID, CellTransform);
				Multimesh.SetInstanceColor(CellID, InactiveColor);
				Multimesh.SetInstanceCustomData(CellID, new Color(0, 0, 0, 0));
			}
		}
	}



	// Create the rendering device and the shader.
	private void InitGPU() {
		RD = RenderingServer.CreateLocalRenderingDevice();

		RDShaderFile ShaderFile = GD.Load<RDShaderFile>("res://code/shaders/compute.glsl");
		RDShaderSpirV ShaderBytecode = ShaderFile.GetSpirV();
		Shader = RD.ShaderCreateFromSpirV(ShaderBytecode);
	}



	private void UpdateGLobals() {
		Globals[0] = GridSize.X;
		Globals[1] = GridSize.Y;
		Globals[2] = ActiveColor.R;
		Globals[3] = ActiveColor.G;
		Globals[4] = ActiveColor.B;
		Globals[5] = InactiveColor.R;
		Globals[6] = InactiveColor.G;
		Globals[7] = InactiveColor.B;
	}



	private void InitBuffers() {
		float[] CurGen = RenderingServer.MultimeshGetBuffer(MultiMeshRid);
		Buffer.BlockCopy(CurGen, 0, CurrentGeneration, 0, CurrentGeneration.Length);
		Buffer.BlockCopy(CurGen, 0, NextGeneration, 0, NextGeneration.Length);

		CurrentGenerationBuffer = RD.StorageBufferCreate((uint)CurrentGeneration.Length, CurrentGeneration);
		NextGenerationBuffer = RD.StorageBufferCreate((uint)NextGeneration.Length, NextGeneration);


		UpdateGLobals();
		byte[] GlobalBytes = new byte[Globals.Length * 4];
		Buffer.BlockCopy(Globals, 0, GlobalBytes, 0, GlobalBytes.Length);
		GlobalsBuffer = RD.StorageBufferCreate((uint)GlobalBytes.Length, GlobalBytes);


		RDUniform CurGenUniform = new RDUniform() {
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0,
		};
		CurGenUniform.AddId(CurrentGenerationBuffer);

		RDUniform NextGenUniform = new RDUniform() {
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1,
		};
		NextGenUniform.AddId(NextGenerationBuffer);

		RDUniform GlobalsUniform = new RDUniform() {
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 2,
		};
		GlobalsUniform.AddId(GlobalsBuffer);


		UniformSet = RD.UniformSetCreate(new Array<RDUniform> { CurGenUniform, NextGenUniform, GlobalsUniform }, Shader, 0);
		Pipeline = RD.ComputePipelineCreate(Shader);
	}



	private void UpdateBuffers() {
		UpdateGLobals();

		byte[] GlobalBytes = new byte[Globals.Length * 4];
		Buffer.BlockCopy(Globals, 0, GlobalBytes, 0, GlobalBytes.Length);


		float[] CurGen = RenderingServer.MultimeshGetBuffer(MultiMeshRid);
		Buffer.BlockCopy(CurGen, 0, CurrentGeneration, 0, CurrentGeneration.Length);
		Buffer.BlockCopy(CurGen, 0, NextGeneration, 0, NextGeneration.Length);


		_ = RD.BufferUpdate(CurrentGenerationBuffer, 0, (uint)CurrentGeneration.Length, CurrentGeneration);
		_ = RD.BufferUpdate(NextGenerationBuffer, 0, (uint)NextGeneration.Length, NextGeneration);
		_ = RD.BufferUpdate(GlobalsBuffer, 0, (uint)GlobalBytes.Length, GlobalBytes);
	}



	// Submits the data to the GPU, then waits for it to finish.
	private void SubmitToGPU() {
		// Create the compute list, and all that good stuff.
		long ComputeList = RD.ComputeListBegin();

		RD.ComputeListBindComputePipeline(ComputeList, Pipeline);
		RD.ComputeListBindUniformSet(ComputeList, UniformSet, 0);
		RD.ComputeListDispatch(ComputeList, xGroups: (uint)TotalCells, yGroups: 1, zGroups: 1);
		RD.ComputeListEnd();

		// Submit and sync.
		RD.Submit();

		// I see everywhere people saying "wait a few frames..."
		// But there is no documentation for "waiting a few frames"
		// The best is waiting till the next frame, any longer and it just crashes.
		RD.Sync();
	}



	private void GetResultsFromGPU() {
		byte[] NewGen = RD.BufferGetData(NextGenerationBuffer);

		float[] NextGen = new float[TotalCells * 16];
		Buffer.BlockCopy(NewGen, 0, NextGen, 0, NewGen.Length);

		//int i = 0;
		//GD.PrintT("Color:", NextGen[i + 8], NextGen[i + 9], NextGen[i + 10], NextGen[i + 11]);
		//GD.PrintT("Custom:", NextGen[i + 12], NextGen[i + 13], NextGen[i + 14], NextGen[i + 15]);

		RenderingServer.MultimeshSetBuffer(MultiMeshRid, NextGen);
	}
	


	int GetCellFromMousePosition() {
		//Vector2 GlobalMousePos = GetNode<Camera2D>("Camera2D").GetGlobalMousePosition();
		Vector2 MousePos = GetNode<Camera2D>("Camera2D").GetGlobalMousePosition();
		if (MousePos.X < 0 || MousePos.Y < 0) return -1;

		int CellID = -1;

		bool OutSideX = MousePos.X > (GridSize.X * (CellSize + GapBetweenCells)) - GapBetweenCells;
		bool OutSideY = MousePos.Y > (GridSize.Y * (CellSize + GapBetweenCells)) - GapBetweenCells;
		if(OutSideX || OutSideY) return CellID;
		
		Vector2 CellPos = new Vector2(
			(int)(MousePos.X / (CellSize + GapBetweenCells)),
			(int)(MousePos.Y / (CellSize + GapBetweenCells))
		);

		CellID = (int)(CellPos.Y * GridSize.X) + (int)CellPos.X;
		if(CellID >= TotalCells) return -1;

		return CellID;
	}



	public override void _Process(double Delta) {
		if(SimulationPaused) {
			int CurCell = GetCellFromMousePosition();
			UpdateHoveredCell(CurCell);
			UpdateCellIfClicked(CurCell);
		}
	}



	private void UpdateCellIfClicked(int CurCell) {
		if (Input.IsActionJustPressed("LeftClick")) {
			if (CurCell != -1) {
				bool IsAlive = Multimesh.GetInstanceCustomData(LastHoveredCell)[0] == 1.0;
				Multimesh.SetInstanceCustomData(CurCell, new Color(IsAlive ? 0 : 1, 0, 0, 0));
			}
		}
	}



	private void UpdateHoveredCell(int CurCell) {
		if (CurCell != LastHoveredCell) {

			//GD.PrintT("LastCell:", LastHoveredCell, "CurCell:", CurCell);

			if (LastHoveredCell >= 0) {
				Color CellOriginalColor = (Multimesh.GetInstanceCustomData(LastHoveredCell)[0] == 1.0) ? ActiveColor : InactiveColor;
				Multimesh.SetInstanceColor(LastHoveredCell, CellOriginalColor);
			}

			if (CurCell >= 0) {
				//Color CellOriginalColor = (Multimesh.GetInstanceCustomData(LastHoveredCell)[0] == 1.0) ? ActiveColor : InactiveColor;
				Multimesh.SetInstanceColor(CurCell, HoverColor);
			}

			LastHoveredCell = CurCell;
		}
	}



	public void OnTimerTimeout() {
		// Setup the GPU if it's not already setup, otherwise update the buffers..
		if (RD == null) {
			InitGPU();
			InitBuffers();
		} else {
			UpdateBuffers();
		}

		GD.Print("Moving to next Generation...");
		// I think this is self explanatory?
		SubmitToGPU();
		GetResultsFromGPU();
	}
}
