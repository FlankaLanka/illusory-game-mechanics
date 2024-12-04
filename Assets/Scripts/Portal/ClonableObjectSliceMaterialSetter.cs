using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ClonableObjectSliceMaterialSetter : MonoBehaviour
{
    public Material sliceMat;

    private MeshRenderer m;

    // Start is called before the first frame update
    void Start()
    {
        m = GetComponent<MeshRenderer>();
        Assert.IsNotNull(sliceMat);
        m.material = new Material(sliceMat);
        Color randColor = new Color(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), Random.Range(0.5f, 1f));
        m.material.SetColor("_MaterialColor", randColor);

        //uncomment this line to set clone's color to be different for debug
        //randColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        m.material.SetColor("_CloneColor", randColor);

        //for player we dont want back faces otherwise camera will block our view when entering portal
        if(gameObject.tag == "Player")
        {
            m.material.SetInt("_BUILTIN_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
        }
    }
}
