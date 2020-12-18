Shader "Museum/Volatile Gradient" {
    
    Properties {
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    
    SubShader {
        CGPROGRAM
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		#pragma instancing_options procedural:ConfigureProcedural
		#pragma editor_sync_compilation
		#pragma target 4.5

        struct Input {
            float3 worldPos;
        };

        half _Smoothness;
        half _Metallic;

		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		    StructuredBuffer<float3> _Positions;  // read-only buffer
		#endif


        float2 _Scale;
        
		void ConfigureProcedural() {
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
				// retrieve the current instance (point) position from the buffer
			    // which has been updated in the compute shader
			    float3 position = _Positions[unity_InstanceID];

			    // compute the transformation matrix that converts vertices from object space to world space
				unity_ObjectToWorld = 0.0;
				unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
				unity_ObjectToWorld._m00_m11_m22 = _Scale.x;

				unity_WorldToObject = 0.0;
				unity_WorldToObject._m03_m13_m23_m33 = float4(-position, 1.0);
				unity_WorldToObject._m00_m11_m22 = _Scale.y;
			#endif
		}

		void ConfigureSurface(Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo = saturate(input.worldPos * 0.9 + 0.5);
            surface.Metallic = _Metallic;
            surface.Smoothness = _Smoothness;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}