using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
	/// <summary>
	/// Link to the world generator being used
	/// </summary>
	private WorldGenerator worldGenerator;
	/// <summary>
	/// The game object that this chunk is attached to
	/// </summary>
	public GameObject chunkObject;
	/// <summary>
	/// The mesh object used for drawing the terrain
	/// </summary>
	MeshFilter meshFilter;
	/// <summary>
	/// The collision used for interacting with the terrain 
	/// </summary>
	MeshCollider meshCollider;
	/// <summary>
	/// The render that the material for the terrain is attached to
	/// </summary>
	MeshRenderer meshRenderer;

	/// <summary>
	/// The position of the chunk with Y representing the Z coordinate in world
	/// </summary>
	Vector2Int chunkPosition2D;
	/// <summary>The position of the chunk</summary>
	public Vector3Int ChunkPostion
	{
		get
		{
			return new Vector3Int(chunkPosition2D.x, 0, chunkPosition2D.y);
		}
	}

	// Stores all the differences between the terrain generated and the current terrain
	public Dictionary<Vector3Int, byte> modifiedTerrain;

	/// <summary>The vertices for the terrain mesh</summary>
	private List<Vector3> vertices = new List<Vector3>();
	/// <summary>The Index order for the terrain mesh</summary>
	private List<int> triangles = new List<int>();

	public Chunk(Vector2Int position)
	{
		// Set the chunk position for later use
		chunkPosition2D = position;
		// Cache the world generator 
		worldGenerator = GameData.worldGenerator;
		// Create the game object for the mesh
		chunkObject = new GameObject
		{
			name = string.Format("Chunk {0}, {1}", position.x, position.y)
		};
		// Set the position of the chunk
		chunkObject.transform.position = ChunkPostion;

		// Add all the needed components to the game object
		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshCollider = chunkObject.AddComponent<MeshCollider>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();

		// Read the material from memory and load it into the renderer
		meshRenderer.material = Resources.Load<Material>("Materials/Terrain");
		// Set tag for later use
		chunkObject.transform.tag = "Terrain";
		// Generate dictionary for changed terrain
		modifiedTerrain = new Dictionary<Vector3Int, byte>();

		// Moved out of this function for multi-threading
		//CreateMeshData();
	}

	public void CreateMeshData()
	{
		// Make sure that there is no pre-existing mesh
		ClearMeshData();

		// Loop through each "cube" in our terrain.
		for (int x = 0; x < worldGenerator.ChunkWidth; x++)
		{
			for (int y = 0; y < worldGenerator.ChunkHeight; y++)
			{
				for (int z = 0; z < worldGenerator.ChunkWidth; z++)
				{
					// Pass the value into our MarchCube function.
					MarchCube(new Vector3Int(x, y, z));
				}
			}
		}

		// Moved out of this function for multi-threading
		//BuildMesh();
	}

	void MarchCube(Vector3Int position)
	{
		// Sample terrain values at each corner of the cube.
		float[] cube = new float[8];
		for (int i = 0; i < 8; i++)
		{
			// Get the values for all the point in the cube that are needed
			cube[i] = SampleTerrain(position + GameData.CornerTable[i]);
		}

		// Get the configuration index of this cube using the points
		int configIndex = GetCubeConfiguration(cube);

		// If the configuration of this cube is 0 or 255 (completely inside the terrain or completely outside of it) we don't need to do anything
		if (configIndex == 0 || configIndex == 255)
			return;

		// Loop through the triangles. There are never more than 5 triangles to a cube and only three vertices to a triangle
		int edgeIndex = 0;
		for (int i = 0; i < 5; i++)
		{
			for (int p = 0; p < 3; p++)
			{

				// Get the current indice. We increment triangleIndex through each loop
				int indice = GameData.TriangleTable[configIndex, edgeIndex];

				// -1 in the TriangleTable represents no more triangles in it so exit
				if (indice == -1)
					return;

				// Get the vertices for the start and end of this edge
				Vector3 vert1 = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 0]];
				Vector3 vert2 = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 1]];

				// The position of the mid point vertex
				Vector3 vertPosition;

				// If smoothTerrain is on calculate surface is based off of the noise values
				if (worldGenerator.smoothTerrain)
				{
					// Get the terrain values at either end of our current edge from the cube array created above.
					float vert1Sample = cube[GameData.EdgeIndexes[indice, 0]];
					float vert2Sample = cube[GameData.EdgeIndexes[indice, 1]];

					// Calculate the difference between the terrain values
					float difference = vert2Sample - vert1Sample;

					// If the difference is 0, then the terrain passes through the middle
					if (difference == 0)
						difference = worldGenerator.terrainSurface;
					else
						difference = (worldGenerator.terrainSurface - vert1Sample) / difference;

					// Calculate the point along the edge that passes through
					vertPosition = vert1 + ((vert2 - vert1) * difference);
				}
				// If smoothTerrain is off then just assume that the surface is directly between the two vertex
				else
				{
					vertPosition = (vert1 + vert2) / 2f;
				}

				// Add new triangles to list
				triangles.Add(VertForIndice(vertPosition));
				// Index for next edge
				edgeIndex++;
			}
		}
	}

	/// <summary>
	/// Converts an array of values to a index on the configuration array
	/// </summary>
	/// <param name="cube">The value of the cube</param>
	/// <returns>The index for the configuration</returns>
	int GetCubeConfiguration(float[] cube)
	{
		// Starting with a configuration of zero, loop through each point in the cube and check if it is below the terrain surface
		int configurationIndex = 0;
		for (int i = 0; i < 8; i++)
		{

			// Set bit-mask depending on what values are above surface
			if (cube[i] > worldGenerator.terrainSurface)
				configurationIndex |= 1 << i;

		}

		return configurationIndex;
	}

	/// <summary>
	/// Fills in the terrain at a single point
	/// </summary>
	/// <param name="pos">The point that it is filling in the terrain</param>
	public void PlaceTerrain(Vector3 pos)
	{
		Vector3Int v3Int = new Vector3Int(Mathf.CeilToInt(pos.x), Mathf.CeilToInt(pos.y), Mathf.CeilToInt(pos.z));
		v3Int -= ChunkPostion;
		modifiedTerrain[v3Int] = byte.MaxValue;
		CreateMeshData();
	}

	/// <summary>
	/// Removes terrain at a single point
	/// </summary>
	/// <param name="pos">The point that it is removing the terrain at</param>
	public void RemoveTerrain(Vector3 pos)
	{
		Vector3Int floorPos = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
		floorPos -= ChunkPostion;
		modifiedTerrain[floorPos] = byte.MinValue;
		CreateMeshData();
	}

	/// <summary>
	/// Get the terrain value at a single point
	/// </summary>
	/// <param name="point">The point of the value that is being requested</param>
	/// <returns>The value for the terrain</returns>
	float SampleTerrain(Vector3Int point)
	{
		//Vector3Int worldPos = point + ChunkPostion;
		Vector3Int worldPos = point + ChunkPostion;
		return GameData.Instance.GetTerrainValue(worldPos);
	}

	/// <summary>
	/// Converts the vertex into an index for the configuration array
	/// </summary>
	/// <param name="vert">The vertex that is being converted</param>
	/// <returns>The index for the vertex</returns>
	int VertForIndice(Vector3 vert)
	{
		// Loop through all the vertices currently in the vertices list
		for (int i = 0; i < vertices.Count; i++)
		{
			// If we find a vert that matches ours, then simply return this index
			if (vertices[i] == vert)
				return i;
		}

		// If we didn't find a match, add this vert to the list and return last index
		vertices.Add(vert);
		return vertices.Count - 1;

	}

	/// <summary>
	/// Removes all the mesh data
	/// </summary>
	public void ClearMeshData()
	{
		vertices.Clear();
		triangles.Clear();
	}

	public void BuildMesh()
	{
		// Create new mesh
		Mesh mesh = new Mesh();
		// Assign values to new mesh
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		// Tell the mesh to calculate its own normals
		mesh.RecalculateNormals();
		// Set the game object to use the new mesh
		meshFilter.mesh = mesh;
		// Set the collider to use the new mesh
		meshCollider.sharedMesh = mesh;
	}
}
