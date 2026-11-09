using System;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class PaintMaterial
    {
        public const string ShaderName="Standard (Specular setup)";
        public static Material Create(MeshRenderer renderer)
        {
            Shader shader=Shader.Find(ShaderName);
            if(shader==null || !shader.isSupported)throw new InvalidOperationException("Stock lit paint shader unavailable: "+ShaderName);
            var material=new Material(shader);
            // KSP strips Fade and highlight-off variants. The installed Transparent variant
            // performs its own RGB premultiplication; textures retain straight RGB/coverage.
            material.SetFloat("_Mode",3);
            material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);
            material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue=3001;
            material.SetColor("_Color",Color.white);
            // Zero specular reflectivity also prevents transparent regions retaining a sheen.
            material.SetColor("_SpecColor",Color.black);material.SetFloat("_Glossiness",0);
            material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
            renderer.receiveShadows=true;
            // The hull already casts the shadow; a lifted paint mesh must not cast another one.
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return material;
        }
    }
}
