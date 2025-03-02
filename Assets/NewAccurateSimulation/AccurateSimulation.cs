using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AccurateSimulation : MonoBehaviour
{
	/**
     * Sources:
     * braintruffle Youtube Video: https://youtu.be/sSJmUmCHAJY?si=IT2LoN023pVSxj3j
     * Sebastian Lague Youtube Video: https://youtu.be/rSKMYc1CQHE?si=Log6jBN0CZMbP09F
     * https://sph-tutorial.physics-simulation.org/pdf/SPH_Tutorial.pdf
     * 
     */
	private class Particle
    {
        
        public Vector2 position;
        public Vector2 velocity;
        public float density;
		public Particle(float x,float y)
		{
			position = new(x, y);
		}
    }
	[SerializeField] public float radius = .1f;
	[SerializeField] public float smoothingRadius = 1f;
	[SerializeField] public float targetDensity = 1f;
	[SerializeField] private float pressureForce = 1f;
	[SerializeField] private Vector2 externalForce;
	[SerializeField] private float viscosity = 1f;
	[SerializeField] private float nearDensityThreashhold = .2f;
	[SerializeField] private float collisionDamping = 0.95f;
	[Range(0, 1000)]
	[SerializeField] private int population = 100;
	[SerializeField] private Vector2 Bounds = new(10,10);

	private Vector2 testPosition = new();
	private Particle[] particles;
	[SerializeField] private TMP_Text display;
	// Start is called before the first frame update
	void Start()
    {
		particles = new Particle[population];
		CreateParticles();
		testPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
	}

    // Update is called once per frame
    void Update()
    {
		CreateParticles();
		for (int i = 0; i < population; i++)
		{
			Drawing.Instance.DrawCircle(particles[i].position.x, particles[i].position.y, radius);
		}
		if (Input.GetMouseButton(0))
		{
			testPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}
		display.text = $"Density: {CalculateDensityAtPosition(testPosition)}";
		Drawing.Instance.DrawCircle(testPosition.x, testPosition.y, smoothingRadius, new Color(0, 0, 1, .5f));

	}
	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(new(0, 0), Bounds);
	}
	
	private float CalculateDensityAtPosition(Vector2 position)
	{
		float mass = 1f;
		float density = 0;
		for(int i = 0; i < population; i++)
		{
			Particle particle = particles[i];
			float distance = (position - particle.position).magnitude;
			density += mass * W(distance,smoothingRadius);
		}

		return density;
	}

	private float W(float r, float h)
	{
		float q = (1 / h) * Mathf.Abs(r);
		float norm = 40 / (7 * Mathf.PI * Mathf.Pow(smoothingRadius, 2));
		if (0 <= q && q <= nearDensityThreashhold)
		{
			return (6 * (Mathf.Pow(q, 3) - Mathf.Pow(q, 2)) + 1) * norm;
		}
		else if (nearDensityThreashhold <= q && q <= 1)
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
		if (0 <= q && q <= nearDensityThreashhold)
		{
			return (18 * Mathf.Pow(q,2) - 12 * q) * norm;
		}
		else if (nearDensityThreashhold <= q && q <= 1)
		{
			return (-6 * Mathf.Pow(1 - q, 2)) * norm;
		}
		else
		{
			return 0;
		}
	}
	private float W2(float r, float h)
	{
		float q = (1 / h) * Mathf.Abs(r);
		float norm = 40 / (7 * Mathf.PI * Mathf.Pow(smoothingRadius, 2));
		if (0 <= q && q <= nearDensityThreashhold)
		{
			return (36 * q-12) * norm;
		}
		else if (nearDensityThreashhold <= q && q <= 1)
		{
			return (12 * (1 - q)) * norm;
		}
		else
		{
			return 0;
		}
	}
	void HandleCollisions(Particle particle)
	{;
		Vector2 vel = particle.velocity;
		Vector2 pos = particle.position;
		// Keep particle inside bounds
		Vector2 halfSize = Vector2.Scale(Bounds, new(0.5f, 0.5f));
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
	private void CreateParticles()
	{
		particles = new Particle[population];
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
			particles[i] = new(x - xOffset, y);
		}
	}
}
