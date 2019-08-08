using System.IO;
using System.Text;
using UnityEngine;

public class PersistentStorage : MonoBehaviour
{
    string savePath;

    private void Awake() {
        savePath = Path.Combine(Application.persistentDataPath, "saveFile.txt");
    }

    public void Save (PersistableObject o, int version) {
        using (
            var writer = new BinaryWriter(File.Open(savePath, FileMode.Create), Encoding.UTF8)
        ) {
            writer.Write(-version);
            o.Save(new GameDataWriter(writer));
        }
    }

    public void Load (PersistableObject o) {
        using (
            var reader = new BinaryReader(File.Open(savePath, FileMode.Open), Encoding.UTF8)
        ) {
            o.Load(new GameDataReader(reader, -reader.ReadInt32()));
        }
    }
}
