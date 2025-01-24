using UnityEngine;

[CreateAssetMenu(fileName = "", menuName = "SO/" + nameof(GrassScriptableObject))]
public class GrassScriptableObject : ScriptableObject
{
    public string Id;
    public Mesh Mesh;
}
