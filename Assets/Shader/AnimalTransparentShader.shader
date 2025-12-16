Shader "Custom/AnimalTransparentLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        // --- ADDED NORMAL MAP PROPERTY ---
        _BumpMap ("Normal Map", 2D) = "bump" {} 
        
        _MaskTex ("Mask Texture (R=Opacity)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        
        // Basic PBR settings
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 200

        CGPROGRAM
        // We use "Standard" lighting so the Normal Map interacts with light.
        // "alphatest:_Cutoff" automatically handles the clipping logic based on o.Alpha.
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff

        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap; // Variable for Normal Map
        sampler2D _MaskTex; // Variable for Mask

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_MaskTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. Albedo (Color)
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // 2. Normal Map Calculation
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

            // 3. Metallic/Smoothness
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // 4. Masking Logic
            // Get value from Mask Texture (Red Channel)
            float maskValue = tex2D(_MaskTex, IN.uv_MaskTex).r;
            
            // Combine Albedo Alpha with Mask Value
            o.Alpha = c.a * maskValue;
            
            // Note: The clipping happens automatically because of 'alphatest:_Cutoff' 
            // defined in the #pragma line above.
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}