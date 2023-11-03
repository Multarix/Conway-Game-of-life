#[compute]
#version 460

// Conway's Game of Life
// https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life



layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;


layout(set = 0, binding = 0, std430) restrict readonly buffer CurrentGenerationBuffer {
	float CurrentGeneration[][16];
};

layout(set = 0, binding = 1, std430) restrict buffer NextGenerationBuffer {
	float NextGeneration[][16];
};

layout(set = 0, binding = 2, std140) restrict readonly buffer Globals {
	float GridSizeX;
	float GridSizeY;
	float ActiveColorR;
	float ActiveColorG;
	float ActiveColorB;
	float InActiveColorR;
	float InActiveColorG;
	float InActiveColorB;
};



vec2 GridSize = vec2(GridSizeX, GridSizeY);
vec3 ActiveColor = vec3(ActiveColorR, ActiveColorG, ActiveColorB);
vec3 InactiveColor = vec3(InActiveColorR, InActiveColorG, InActiveColorB);



bool isCellAlive(int cellID) {
	float aliveFloat = CurrentGeneration[cellID][12];
	return (aliveFloat == 1.0f);
}


int[9] getNearbyCells(int cellID){
	int gridXInt = int(GridSize.x);
	int gridYInt = int(GridSize.y);
	int totalCells = gridXInt * gridYInt;
	
	bool isTopRow = (cellID < gridXInt);
	bool isBottomRow = (cellID >= totalCells - gridXInt);
	bool isLeftColumn = (cellID % gridXInt == 0);
	bool isRightColumn = (cellID % gridXInt == gridXInt - 1);
	
	int[] nearbyCells = {-1, -1, -1, -1, cellID, -1, -1, -1, -1};
	
	if(!isTopRow){
		nearbyCells[1] = cellID - gridXInt;
		
		if(!isLeftColumn){
			nearbyCells[0] = cellID - gridXInt - 1;
		}
		
		if(!isRightColumn){
			nearbyCells[2] = cellID - gridXInt + 1;
		}
	}
	
	if(!isBottomRow){
		nearbyCells[7] = cellID + gridXInt;
		
		if(!isLeftColumn){
			nearbyCells[6] = cellID + gridXInt - 1;
		}
		
		if(!isRightColumn){
			nearbyCells[8] = cellID + gridXInt + 1;
		}
	}
	
	if(!isLeftColumn){
		nearbyCells[3] = cellID - 1;
	}
	
	if(!isRightColumn){
		nearbyCells[5] = cellID + 1;
	}
	
	return nearbyCells;
}



bool isAliveNextGen(int thisCell){
	int nearbyAliveCells = 0;
	bool aliveNextGen = false;
	
	int[] nearbyCells = getNearbyCells(thisCell);
	
	for(int i = 0; i < 9; i++){
		int cellID = nearbyCells[i];
		if(cellID == -1) continue;
		if(cellID == thisCell) continue;
		
		bool cellAlive = isCellAlive(cellID);
		if(cellAlive) nearbyAliveCells++;
	}
	
	NextGeneration[thisCell][15] = float(nearbyAliveCells);
	
	if(isCellAlive(thisCell)){
		if(nearbyAliveCells == 2 || nearbyAliveCells == 3){
			return aliveNextGen = true;
		}
		
		return aliveNextGen = false;
	}
		
		
	if(nearbyAliveCells == 3){
		return aliveNextGen = true;
	}

	return aliveNextGen;
}



void createCell(int cellID, bool aliveNextGen){
	if(aliveNextGen){
		NextGeneration[cellID][8] = ActiveColor.r;
		NextGeneration[cellID][9] = ActiveColor.g;
		NextGeneration[cellID][10] = ActiveColor.b;
		NextGeneration[cellID][12] = 1.0f;
	} else {
		NextGeneration[cellID][12] = 0.0f;
		NextGeneration[cellID][8] = InactiveColor.r;
		NextGeneration[cellID][9] = InactiveColor.g;
		NextGeneration[cellID][10] = InactiveColor.b;
	}
}




void main() {
	int thisCell = int(gl_GlobalInvocationID.x);
	
	bool aliveNextGen = isAliveNextGen(thisCell);
	createCell(thisCell, aliveNextGen);
}