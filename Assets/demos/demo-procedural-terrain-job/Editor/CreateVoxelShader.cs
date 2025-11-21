using UnityEngine;
using UnityEditor;
using System.IO;

namespace TimeSurvivor.Demos.ProceduralTerrain.Editor
{
    /// <summary>
    /// Editor utility to create the custom vertex color shader for URP.
    /// Menu: Tools > Voxel Demos > Create Voxel Terrain Shader
    /// </summary>
    public static class CreateVoxelShader
    {
        private const string SHADER_PATH = "Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrainShader.shader";
        private const string MATERIAL_PATH = "Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrain.mat";

        [MenuItem("Tools/Voxel Demos/Create Voxel Terrain Shader")]
        public static void CreateShader()
        {
            // Check if shader already exists
            if (File.Exists(SHADER_PATH))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Shader Already Exists",
                    $"The shader already exists at:\n{SHADER_PATH}\n\nDo you want to overwrite it?",
                    "Overwrite",
                    "Cancel"
                );

                if (!overwrite)
                {
                    Debug.Log("[CreateVoxelShader] Shader creation cancelled.");
                    return;
                }
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(SHADER_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write shader file
            File.WriteAllText(SHADER_PATH, GetShaderCode());
            AssetDatabase.Refresh();

            Debug.Log($"[CreateVoxelShader] Shader created successfully at: {SHADER_PATH}");

            // Optionally create material
            if (!File.Exists(MATERIAL_PATH))
            {
                bool createMaterial = EditorUtility.DisplayDialog(
                    "Create Material?",
                    "Would you like to create a material using this shader?",
                    "Yes",
                    "No"
                );

                if (createMaterial)
                {
                    CreateMaterialWithShader();
                }
            }

            // Ping the shader in Project window
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
            EditorGUIUtility.PingObject(shader);
        }

        private static void CreateMaterialWithShader()
        {
            // Wait for shader to compile
            AssetDatabase.ImportAsset(SHADER_PATH, ImportAssetOptions.ForceSynchronousImport);

            Shader shader = Shader.Find("Custom/VoxelTerrainVertexColor");
            if (shader == null)
            {
                Debug.LogError("[CreateVoxelShader] Could not find shader 'Custom/VoxelTerrainVertexColor'. Wait for Unity to compile the shader.");
                return;
            }

            Material material = new Material(shader);
            material.SetFloat("_Smoothness", 0.2f);

            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateVoxelShader] Material created successfully at: {MATERIAL_PATH}");
            EditorGUIUtility.PingObject(material);
        }

        private static string GetShaderCode()
        {
            return @"Shader ""Custom/VoxelTerrainVertexColor""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Smoothness (""Smoothness"", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" ""RenderPipeline""=""UniversalPipeline"" }
        LOD 100

        Pass
        {
            Name ""ForwardLit""
            Tags { ""LightMode""=""UniversalForward"" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Apply vertex color
                half4 finalColor = texColor * input.color;

                // Simple lighting (diffuse)
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, lightDir));
                float3 lighting = mainLight.color * NdotL;

                // Ambient
                float3 ambient = half3(0.3, 0.3, 0.3);

                finalColor.rgb *= (lighting + ambient);

                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack ""Universal Render Pipeline/Lit""
}";
        }
    }
}
