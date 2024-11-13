using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MeshRenderer))]
public class SliceMaterialSetter : MonoBehaviour
{
    public Material sliceMat;

    private MeshRenderer m;

    // Start is called before the first frame update
    void Start()
    {
        m = GetComponent<MeshRenderer>();

        Assert.IsNotNull(sliceMat);
        m.material = new Material(sliceMat);
    }
}
