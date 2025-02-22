using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class CalculateParticals : MonoBehaviour
{

    [Range(0, 1023)]
    [SerializeField] private int population;
    [SerializeField] private float radius;
    [SerializeField] private Vector2 boundsSize;
    [SerializeField] private float collisionDamping;
    [SerializeField] private float gravity;
    [SerializeField] private DensityCalculator dc;
    [SerializeField] private float gravityConstant = (float)(6.67 * Mathf.Pow(10, -11));
    [SerializeField] private float massOfEarth = (float)(5.972 * Mathf.Pow(10, 24));
    [SerializeField] private float radiusToEarth = (float)(6.371 * Mathf.Pow(10, 6));
    public static Vector2 BoundsSize;

    public Particle[] particles = new Particle[1023];
    private int particleIndex = 0;
    private Drawing graphics;
    public List<Particle>[,] grid;
    // Start is called before the first frame update
    void Start()
    {
        BoundsSize = boundsSize;
        graphics = Drawing.Instance;
        grid = new List<Particle>[(int)Mathf.Ceil((int)boundsSize.x / dc.smoothingRadius) + 2, (int)Mathf.Ceil((int)boundsSize.y / dc.smoothingRadius) + 2];
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = new List<Particle>();
            }

        }
		CreateRandomParticles();

		//gravity = (gravityConstant * massOfEarth) / Mathf.Pow(radiusToEarth,2);
	}

	// Update is called once per frame
	void Update()
    {
        //CreateParticles();
        Mesh mesh = new Mesh();
        mesh.Clear();
        Vector3[] vertices = new Vector3[particleIndex];
        
        for (uint i = 0; i < particleIndex; i++)
        {
            Particle particle = particles[i];
            particle.predictedPosition = particle.position + (new Vector2(0, gravity) + new Vector2(0, -gravity * Time.deltaTime) + particle.velocity) * Time.deltaTime;
        }
        for (uint i = 0; i < particleIndex; i++)
        {
			Particle particle = particles[i];
            particle.neighbours = GetNeigboringParticles(particle);
            dc.CalculateDensitie(particle);
        }
		foreach (List<Particle> partilceList in grid)
		{
			partilceList.Clear();
		}
		for (uint i = 0; i < particleIndex; i++)
        {
            Particle particle = particles[i];
			dc.CalculateVelocity(particle);
            particle.velocity += new Vector2(0, gravity);
            particle.velocity += new Vector2(0, -gravity * Time.deltaTime);
            particle.position += particle.velocity * Time.deltaTime;
            HandleCollisions(i);
            (int x, int y) = particle.GetIndexPair();
            //Debug.Log($"Position: {particle.position}, x: {x},y: {y}");
            grid[x, y].Add(particle);
            Color color = Color.white;
            if (particle.density > dc.targetDensity)
                color = Color.blue;
            if (particle.density < dc.targetDensity)
                color = Color.blue;
            graphics.DrawCircle(particle.position.x, particle.position.y, radius, color);
            vertices[i] = particle.position;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
    }
	private List<Particle> GetNeigboringParticles(Particle particle)
    {
        List<Particle> otherParticles = new();
		(int x, int y) = particle.GetIndexPair();
        (int, int)[] gridIndex = {
            (x - 1, y - 1), (x + 0, y - 1), (x + 1, y - 1),
            (x - 1, y + 0), (x + 0, y + 0), (x + 1, y + 0),
            (x - 1, y + 1), (x + 0, y + 1), (x + 1, y + 1),
        };
        foreach ((int i, int j) in gridIndex)
        {
            if (i >= 0 && i < grid.GetLength(0) && j >= 0 && j < grid.GetLength(1))
            {
               otherParticles.AddRange(grid[i, j]);
            }
        }
        return otherParticles;
    }
    private void CreateRandomParticles()
    {
        Array.Clear(particles, 0, particleIndex);
        for (int i = 0; i < population; i++)
        {
            particles[particleIndex] = new(new(UnityEngine.Random.Range(-boundsSize.x / 2, boundsSize.x/2), UnityEngine.Random.Range(-boundsSize.y / 2, boundsSize.y / 2)), Vector2.zero);
			(int x, int y) = particles[particleIndex].GetIndexPair();
			grid[x, y].Add(particles[particleIndex]);
            particleIndex++;
		}
    }

    private void CreateParticles()
    {
        Array.Clear(particles, 0, particleIndex);
        particleIndex = 0;
        int rowSize = Mathf.RoundToInt(Mathf.Sqrt(population));
        int rowIndex = 0;
        float xOrigin = -rowSize * radius;
        float yOrigin = rowSize * radius;
        for (int i = 0; i < population; i++)
        {
            if (i % rowSize == 0 && i != 0)
            {
                rowIndex++;
            }
            float y = yOrigin - rowIndex * radius * 2;
            float x = xOrigin + radius * 2 * i;
            float xOffset = rowSize * radius * 2 * rowIndex;
            particles[particleIndex++] = new(new(x - xOffset, y), Vector3.zero);
        }
    }
    private void CalculateCollision(Particle particle1,Particle particle2)
    {
        float distance = Vector3.Distance(particle1.position, particle2.position);
        if (distance < radius)
        {
            float angle1 = -Mathf.Atan2(particle1.position.y - particle2.position.y,particle1.position.x - particle2.position.x);
            particle1.velocity.x += Mathf.Cos(angle1);
            particle1.velocity.y += Mathf.Sin(angle1);

            float angle2 = -Mathf.Atan2( particle2.position.y - particle1.position.y , particle2.position.x - particle1.position.x ); ;
            particle1.velocity.x += Mathf.Cos(angle2);
            particle1.velocity.y += Mathf.Sin(angle2);
        }

    }
    
    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);
    }
    void HandleCollisions(uint particleIndex)
    {
        Particle particle = particles[particleIndex];
        Vector2 vel = particle.velocity;
        Vector2 pos = particle.position;
        pos.y -= radius / 2;

        // Keep particle inside bounds
        Vector2 halfSize = Vector2.Scale(boundsSize, new(0.5f,0.5f));
        Vector2 edgeDst = halfSize - new Vector2(Mathf.Abs(pos.x),Mathf.Abs(pos.y));

        if (edgeDst.x <= 0)
        {
            pos.x = halfSize.x * Mathf.Sign(pos.x);
            vel.x *= -1 * collisionDamping;
        }
        if (edgeDst.y <= 0)
        {
            pos.y = halfSize.y * Mathf.Sign(pos.y);
            vel.y *= -1 * collisionDamping;
        }


        pos.y += radius / 2;
        // Update position and velocity
        particle.position = pos ;
        particle.velocity = vel;
    }

}
