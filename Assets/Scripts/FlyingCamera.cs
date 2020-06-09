using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingCamera : MonoBehaviour
{
	/// <summary>
	/// The camera that this script is moving
	/// </summary>
	public Camera cam;
	/// <summary>
	/// The script that is controlling the terrain
	/// </summary>
	public WorldGenerator world;

	private void Update()
	{
		// Movement
		transform.position = Vector3.MoveTowards(
			transform.position, 
			transform.position + (cam.transform.forward * Input.GetAxis("Vertical")) 
			+ (transform.right * Input.GetAxis("Horizontal")), Time.deltaTime * 10f);
		transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0));
		cam.transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y"), 0, 0));

		// Place terrain on click
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));

			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				// If they clicked on terrain
				if (hit.transform.CompareTag("Terrain"))
				{
					// Get the chunk
					Chunk clickedOn = GameData.Instance.GetChunkFromPos(hit.transform.position);
					// And place block in it
					clickedOn.PlaceTerrain(hit.point);
					print($"Hit {hit.point} and reacted with {new Vector3Int(Mathf.CeilToInt(hit.point.x), Mathf.CeilToInt(hit.point.y), Mathf.CeilToInt(hit.point.z))}");
				}
			}
		}

		// Remove terrain on click
		if (Input.GetMouseButtonDown(1))
		{
			Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));

			if (Physics.Raycast(ray, out RaycastHit hit))
			{

				if (hit.transform.CompareTag("Terrain"))
					GameData.Instance.GetChunkFromPos(hit.transform.position).RemoveTerrain(hit.point);

			}

		}

	}

}
