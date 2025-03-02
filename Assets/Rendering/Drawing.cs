using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
public class Drawing : MonoBehaviour
{
    private static Drawing _Instance;
    public static Drawing Instance
    {
        get
        {
            if (!_Instance)
            {
                _Instance = new GameObject().AddComponent<Drawing>();
                _Instance.name = _Instance.GetType().ToString();
                DontDestroyOnLoad(_Instance.gameObject);
            }
            return _Instance;
        }
    }

    [SerializeField] private Material material;

    private List<Matrix4x4[]> matrices = new ();
    private int matrixIndex = 0;

    private List<Vector4[]> colors = new ();
    private int colorIndex = 0;
    
    private MaterialPropertyBlock block;
    private Mesh mesh;

    
    
    public void DrawCircle(float x,float y,float radius,Color? color = null)
    {

        color ??= Color.white;
        Vector3 position = new Vector3(x,y);
        Quaternion rotation = Quaternion.identity;
        Vector3 scale = 2 * radius * Vector3.one;
        Matrix4x4 mat = Matrix4x4.TRS(position,rotation,scale);

        

        matrices.Last()[matrixIndex++] = mat;
        colors.Last()[colorIndex++] = (Vector4)color;
        if(matrixIndex >= 1022)
        {
            matrixIndex = 0;
            matrices.Add(new Matrix4x4[1022]);
        }
		if (colorIndex >= 1022)
		{
			colorIndex = 0;
			colors.Add(new Vector4[1022]);
		}


    }
    private void Setup()
    {
        //TODO: Better alternative for loading Material
        matrices.Add(new Matrix4x4[1022]);
        colors.Add(new Vector4[1022]);
        material = Resources.Load("ParticleMaterial") as Material;
        mesh = CreateQuad();
        material.enableInstancing = true;
        block = new MaterialPropertyBlock();
    }
	
	private Mesh CreateQuad(float width = 1f, float height = 1f)
    {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        var tris = new int[6] {
            // lower left tri.
            0, 2, 1,
            // lower right tri
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    private void Awake()
    {
        Setup();
    }

    private void Update()
    {
        // Draw a bunch of meshes each frame.
        int i = 0;
        foreach (Matrix4x4[] matrixArray in matrices)
        {
			//Set Colors in Shader to use with instance Id
			block.SetVectorArray("_Colors", colors[i]);
			Graphics.DrawMeshInstanced(mesh, 0, material, matrixArray, matrixArray.Length, block);
            i++;
        }

        //Cleanup
        matrices = new();
        colors = new();
		matrices.Add(new Matrix4x4[1022]);
		colors.Add(new Vector4[1022]);
		colorIndex = 0;
        matrixIndex = 0;
        block.SetVectorArray("_Colors", colors.First());
    }
}