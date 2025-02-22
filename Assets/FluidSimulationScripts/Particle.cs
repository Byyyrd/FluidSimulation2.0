using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Particle
{
    public Vector2 position = Vector3.zero;
    public Vector2 velocity = Vector3.zero;
    public Vector2 predictedPosition = Vector3.zero;
    public List<Particle> neighbours;
    public float density = 0;
    public Particle(Vector2 position, Vector2 velocity)
    {
        this.position = position;
        this.velocity = velocity;
    }
	public (int,int) GetIndexPair()
	{
        int x = (int)position.x + (int)Mathf.Ceil(CalculateParticals.BoundsSize.x / 2) + 1;
        int y = (int)position.y + (int)Mathf.Ceil(CalculateParticals.BoundsSize.y / 2) + 1;
        return (x,y);

	}
}
