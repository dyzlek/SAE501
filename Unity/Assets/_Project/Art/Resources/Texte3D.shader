// Shader des textes 3D du jeu (TextMesh), écrit pour URP et compatible casque.
// Celui de Unity (GUI/Text Shader) ne gère pas le rendu stéréo du Quest : le texte n'apparaît que dans un œil
// ou se dédouble, et il se dessine par-dessus les murs. Celui-ci est un simple texte transparent :
// couleur du TextMesh × forme des lettres (alpha de la texture de police), caché par ce qui est devant.
// Il utilise la bibliothèque d'URP (Core.hlsl) : c'est elle qui donne la bonne caméra à chaque œil.
// Il est dans un dossier Resources pour être toujours inclus dans le build (Shader.Find au lancement).
Shader "SAE/Texte 3D"
{
    Properties
    {
        _MainTex ("Texture de la police", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Texte"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO          // l'image de chaque œil (rendu stéréo du casque)
            };

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 c = input.color;
                c.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return c;
            }
            ENDHLSL
        }
    }
}
