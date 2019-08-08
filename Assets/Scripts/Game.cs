using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : PersistableObject
{
    public static Game Instance { get; private set; }

    const int saveVersion = 2;
    string savePath;
    [SerializeField] PersistentStorage storage;

    [SerializeField] ShapeFactory shapeFactory;
    List<Shape> shapes;

    [SerializeField] KeyCode createKey = KeyCode.C;
    [SerializeField] KeyCode newGameKey = KeyCode.N;
    [SerializeField] KeyCode saveKey = KeyCode.S;
    [SerializeField] KeyCode loadKey = KeyCode.L;
    [SerializeField] KeyCode destroyKey = KeyCode.X;

    [SerializeField] int levelCount;
    int loadedLevelBuildIndex;

    float creationProgress;
    public float CreationSpeed { get; set; }
    float destructionProgress;
    public float DestructionSpeed { get; set; }

    public SpawnZone SpawnZoneOfLevel { get; set; }

    private void OnEnable() {
        Instance = this;
    }

    private void Start() {
        shapes = new List<Shape>();

        if (Application.isEditor) {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("Level ")) {
                    SceneManager.SetActiveScene(loadedScene);
                    loadedLevelBuildIndex = loadedScene.buildIndex;
                    return;
                }
            }
        }

        StartCoroutine(LoadLevel(1));
    }

    private void Update() {
        if (Input.GetKeyDown(createKey)) {
            CreateShape();
        }
        else if (Input.GetKeyDown(newGameKey)) {
            BeginNewGame();
        }
        else if (Input.GetKeyDown(saveKey)) {
            storage.Save(this, saveVersion);
        }
        else if (Input.GetKeyDown(loadKey)) {
            BeginNewGame();
            storage.Load(this);
        }
        else if (Input.GetKeyDown(destroyKey)) {
            DestroyShape();
        }
        else {
            for (int i = 0; i <= levelCount; i++) {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
                    BeginNewGame();
                    StartCoroutine(LoadLevel(i));
                    return;
                }
            }
        }

        //creation process
        creationProgress += Time.deltaTime * CreationSpeed;
        while (creationProgress >= 1f) {
            creationProgress -= 1f;
            CreateShape();
        }

        //destruction process
        destructionProgress += Time.deltaTime * DestructionSpeed;
        while (destructionProgress >= 1f) {
            destructionProgress -= 1f;
            DestroyShape();
        }
    }

    private void CreateShape() {
        Shape instance = shapeFactory.GetRandom();
        Transform t = instance.transform;
        t.localPosition = SpawnZoneOfLevel.SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);
        instance.SetColor(Random.ColorHSV(
            hueMin: 0f, hueMax: 1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin: 0.25f, valueMax: 1f,
            alphaMin: 1f, alphaMax: 1f
        ));
        shapes.Add(instance);
    }

    private void BeginNewGame() {
        for (int i = 0; i < shapes.Count; i++) {
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    public override void Save(GameDataWriter writer) {
        /** our first version had a positive shape count as it's first value, 
         * not version.
         * in order to be backwards compatible, we'll be able to identify if a
         * file has a version by using a negative number to store it */
        writer.Write(shapes.Count);
        writer.Write(loadedLevelBuildIndex);
        for (int i = 0; i < shapes.Count; i++) {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader) {
        int version = reader.Version;
        if (version > saveVersion) {
            /* saveVersion makes sure that we never load a game saved in the future with
             * past code */
            Debug.Log("Unsupported future save version " + version);
            return;
        }

        int count = version > 0 ? reader.ReadInt() : -version;
        StartCoroutine(LoadLevel(version < 2 ? 1 : reader.ReadInt()));
        for (int i = 0; i < count; i++) {
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactory.Get(shapeId, materialId);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }

    void DestroyShape() {
        if (shapes.Count > 0) {
            int index = Random.Range(0, shapes.Count);
            /* we don't care about the order of the shapes and we want to 
             * efficiently remove the target shape, so we copy the last object on
             * the list to the location of the target shape, and then remove
             * the last object. */
            shapeFactory.Reclaim(shapes[index]);
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    IEnumerator LoadLevel(int levelBuildIndex) {
        enabled = false;
        if (loadedLevelBuildIndex > 0) {
            //we already have a loaded scene we need to unload
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }
        yield return SceneManager.LoadSceneAsync(
            levelBuildIndex, LoadSceneMode.Additive
        ); ;
        SceneManager.SetActiveScene(
            SceneManager.GetSceneByBuildIndex(levelBuildIndex)
        );
        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }
}
