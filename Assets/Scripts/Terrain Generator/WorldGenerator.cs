using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	[Tooltip("Refreshes the loaded chunks with the new variables")]
	public bool refresh = false;
	[Tooltip("Randomises seed and refreshes")]
	public bool randomiseSeed = false;
	[Header("Terrain settings")]
	[Tooltip("How big the generated area is")]
	public int WorldSizeInChunks = 4;
	[Tooltip("How high the value has to be for it to be ground")]
	[Range(0,1)]
	public float terrainSurface = 0.5f;
	[Min(16)]
	[Tooltip("How wide the individual chunks are")]
	public int ChunkWidth = 16;
	[Tooltip("How tall the individual chunks are")]
	[Min(20)]
	public int ChunkHeight = 20;

	[Header("Generation settings")]
	[Tooltip("If the terrain should be smooth or 'blocky'")]
	public bool smoothTerrain = true;
	[Tooltip("The seed used generation (0 means random)")]
	public long seed = 0;


	[Tooltip("How often the change in height is")]
	[Range(0.1f, 1.5f)]
	public float noiseScale = 1;
	[Tooltip("How fine the detail is")]
	[Range(2, 8)]
	public int numOctaves = 5;
	[Range(1, 4)]
	[Tooltip("How big the details are")]
	public float weightMultiplier = 3;

	[Tooltip("How aggressive the details are")]
	[Range(1, 2.5f)]
	public float lacunarity = 2;
	[Tooltip("How filled the terrain is")]
	[Range(0.5f, 1.5f)]
	public float persistence = 0.8f;
	[Tooltip("How big the differences are between the highs and the lows")]
	[Min(0.5f)]
	public float noiseWeight = 6;
	[Tooltip("How high up the \"Floor\" is")]
	public float floorOffset = -2;

	private void Awake()
	{
		GameData.worldGenerator = this;
	}

	void Start()
	{
		CreateChunks();
		PopulateChunks();
	}

	void CreateChunks()
	{
		// Check that the instance is generated (Dumb way to fix an error I was getting)
		_ = GameData.Instance;
		// Create all chunks
		for (int x = 0; x < WorldSizeInChunks; x++)
		{
			for (int z = 0; z < WorldSizeInChunks; z++)
			{
				// in world space: X = X, Y = Z
				Vector2Int chunkPos = new Vector2Int(x * ChunkWidth, z * ChunkWidth);
				if (GameData.chunks.ContainsKey(chunkPos)) continue; // Skip the creation
				GameData.chunks.Add(chunkPos, new Chunk(chunkPos));
				if (GameData.chunks[chunkPos] == null) print("Failed to create chunk");
				GameData.chunks[chunkPos].chunkObject.transform.SetParent(transform);
			}
		}
	}

	private void Update()
	{
		if (randomiseSeed)
		{
			seed = 0;
		}
		if (refresh || randomiseSeed)
		{
			randomiseSeed = false;
			refresh = false;
			CreateChunks();
			PopulateChunks();
		}
	}

	private void PopulateChunks()
	{
		if (seed == 0)
		{
			seed = new System.Random().Next();
		}
		GameData data = GameData.Instance;
		List<Thread> threads = new List<Thread>();
		// Send off a thread for each chunk for generation
		foreach (var chunk in GameData.chunks)
		{
			Thread temp = new Thread(() =>
			{
				chunk.Value.CreateMeshData();
				//print($"Created mesh for {chunk.Key.x}, {chunk.Key.y}");
			});

			threads.Add(temp);
			temp.Start();
			temp.Name = string.Format("{0}, {1}", chunk.Key.x, chunk.Key.y);
		}

		// Wait for all threads to rejoin
		foreach (var thread in threads)
		{
			thread.Join();
			//print($"{thread.Name} rejoined");
		}

		// Build all meshes
		foreach (var chunk in GameData.chunks.Values)
		{
			//print($"Building chunk from Thread: {Thread.CurrentThread.Name}");
			chunk.BuildMesh();
		}

		Debug.Log(string.Format("{0} x {0} world generated.", (WorldSizeInChunks * ChunkWidth)));
	}
}
