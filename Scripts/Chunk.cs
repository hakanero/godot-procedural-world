using Godot;
using System;
using System.Threading.Tasks;

public class Chunk : StaticBody
{
	MeshInstance meshInstance;
	CollisionShape collisionShape;

	Color grassColor = new Color("36ea2f");
	Color sideColor = new Color("a58f4f");

	public float[,] heightMap;
	public Vector2 position;

	public float heim;
	public int vsiz;
	public int size;
	public DateTime lastVisited;

	[Signal] public delegate void Removed(Vector2 key);

	bool mesh_generated = false;
	bool coll_generated = false;
	public override void _Ready()
	{
		lastVisited = DateTime.Now;
		Translate(new Vector3(position.x*size, 0, position.y*size));
		PhysicsMaterialOverride = ResourceLoader.Load<PhysicsMaterial>("res://Materials/Terraip.tres");
		meshInstance = new MeshInstance()
		{
			MaterialOverride = ResourceLoader.Load<Material>("res://Materials/Terraib.tres")
		};
		collisionShape = new CollisionShape();
		AddChild(meshInstance);
		AddChild(collisionShape);
	}

	public async void Generate()
	{
		if (!IsQueuedForDeletion())
		{
			ArrayMesh mesh = await Task.Run(GenerateMesh);
			meshInstance.Mesh = mesh;
			mesh_generated = true;
		}
		if (!IsQueuedForDeletion())
		{
			Shape shape = await Task.Run(GenerateCollision);
			collisionShape.Shape = shape;
			coll_generated = true;
		}
	}

	float GetHeight(int x, int z) => heightMap[x + 1, z + 1] * heim;
	Task<ArrayMesh> GenerateMesh()
	{
		//Variables
		ArrayMesh mesh = new ArrayMesh();
		Vector3[] vertices = new Vector3[vsiz * vsiz];
		Vector3[] normals = new Vector3[vsiz * vsiz];
		Color[] colors = new Color[vsiz * vsiz];
		int[] indices = new int[size * size * 6];
		//Generate Mesh Data
		for (int z = 0, v = 0, t = 0; z < vsiz; z++)
		{
			for (int x = 0; x < vsiz; x++, v++)
			{
				vertices[v] = new Vector3(x, GetHeight(x, z), z);
				normals[v] = GetNormal(x, z);
				colors[v] = GetColor(normals[v]);
				if (x < size && z < size)
				{
					indices[t++] = v;
					indices[t++] = v + vsiz + 1;
					indices[t++] = v + vsiz;
					indices[t++] = v + vsiz + 1;
					indices[t++] = v;
					indices[t++] = v + 1;
				}
			}
		}
		//Create Mesh
		Godot.Collections.Array array = new Godot.Collections.Array();
		array.Resize((int)ArrayMesh.ArrayType.Max);
		array[(int)ArrayMesh.ArrayType.Vertex] = vertices;
		array[(int)ArrayMesh.ArrayType.Index] = indices;
		array[(int)ArrayMesh.ArrayType.Normal] = normals;
		array[(int)ArrayMesh.ArrayType.Color] = colors;
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);
		return Task.FromResult(mesh);
	}
	Task<Shape> GenerateCollision()
	{
		return Task.FromResult(meshInstance.Mesh.CreateTrimeshShape());
	}
	Vector3 GetNormal(int x, int z)
	{
		float rl = GetHeight(x + 1, z) - GetHeight(x - 1, z);
		float fb = GetHeight(x, z + 1) - GetHeight(x, z - 1);
		Vector3 normal = new Vector3(rl, 1, fb);
		return normal.Normalized();
	}
	Color GetColor(Vector3 normal)
	{
		float sa = normal.AngleTo(Vector3.Up);
		return ColorLerp(grassColor, sideColor, sa / (Mathf.Pi * 0.5f));
	}
	Color ColorLerp(Color from, Color to, float t)
	{
		float r = Mathf.Lerp(from.r, to.r, t);
		float g = Mathf.Lerp(from.g, to.g, t);
		float b = Mathf.Lerp(from.b, to.b, t);
		return new Color(r, g, b);
	}
	public void Visit()
	{
		lastVisited = DateTime.Now;
	}
	public override void _Process(float delta)
	{
		if(DateTime.Now.Subtract(lastVisited).TotalSeconds > 10)
		{
			if (coll_generated && mesh_generated)
			{
				QueueFree();
				EmitSignal(nameof(Removed), position);
			}
		}
	}
}
