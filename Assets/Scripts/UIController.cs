using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}

	public void Refresh()
	{
		GameData.worldGenerator.refresh = true;
	}
	
	public void RandomiseSeed()
	{
		GameData.worldGenerator.randomiseSeed = true;
	}

	public void ChangeSeed(string newVal)
	{
		GameData.worldGenerator.seed = long.Parse(newVal);
	}

	public void ChangeWorldSize(string newVal)
	{
		GameData.worldGenerator.WorldSizeInChunks = int.Parse(newVal);
	}

	public void ChangeSmoothTerrain(bool newVal)
	{
		GameData.worldGenerator.smoothTerrain = newVal;
	}

	public void ChangeNoiseScale(float newVal)
	{
		GameData.worldGenerator.noiseScale = newVal;
	}
	public void ChangeNumOctave(float newVal)
	{
		GameData.worldGenerator.numOctaves = (int)newVal;
	}
}
