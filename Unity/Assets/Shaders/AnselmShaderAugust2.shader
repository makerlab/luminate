Shader "Custom/AnselmShaderAugust" {
	Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _Emission ("Emmisive Color", Color) = (0,0,0,0)
        _Shininess ("Shininess", Range (0.01, 1)) = 0.7
        _MainTex ("Base (RGB)", 2D) = "white" {}
   	}
	SubShader {
        Pass {
            Material {
                Diffuse [_Color]
                Ambient [_Color]
                Shininess [_Shininess]
                Specular [_SpecColor]
                Emission [_Emission]
            }
            Lighting On
            SeparateSpecular On
			//ColorMaterial AmbientAndDiffuse  // this gives me per vertex colors
		    //Blend SrcAlpha OneMinusSrcAlpha  // this gives me alpha if i want it
			//Cull Off // this will help if i want to do 2d ribbons
            SetTexture [_MainTex] {
                Combine texture * primary DOUBLE, texture * primary
            }
        }
   	} 
	FallBack "Diffuse"
}

/* // some interesting fuzting around

	    Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
	    Blend SrcAlpha OneMinusSrcAlpha
	    Cull Off
	    LOD 200
        //Lighting On
		//ColorMaterial AmbientAndDiffuse
		
		// extra pass that renders to depth buffer only
    	//Pass {
    	//    ZWrite On
    	//    ColorMask 0
    	//}

        Material {
           Diffuse (1,1,1,1)
           Ambient (1,1,1,1)
         }
        Lighting On

        Pass {
            SetTexture [_MainTex] {
                Combine Primary * Texture
            }
        }
    
*/

/* // a low level approach
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
	    fixed4 _Color;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			//half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			//o.Albedo = c.rgb;
			//o.Alpha = c.a;
		    o.Albedo = _Color.rgb;
    		o.Emission = _Color.rgb; // * _Color.a;
    		o.Alpha = _Color.a;
		}
		ENDCG
*/