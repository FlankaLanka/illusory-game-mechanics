using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ClonableObjectSliceMaterialSetter : MonoBehaviour
{
    public Material sliceMat;

    public Vector3 randColorLowerBound;
    public Vector3 randColorUpperBound;

    private MeshRenderer m;

    // Start is called before the first frame update
    void Start()
    {
        m = GetComponent<MeshRenderer>();
        Assert.IsNotNull(sliceMat);
        m.material = new Material(sliceMat);

        Color randColor = new Color(Random.Range(randColorLowerBound.x, randColorUpperBound.x),
                                    Random.Range(randColorLowerBound.y, randColorUpperBound.y),
                                    Random.Range(randColorLowerBound.z, randColorUpperBound.z));
        m.material.SetColor("_MaterialColor", randColor);
        m.material.SetColor("_CloneColor", randColor);

        //m.material.SetColor("_MaterialColor", Color.red);
        //m.material.SetColor("_CloneColor", Color.green);
    }
}
