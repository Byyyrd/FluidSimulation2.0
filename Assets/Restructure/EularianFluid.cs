using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem;

// Source: https://sph-tutorial.physics-simulation.org/pdf/SPH_Tutorial.pdf
// https://sph-tutorial.physics-simulation.org
public class EularianFluid : MonoBehaviour
{
	[Header("Grid Values")]

	[Range(0, 4000)][SerializeField] private int population;
	[Range(0, 1)][SerializeField] private float collisionDamping;
	[SerializeField] private Vector2 Boundaries;
	[SerializeField] private Vector2 gravity;
	private EularianParticle[] particles;
	private EularianParticle[] edgeParticles;
	private List<EularianParticle>[,] grid;

	[Header("Particle Values")]
	[SerializeField] private float radius;
	[SerializeField] private float smoothingRadius = 1f;
	[SerializeField] private float nearDensityThreashhold = .5f;
	[SerializeField] private float targetDensity = 1f;
	[SerializeField] private float pressureForce = 1f;
	[SerializeField] private float viscosityStrenght = 1f;

	[Header("Debug")]
	[SerializeField] private TMP_Text display;
	[SerializeField] private float speed;
	[SerializeField] private float force;
	[SerializeField] private float range;
	[SerializeField] private float mass = 1f;
	[SerializeField] private float edgeParticleDist = .125f;
	[SerializeField] private float densityError = 1f;
	[SerializeField] private float timeStep = 0.1f;

	private Vector2 externalForces = Vector2.zero;
	private Vector2 probePosition;
	private Drawing graphics;
	public static Vector2 Bounds;


	public void Start()
	{
		Bounds = Boundaries;
		grid = new List<EularianParticle>[(int)Mathf.Ceil(Boundaries.x) + 2, (int)Mathf.Ceil(Boundaries.y) + 2];
		for (int i = 0; i < grid.GetLength(0); i++)
		{
			for (int j = 0; j < grid.GetLength(1); j++)
			{
				grid[i, j] = new List<EularianParticle>();
			}

		}
		GenerateEdgeParticles();



		graphics = Drawing.Instance;
		CreateParticles();
		//foreach (EularianParticle particle in particles)
		//{
		//	particle.velocity = particle.position.normalized;
		//	particle.position += particle.velocity;
		//}

	}

	public void Update()
	{
		if (Time.deltaTime < 5f)
		{

			AnalyticalTools();
			foreach (EularianParticle particle in edgeParticles)
			{
				if (particle != null)
				{
					particle.neighbours = GetNeigboringParticles(particle);
					particle.predictedDensity = CalculateDensity(particle);
					particle.predictedVelocity = particle.velocity;
				}
			}
			foreach(EularianParticle particle in particles)
			{
				if (particle != null)
				{
					particle.neighbours = GetNeigboringParticles(particle);
					particle.predictedDensity = CalculateDensity(particle);

				}
			}
			//Calculate a_nonp -> extForces + viscosity 
 			foreach (EularianParticle particle in particles)
			{
				if (particle != null)
				{
					externalForces = Vector2.zero;
					if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
					{
						probePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
						Vector2 moveToMouse = probePosition - particle.position;
						if (moveToMouse.magnitude < range)
						{
							//particle.velocity = Vector2.zero;
							if (Input.GetMouseButton(1))
							{
								moveToMouse *= -1;
							}
							externalForces = moveToMouse.normalized * force - particle.velocity;
						}
					}
					particle.anonp = viscosityStrenght * CalculateViscosity(particle) + gravity + externalForces;
					particle.predictedVelocity = particle.velocity + Time.deltaTime * particle.anonp;
					if (float.IsNaN(particle.predictedVelocity.x))
					{
						Debug.Log(particle.predictedVelocity);
					}
				}
			}
			//Calculate pred.denisty and pressure
			foreach (EularianParticle particle in particles)
			{
				if (particle != null)
				{
					float sum = 0;
					foreach (EularianParticle neighbour in particle.neighbours)
					{
						Vector2 direction = (neighbour.position - particle.position).normalized;
						float distance = (neighbour.position - particle.position).magnitude;
						sum += Vector2.Dot((particle.predictedVelocity - neighbour.predictedVelocity),direction * W1(distance,smoothingRadius));
					}

					particle.predictedDensity = CalculateDensity(particle) + Time.deltaTime * sum;
					if (float.IsNaN(particle.predictedDensity))
					{
						Debug.Log(particle.predictedDensity);
					}
					particle.pressure = pressureForce * (particle.predictedDensity / targetDensity - 1);
				}
			}
			foreach (EularianParticle particle in particles)
			{
				if (particle != null)
				{
					particle.ap = CalculatePressure(particle);
					if(float.IsNaN(particle.ap.x) || float.IsNaN(particle.ap.y))
					{
						Debug.Log(particle.ap);
					}
				}
			}
			foreach (EularianParticle particle in particles)
			{
				particle.velocity = particle.predictedVelocity + Time.deltaTime * particle.ap;
				particle.position += speed * particle.velocity * Time.deltaTime;
				if (float.IsNaN(particle.position.x) || float.IsNaN(particle.position.y))
				{
					Debug.Log(particle.position);
				}
				HandleCollisions(particle);
				(int xIndex, int yIndex) = particle.GetIndexPair();
				grid[xIndex, yIndex].Add(particle);
				graphics.DrawCircle(particle.position.x, particle.position.y, radius, Color.blue);

			}

			foreach (EularianParticle particle in edgeParticles)
			{
				if (particle != null)
				{
					(int xIndex, int yIndex) = particle.GetIndexPair();
					grid[xIndex, yIndex].Add(particle);
				}
			}
		}

	}



	private Vector2 CalculatePressure(EularianParticle particle)
	{
		Vector2 pressureGradient = Vector2.zero;
		float pressure = particle.pressure;
		float particlePressureGradient = pressure / Mathf.Pow(particle.predictedDensity, 2);
		foreach (EularianParticle otherParticle in particle.neighbours)
		{
			if (otherParticle != null && otherParticle != particle)
			{
				Vector2 dir = (otherParticle.position - particle.position).normalized;
				float otherPressure = otherParticle.pressure;
				float otherParticlePressureGradient = otherPressure / Mathf.Pow(otherParticle.predictedDensity, 2);
				float distance = (otherParticle.position - particle.position).magnitude;
				pressureGradient += mass * (particlePressureGradient + otherParticlePressureGradient) * W1(distance, smoothingRadius) * dir;
				if(float.IsNaN(pressureGradient.x) || float.IsNaN(pressureGradient.y))
				{
					Debug.Log(otherPressure);
					Debug.Log(otherParticlePressureGradient);
					Debug.Log(distance);
				}

			}

		}
		return pressureGradient;
	}
	private float PressureFromDensity(float density)
	{
		//return pressureForce * (Mathf.Pow(density / targetDensity, 1) - 1);
		return pressureForce * (density - targetDensity);
	}
	private Vector2 CalculateViscosity(EularianParticle particle)
	{
		Vector2 viscosity = Vector2.zero;
		foreach (EularianParticle otherParticle in particle.neighbours)
		{
			if (otherParticle != null && otherParticle != particle)
			{
				float density = otherParticle.predictedDensity;
				float distance = (otherParticle.position - particle.position).magnitude;
				Vector2 A = otherParticle.velocity - particle.velocity;
				if (distance != 0)
				{
					viscosity += (mass / density) * A * ((2 * W1(distance, smoothingRadius)) / distance);

				}
			}

		}
		return -viscosity;
	}




	private float CalculateDensity(EularianParticle particle)
	{
		float density = 0;
		foreach (EularianParticle otherParticle in particle.neighbours)
		{
			if (otherParticle != null)
			{
				float dist = (otherParticle.position - particle.position).magnitude;
				float influence = W(dist, smoothingRadius);

				density += influence * mass;
			}

		}
		return density;
	}

	private float CalculateDensityAtPosition(Vector2 position)
	{
		float density = 0;
		float volume = (7 * Mathf.PI * Mathf.Pow(smoothingRadius, 2)) / 40;
		foreach (EularianParticle otherParticle in particles)
		{
			if (otherParticle != null)
			{
				float dist = (otherParticle.position - position).magnitude;
				float influence = W(dist, smoothingRadius);
				density += influence * mass;
			}

		}
		return density;
	}


	private float W(float r, float h)
	{
		float q = (1 / h) * Mathf.Abs(r);
		float norm = 40 / (7 * Mathf.PI * Mathf.Pow(smoothingRadius, 2));
		if (0 <= q && q <= 0.5)
		{
			return (6 * (Mathf.Pow(q, 3) - Mathf.Pow(q, 2)) + 1) * norm;
		}
		else if (0.5 <= q && q <= 1)
		{
			return (2 * Mathf.Pow(1 - q, 3)) * norm;
		}
		else
		{
			return 0;
		}
	}
	private float W1(float r, float h)
	{
		float q = (1 / h) * Mathf.Abs(r);
		float norm = 40 / (7 * Mathf.PI * Mathf.Pow(smoothingRadius, 2));
		if (0 <= q && q <= 0.5)
		{
			return (6 * q * (3 * q - 2)) * norm;
		}
		else if (0.5 <= q && q <= 1)
		{
			return (-6 * Mathf.Pow(1 - q, 2)) * norm;
		}
		else
		{
			return 0;
		}
	}

















	private List<EularianParticle> GetNeigboringParticles(EularianParticle particle)
	{
		List<EularianParticle> otherParticles = new();
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
		//otherParticles.Remove(particle);
		return otherParticles;
	}

	private void AnalyticalTools()
	{
		if (Input.GetMouseButtonDown(0))
		{
			probePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}
		display.text = $"Density: {CalculateDensityAtPosition(probePosition)}";
		graphics.DrawCircle(probePosition.x, probePosition.y, smoothingRadius, new Color(1, 0, 0, .5f));
	}


	public void OnDrawGizmos()
	{
#if UNITY_EDITOR
		if (!EditorApplication.isPlaying)
		{
			CreateParticles();
			foreach (EularianParticle particle in particles)
			{
				particle.velocity = particle.position.normalized;
				Gizmos.DrawSphere(new(particle.position.x, particle.position.y, 0), radius);
				Gizmos.DrawRay(particle.position, particle.velocity * radius);
			}
		}
		else
		{
			//foreach (EularianParticle particle in particles)
			//{
			//	Gizmos.DrawRay(particle.position, particle.velocity);
			//}
			//foreach (EularianParticle particle in edgeParticles)
			//{
			//	Gizmos.DrawRay(particle.position, particle.velocity);
			//}
		}
#endif
		Gizmos.DrawWireCube(Vector2.zero, Boundaries);
	}


	private void CreateParticles()
	{
		particles = new EularianParticle[population];
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
			particles[i] = new(new(x - xOffset + radius, y - radius), Vector3.zero);
#if UNITY_EDITOR
			if (EditorApplication.isPlaying)
			{
				(int xIndex, int yIndex) = particles[i].GetIndexPair();
				grid[xIndex, yIndex].Add(particles[i]);
			}
#else
			(int xIndex, int yIndex) = particles[i].GetIndexPair();
			grid[xIndex, yIndex].Add(particles[i]);
#endif
		}
	}
	private void CreateRandomParticles()
	{
		particles = new EularianParticle[population];
		for (int i = 0; i < population; i++)
		{
			particles[i] = new(new(UnityEngine.Random.Range(-Boundaries.x / 2 + 1, Boundaries.x / 2 - 1), UnityEngine.Random.Range(-Boundaries.y / 2 + 1, Boundaries.y / 2 - 1)), Vector2.zero);
			(int x, int y) = particles[i].GetIndexPair();
			grid[x, y].Add(particles[i]);
		}
	}
	private void GenerateEdgeParticles()
	{
		float xPos, yPos;
		float widthParticleCount = Boundaries.x * 2 * 1f / edgeParticleDist + 2;
		float heightParticleCount = Boundaries.y * 2 * 1f / edgeParticleDist + 2;
		edgeParticles = new EularianParticle[(int)(widthParticleCount + heightParticleCount) + 4];
		//Top and Bottom
		for (int i = 0; i < widthParticleCount; i += 2)
		{
			xPos = -Boundaries.x / 2 + (i / 2) * edgeParticleDist;
			yPos = -Boundaries.y / 2 - radius - nearDensityThreashhold / 3;
			NewParticle(ref edgeParticles, i, new(xPos, yPos), new(0, 1));
			NewParticle(ref edgeParticles, i + 1, new(xPos, -yPos), new(0, -1));
		}
		//Left and right
		for (int i = 0; i < heightParticleCount; i += 2)
		{
			int index = (int)widthParticleCount + i;
			xPos = -Boundaries.x / 2 - radius - nearDensityThreashhold / 3;
			yPos = -Boundaries.y / 2 + (i / 2) * edgeParticleDist;
			NewParticle(ref edgeParticles, index, new(xPos, yPos), new(1, 0));
			NewParticle(ref edgeParticles, index + 1, new(-xPos, yPos), new(-1, 0));
		}
		xPos = -Boundaries.x / 2 - radius - nearDensityThreashhold / 4;
		yPos = -Boundaries.y / 2 - radius - nearDensityThreashhold / 4;
		Vector2[] positions = { new(xPos, yPos), new(xPos, -yPos), new(-xPos, yPos), new(-xPos, -yPos) };
		Vector2[] velocities = { new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };
		for (int i = 0; i < 4; i++)
		{
			int index = (int)(widthParticleCount + heightParticleCount) + i;
			NewParticle(ref edgeParticles, index, positions[i], velocities[i]);
		}
	}
	private void NewParticle(ref EularianParticle[] array, int index, Vector2 position, Vector2 velocity)
	{
		array[index] = new EularianParticle(position, (velocity / 2 + gravity) * 0);
		(int x, int y) = array[index].GetIndexPair();
		grid[x, y].Add(array[index]);
	}
	void HandleCollisions(EularianParticle particle)
	{
		Vector2 vel = particle.velocity;
		Vector2 pos = particle.position;
		// Keep particle inside bounds
		Vector2 halfSize = Vector2.Scale(Boundaries, new(0.5f, 0.5f));
		Vector2 edgeDst = halfSize - new Vector2(Mathf.Abs(pos.x), Mathf.Abs(pos.y));

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
		// Update position and velocity
		particle.position = pos;
		particle.velocity = vel;
	}
}