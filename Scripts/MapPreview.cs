using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour {
  public Renderer textureRenderer;
  public MeshFilter meshFilter;
  public MeshRenderer meshRenderer;
  public MeshCollider meshCollider;

  public enum DrawMode {
    NoiseMap,
    Mesh,
    FalloffMap,
  }
  public DrawMode drawMode;

  public MeshSettings meshSettings;
  public HeightMapSettings heightMapSettings;

  public ErosionSettings erosionSettings;
  public TextureData textureData;

  public Material terrainMaterial;

  [Range(0, MeshSettings.numSupportedLODs - 1)]
  public int editorLOD;
  public bool autoUpdate;
  public bool hideOnStart;

  float[,] falloffMap;
  private HeightMap heightMap;
  void Start() {
    if (hideOnStart) {
      gameObject.SetActive(false);
    }
  }
  public void DrawMapInEditor() {
    int mapSize = meshSettings.numVertsPerLine;
    int mapSizeWithBorder = mapSize + erosionSettings.brushRadius * 2;
    float[] values = HeightMapGenerator.GenerateHeightMap(mapSize, erosionSettings, heightMapSettings, Vector2.zero);
    Erosion erosion = FindObjectOfType<Erosion>();
    if (heightMapSettings.useErosion) {
      values = HeightMapGenerator.ApplyErosionAndHeightMultiplier(values, mapSize, erosion, erosionSettings, heightMapSettings);
    }

    heightMap = HeightMapGenerator.HeightMapForValues(values, mapSizeWithBorder);

    if (drawMode == DrawMode.NoiseMap) {
      DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
    } else if (drawMode == DrawMode.Mesh) {
      DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLOD));
    } else if (drawMode == DrawMode.FalloffMap) {
      DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
    }
  }

  public void SpawnVegetation() {
    VegetationSpawner vegSpawn = FindObjectOfType<VegetationSpawner>();
    vegSpawn.Spawn(transform, heightMap.values);
  }

  public void DrawTexture(Texture2D texture) {
    textureRenderer.sharedMaterial.SetTexture("_MainTex", texture);
    textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

    textureRenderer.gameObject.SetActive(true);
    meshFilter.gameObject.SetActive(false);
  }
  public void DrawMesh(MeshData meshData) {
    Mesh mesh = meshData.CreateMesh();
    meshFilter.sharedMesh = mesh;
    meshCollider.sharedMesh = mesh;

    textureRenderer.gameObject.SetActive(false);
    meshFilter.gameObject.SetActive(true);
  }

  void OnValuesUpdated() {
    if (!Application.isPlaying) {
      DrawMapInEditor();
    }
  }

  void OnTextureValuesUpdated() {
    textureData.ApplyToMaterial(terrainMaterial);
  }

  void OnValidate() {
    if (meshSettings != null) {
      meshSettings.OnValuesUpdated -= OnValuesUpdated;
      meshSettings.OnValuesUpdated += OnValuesUpdated;
    }
    if (heightMapSettings != null) {
      heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
      heightMapSettings.OnValuesUpdated += OnValuesUpdated;
    }
    if (textureData != null) {
      textureData.OnValuesUpdated -= OnTextureValuesUpdated;
      textureData.OnValuesUpdated += OnTextureValuesUpdated;
    }
  }
}
