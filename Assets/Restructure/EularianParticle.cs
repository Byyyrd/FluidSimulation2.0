using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EularianParticle
{
	public Vector2 position = Vector2.zero;
	public Vector2 velocity = Vector2.zero;
	public Vector2 predictedPosition = Vector2.zero;
	public List<EularianParticle> neighbours;
	public float density = 0;
	public Vector2 pressure = Vector2.zero;
	public EularianParticle(Vector2 position, Vector2 velocity)
	{
		this.position = position;
		this.velocity = velocity;
		this.predictedPosition = position;
	}
	public (int, int) GetIndexPair()
	{
		int x = (int)(position.x + Mathf.Ceil(EularianFluid.Bounds.x / 2)) + 1;
		int y = (int)(position.y + Mathf.Ceil(EularianFluid.Bounds.y / 2)) + 1;
		return (x, y);

	}
	public (int, int) GetPredictedIndexPair()
	{
		int x = (int)((position.x + velocity.x * Time.deltaTime) + Mathf.Ceil(EularianFluid.Bounds.x / 2)) + 1;
		int y = (int)((position.y + velocity.y * Time.deltaTime) + Mathf.Ceil(EularianFluid.Bounds.y / 2)) + 1;
		return (x, y);

	}
}
