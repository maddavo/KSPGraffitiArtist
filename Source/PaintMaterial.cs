using System;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class PaintMaterial
    {
        public const string ShaderName="KSP/Alpha/Cutoff";
        public const float AlphaCutoff=.15f;
        public static Material Create(MeshRenderer renderer)
        {
            Shader shader=Shader.Find(ShaderName);
            if(shader==null || !shader.isSupported)throw new InvalidOperationException("Stock lit paint shader unavailable: "+ShaderName);
            var material=new Material(shader);
            material.renderQueue=2451;
            material.SetFloat("_Cutoff",AlphaCutoff);
            if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);
            renderer.receiveShadows=true;
            // The hull already casts the shadow; a lifted paint mesh must not cast another one.
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return material;
        }
    }
}
