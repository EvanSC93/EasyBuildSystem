Shader "Unlit/Test"
{
    Properties
    {
        _MainColor ("MainColor", color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" { }
        
        _DiffuseColor ("DiffuseColor", color) = (1, 1, 1, 1)
        _DiffuseMin ("DiffuseMin", Range(0, 1)) = 0
        _DiffuseMax ("DiffuseMax", Range(0, 1)) = 0
        
        _SpecularColor ("SpecularColor", color) = (1, 1, 1, 1)
        _SpecularPow ("SpecularPow", Range(1, 128)) = 1
        
        _OutLine ("OutLine", Range(0, 0.1)) = 0
        _OutLineColor ("OutLineColor", color) = (0, 0, 0, 0)
        
        _RimColor ("RimColor", color) = (1, 1, 1, 1)
        _RimPow ("RimPow", Range(1, 16)) = 1
        _RimClamp ("RimClamp", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }
        
        Pass
        {
            Cull Front
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
            };
            
            fixed4 _OutLineColor;
            float _OutLine;
            
            v2f vert(appdata_base v)
            {
                v2f o;
                v.vertex.xyz += v.normal * _OutLine;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                return _OutLineColor;
            }
            ENDCG
            
        }
        
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
                float4 localPos: TEXCOORD1;
                float3 normal: NORMAL0;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _MainColor, _DiffuseColor, _SpecularColor, _RimColor;
            float _DiffuseMin, _DiffuseMax, _SpecularPow, _RimPow, _RimClamp;
            
            v2f vert(appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.localPos = v.vertex;
                o.normal = v.normal;
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                float3 worldNormal = UnityObjectToWorldNormal(i.normal);
                float3 worldLight = WorldSpaceLightDir(i.localPos);
                float3 worldView = normalize(WorldSpaceViewDir(i.localPos));
                float3 worldRef = normalize(reflect(-worldLight, worldNormal));
                
                float LdotN = dot(worldNormal, worldLight) * 0.5 + 0.5;
                float VdotR = saturate(dot(worldView, worldRef));
                float VdotN = 1 - saturate(dot(worldNormal, worldView));
                
                fixed4 col = tex2D(_MainTex, i.uv) * _MainColor;
                
                fixed3 diffuseCol = lerp(_DiffuseColor, col.rgb, step(_DiffuseMin, LdotN) * smoothstep(_DiffuseMin, _DiffuseMax, LdotN));
                
                fixed4 finalColor = fixed4(0, 0, 0, 1);
                finalColor.rgb += diffuseCol;
                finalColor.rgb = lerp(finalColor.rgb, _SpecularColor.rgb, step(0.5, pow(VdotR, _SpecularPow)));
                finalColor.rgb = lerp(finalColor.rgb, _RimColor.rgb, step(_RimClamp, pow(VdotN, _RimPow)));
                
                return finalColor;
            }
            ENDCG
            
        }
    }
    Fallback "Diffuse"
}
