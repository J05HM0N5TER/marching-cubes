using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTerrain : MonoBehaviour
{
	private bool isDirty = true;
	private const long noiseSeed = 1;
	private Noise.OpenSimplex2S noise;
	private const uint terrainSize = 30;
	private const float scale = 5;
	private double[,,] terrainValues = new double[terrainSize, terrainSize, terrainSize];
	// Start is called before the first frame update
	void Start()
	{
		SetUp();
	}

	private void SetUp()
	{
		noise = new Noise.OpenSimplex2S(noiseSeed);
		GenerateTerrain();
	}

	// Update is called once per frame
	void Update()
	{
		if (isDirty)
			SetUp();
	}

	private void GenerateTerrain()
	{
		isDirty = false;
		for (int z = 0; z < terrainSize; z++)
		{
			float scaledZ = z / scale;
			for (int y = 0; y < terrainSize; y++)
			{
				float scaledY = y / scale;
				for (int x = 0; x < terrainSize; x++)
				{
					float scaledX = x / scale;

					terrainValues[x, y, z] = noise.Noise3_XYBeforeZ(scaledX, scaledY, scaledZ);
				}
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (isDirty)
			SetUp();
		for (int z = 0; z < terrainSize; z++)
		{
			for (int y = 0; y < terrainSize; y++)
			{
				for (int x = 0; x < terrainSize; x++)
				{
					float current = (float)terrainValues[x, y, z];
					Gizmos.color = new Color(current, current, current, 0.5f);
					Gizmos.DrawSphere(new Vector3(x, y, z), 0.3f);
				}
			}
		}
	}
}
