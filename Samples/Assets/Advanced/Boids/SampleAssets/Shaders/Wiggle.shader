// Upgrade NOTE: upgraded instancing buffer 'FishInstanceProperties' to new syntax.

Shader "Custom/Wiggle"
{
	Properties
    {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGBA)", 2D) = "white" {}
		_Gloss ("_MetallicGloss (RGB)", 2D) = "white" {}
		_Tints ("Tints (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Amount ("Wave1 Frequency", float) = 1
		_TimeScale ("Wave1 Speed", float) = 1.0
		_Distance ("Distance", float) = 0.1
	}

    SubShader
    {
		Tags { "RenderType"="Opaque" }
		Cull Off
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
		#pragma target 5.0
		#pragma multi_compile_instancing

        // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html
        // idk if I really need this, but is the only way I've found to be able to access unity_InstanceID in the vertex shader
        // trying to find info from hidden unity shader utilities, macros, etc, is so painful
        #pragma instancing_options procedural:setup

		sampler2D _MainTex;
		sampler2D _Tints;
		sampler2D _Gloss;

		struct Input
        {
			float2 uv_MainTex;
		};

	    struct Boid
        {
			float3 pos;
            float3 fwd;
		};

		half4 _Direction;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _TimeScale;
		float _Amount;
		float _Distance;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
       	StructuredBuffer<Boid> boidBuffer;
        #endif

        UNITY_INSTANCING_BUFFER_START (FishInstanceProperties)
            UNITY_DEFINE_INSTANCED_PROP (float, _InstanceCycleOffset)

#define _InstanceCycleOffset_arr FishInstanceProperties

        UNITY_INSTANCING_BUFFER_END(FishInstanceProperties)

        // look at matrix https://i.stack.imgur.com/LV0gi.png (column major)
        float4x4 lookAtMatrix(float3 target, float3 eye, float3 up)
        {
            float3 fwd = normalize(target - eye);
            float3 side = normalize(cross(fwd, up));
            float3 up2 = normalize(cross(side, fwd));

            return float4x4(
                side.x, up2.x, fwd.x, 0,
                side.y, up2.y, fwd.y, 0,
                side.z, up2.z, fwd.z, 0,
                -dot(side, eye), -dot(up2, eye), -dot(fwd, eye), 1
            );
        }

        // this belongs to the pragma instancing_options setup function defined, see comment above
		void setup()
        {

        }

		void vert(inout appdata_full v)
		{
            // Unity left the "wiggle" cycle offset prepared but afaik they don't actually use it
			float cycleOffset = UNITY_ACCESS_INSTANCED_PROP(_InstanceCycleOffset_arr, _InstanceCycleOffset);
			float4 offs = sin((cycleOffset + _Time.y) * _TimeScale + v.vertex.z * _Amount) * _Distance;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

            // from where +
            float3 pos = boidBuffer[unity_InstanceID].pos;

            // + what to face to
            float3 target = pos + boidBuffer[unity_InstanceID].fwd;

            float4x4 lookAt = lookAtMatrix(target, pos, float3(0.0, 1.0, 0.0));
       		v.vertex = mul(lookAt, v.vertex);
            v.vertex.xyz += pos.xyz;

            #endif

			v.vertex.x += offs;
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 g = tex2D (_Gloss, IN.uv_MainTex);
			fixed4 tintColour = tex2D (_Tints, float2(UNITY_ACCESS_INSTANCED_PROP(_InstanceCycleOffset_arr, _InstanceCycleOffset), 0));
			o.Albedo = lerp(c.rgb, c.rgb * tintColour, c.a) * _Color;
			o.Metallic =  _Metallic;
			o.Smoothness = g.a * _Glossiness;
			//o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
