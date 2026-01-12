using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShaderOven : MonoBehaviour
{
    public Button button;
    public ShaderVariantCollection shaders;
    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(Button_Click);
    }

    private void Button_Click()
    {
        Shader.WarmupAllShaders();
        shaders.WarmUp();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
