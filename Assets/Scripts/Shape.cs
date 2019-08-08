﻿using UnityEngine;

public class Shape : PersistableObject {
    int shapeId = int.MinValue;

    public int ShapeId {
        get {
            return shapeId;
        }
        set {
            if (shapeId == int.MinValue && value != int.MinValue) {
                shapeId = value;
            } else {
                Debug.Log("Not allowed to change shapeId.");
            }
        }
    }

    public int MaterialId { get; private set; }

    public void SetMaterial (Material material, int materialId) {
        meshRenderer.material = material;
        MaterialId = materialId;
    }

    Color color;
    static int colorPropertyId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock sharedPropertyBlock;

    public void SetColor (Color color) {
        this.color = color;
        //to prevent creating a new material each time we set a color:
        if (sharedPropertyBlock == null) {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    MeshRenderer meshRenderer;

    private void Awake() {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public override void Save(GameDataWriter writer) {
        base.Save(writer);
        writer.Write(color);
    }

    public override void Load(GameDataReader reader) {
        base.Load(reader);
        SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
    }
}
