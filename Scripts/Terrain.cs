using Godot;
using System.Collections.Generic;
using System;

public class Terrain : Spatial
{
	[Export] readonly OpenSimplexNoise noise;
	[Export] readonly Curve curve;
	[Export] readonly int chunkSize = 32;
	[Export] readonly int chunksAround = 16;
	[Export] readonly float heightMultiplier = 64;
	[Export] readonly float sampleSize = 0.5f;
	[Export] readonly NodePath playerNode;
	readonly List<Vector2> chunksToGenerate = new List<Vector2>();
	readonly List<Vector2> chunksToDelete = new List<Vector2>();
	readonly Dictionary<Vector2, NodePath> chunks = new Dictionary<Vector2, NodePath>();
	readonly ChunkLoader chunkLoader = new ChunkLoader();
	KinematicBody player;
	Vector2 newPlayerCPos = new Vector2(0, 0);
	Vector2 playerCPos = new Vector2(10000, 10000);
	int vertexSize;
	public override void _Ready()
	{
		noise.Seed = Mathf.RoundToInt(GD.Randf()*100000);
		player = GetNode<KinematicBody>(playerNode);
		vertexSize = chunkSize + 1;
	}

	float[,] GenerateHeightmap(Vector2 p)
	{
		float[,] heightMap = new float[vertexSize+2, vertexSize+2];
		float height;
		for (int z = -1; z < vertexSize + 1; z++)
			for (int x = -1; x < vertexSize + 1; x++)
			{
				height = noise.GetNoise2d((x + p.x) * sampleSize, (z + p.y) * sampleSize);
				height = (height + 1) / 2;
				height = curve.Interpolate(height);
				heightMap[x + 1, z + 1] = height;
			}
		return heightMap;
	}
	void GenerateChunk(Vector2 p)
	{
		Chunk c = new Chunk()
		{
			position = p,
			heightMap = GenerateHeightmap(p*chunkSize),
			heim = heightMultiplier,
			size = chunkSize,
			vsiz = vertexSize,
			Name = p.ToString()
		};
		AddChild(c);
		c.Connect("Removed", this, nameof(ChunkRemoved));
		chunks.Add(p,c.GetPath());
		chunkLoader.AddToQueue(new Action(c.Generate));
	}

	void GetPlayerCPos()
	{
		newPlayerCPos.x = Mathf.Floor(player.Transform.origin.x / chunkSize);
		newPlayerCPos.y = Mathf.Floor(player.Transform.origin.z / chunkSize);
		if(newPlayerCPos != playerCPos)
		{
			playerCPos = newPlayerCPos;
			GenerateChunks();
		}
	}
	void GenerateChunks()
	{
		Vector2 v1, v2, v3, v4;
		chunksToGenerate.Clear();
		for (int i = 0; i <= chunksAround; i++)
		{
			for (int j = 0; j <= i; j++)
			{
				v1 = new Vector2(i - j, j) + playerCPos;
				v2 = new Vector2(j - i, j) + playerCPos;
				v3 = new Vector2(i - j, -j) + playerCPos;
				v4 = new Vector2(j - i, -j) + playerCPos;
				if (!chunksToGenerate.Contains(v1)) chunksToGenerate.Add(v1);
				if (!chunksToGenerate.Contains(v2)) chunksToGenerate.Add(v2);
				if (!chunksToGenerate.Contains(v3)) chunksToGenerate.Add(v3);
				if (!chunksToGenerate.Contains(v4)) chunksToGenerate.Add(v4);
			}
		}
		foreach (Vector2 key in chunksToGenerate)
		{
			if (chunks.ContainsKey(key))
			{
				Chunk c = GetNode<Chunk>(chunks[key]);
				if (c.Visible == false) c.Visible = true;
			}
			else
			{
				GenerateChunk(key);
			}
		}

		foreach (Vector2 key in chunks.Keys)
		{
			Chunk c = GetNode<Chunk>(chunks[key]);
			if (!chunksToGenerate.Contains(key))
				c.Visible = false;
		}
	}

	void ChunkRemoved(Vector2 key)
	{
		chunks.Remove(key);
	}

	void VisitChunks()
	{
		foreach(Vector2 key in chunksToGenerate)
		{
			if (chunks.ContainsKey(key))
			{
				GetNode<Chunk>(chunks[key]).Visit();
			}
		}
	}

	public override void _Process(float delta)
	{
		GetPlayerCPos();
		VisitChunks();
		chunkLoader.Next();
		GD.Print(Engine.GetFramesPerSecond());
	}
}
class ChunkLoader
{
	readonly List<Action> queue = new List<Action>();
	bool working = false;
	public void AddToQueue(Action func) => queue.Add(func);
	public void Next()
	{
		if(!working && queue.Count != 0)
		{
			working = true;
			queue[0].Invoke();
			queue.RemoveAt(0);
			working = false;
		}
	}
}
