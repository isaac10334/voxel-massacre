Shader "Custom/InvisibleWalls" {
  Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
    _Detail ("Detail (RGB) Gloss (A)", 2D) = "gray" {}
  }

  SubShader {
    // Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
    Tags {"Queue" = "Transparent" "RenderType"="Transparent" }

    // Blend SrcAlpha OneMinusSrcAlpha

    LOD 300

    CGPROGRAM

    #pragma surface surf BlinnPhong alpha vertex:vert

    sampler2D _Detail;
    float4 _Color;
    float _Shininess;

    struct Input {
      float2 uv_MainTex;
      float2 uv_Detail;
      float3 worldPos;
      float4 pos;
    };
    sampler2D _MainTex;

    void vert(inout appdata_full v, out Input o)
    {
      UNITY_INITIALIZE_OUTPUT(Input,o);
      o.uv_Detail = v.texcoord;   // maybe need this
      o.pos = UnityObjectToClipPos  (v.vertex);
    }
    
    void surf (Input IN, inout SurfaceOutput o) {
      fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
      o.Albedo = c.rgb;
      o.Alpha = c.a;

      // if(distance(IN.pos, _WorldSpaceCameraPos) > 25)
      // {
      //   clip(-1);
      //   discard;
      // }
      
      // if(distance(mul(unity_ObjectToWorld, IN.worldPos ), _WorldSpaceCameraPos) > 15)
      // {
      //   clip(-1);
      //   discard;
      // }

      if(distance(IN.worldPos, _WorldSpaceCameraPos) > 15)
      {
        clip(-1);
        discard;
      }
    }
    ENDCG
  }
  Fallback "Specular"
}
