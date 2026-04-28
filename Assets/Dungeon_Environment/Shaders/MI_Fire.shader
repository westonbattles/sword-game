Shader "Unreal/MI_Fire"
{
	Properties
	{
		_MainTex("MainTex (RGB)", 2D) = "white" {}
		Material_Texture2D_0( "T_Fire_BC_O", 2D ) = "white" {}

		View_BufferSizeAndInvSize( "View_BufferSizeAndInvSize", Vector ) = ( 1920,1080,0.00052, 0.00092 )//1920,1080,1/1920, 1/1080
		LocalObjectBoundsMin( "LocalObjectBoundsMin", Vector ) = ( 0, 0, 0, 0 )
		LocalObjectBoundsMax( "LocalObjectBoundsMax", Vector ) = ( 100, 100, 100, 0 )
	}
	SubShader 
	{
		//BLEND_OFF Tags { "RenderType" = "Opaque" }
		 Tags { "RenderType" = "Transparent"  "Queue" = "Transparent" }
		
		//Blend SrcAlpha OneMinusSrcAlpha
		Cull Off

		CGPROGRAM

		#include "UnityPBSLighting.cginc"
		//BLEND_OFF #pragma surface surf Standard vertex:vert addshadow
		 #pragma surface surf Standard vertex:vert alpha:fade addshadow
		
		#pragma target 5.0

		#define NUM_TEX_COORD_INTERPOLATORS 1
		#define NUM_MATERIAL_TEXCOORDS_VERTEX 1
		#define NUM_CUSTOM_VERTEX_INTERPOLATORS 0

		struct Input
		{
			//float3 Normal;
			float2 uv_MainTex : TEXCOORD0;
			float2 uv2_Material_Texture2D_0 : TEXCOORD1;
			//float2 uv2_MainTex : TEXCOORD1;
			float4 color : COLOR;
			float4 tangent;
			//float4 normal;
			float3 viewDir;
			float4 screenPos;
			float3 worldPos;
			//float3 worldNormal;
			float3 normal2;
			INTERNAL_DATA
		};
		void vert( inout appdata_full i, out Input o )
		{
			float3 p_normal = mul( float4( i.normal, 0.0f ), unity_WorldToObject );
			//half4 p_tangent = mul( unity_ObjectToWorld,i.tangent );

			//half3 normal_input = normalize( p_normal.xyz );
			//half3 tangent_input = normalize( p_tangent.xyz );
			//half3 binormal_input = cross( p_normal.xyz,tangent_input.xyz ) * i.tangent.w;
			UNITY_INITIALIZE_OUTPUT( Input, o );

			//o.worldNormal = p_normal;
			o.normal2 = p_normal;
			o.tangent = i.tangent;
			//o.binormal_input = binormal_input;
		}
		uniform sampler2D _MainTex;
		/*
		struct SurfaceOutputStandard
		{
		fixed3 Albedo;		// base (diffuse or specular) color
		fixed3 Normal;		// tangent space normal, if written
		half3 Emission;
		half Metallic;		// 0=non-metal, 1=metal
		// Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
		// Everywhere in the code you meet smoothness it is perceptual smoothness
		half Smoothness;	// 0=rough, 1=smooth
		half Occlusion;		// occlusion (default 1)
		fixed Alpha;		// alpha for transparencies
		};
		*/


		#define Texture2D sampler2D
		#define TextureCube samplerCUBE
		#define SamplerState int

		//#define UE5
		#define MATERIAL_TANGENTSPACENORMAL 1
		//struct Material
		//{
			//samplers start
			uniform sampler2D    Material_Texture2D_0;
			uniform SamplerState Material_Texture2D_0Sampler;
			
		//};

		#ifdef UE5
			#define UE_LWC_RENDER_TILE_SIZE			2097152.0
			#define UE_LWC_RENDER_TILE_SIZE_SQRT	1448.15466
			#define UE_LWC_RENDER_TILE_SIZE_RSQRT	0.000690533954
			#define UE_LWC_RENDER_TILE_SIZE_RCP		4.76837158e-07
			#define UE_LWC_RENDER_TILE_SIZE_FMOD_PI		0.673652053
			#define UE_LWC_RENDER_TILE_SIZE_FMOD_2PI	0.673652053
			#define INVARIANT(X) X
			#define PI 					(3.1415926535897932)

			//#include "LargeWorldCoordinates.hlsl"
		#endif
		struct MaterialStruct
		{
			float4 VectorExpressions[5];
			float4 ScalarExpressions[2];
			float VTPackedPageTableUniform[2];
			float VTPackedUniform[1];
		};
		struct ViewStruct
		{
			float GameTime;
			float RealTime;
			float DeltaTime;
			float PrevFrameGameTime;
			float PrevFrameRealTime;
			float MaterialTextureMipBias;
			SamplerState MaterialTextureBilinearWrapedSampler;
			SamplerState MaterialTextureBilinearClampedSampler;
			float4 PrimitiveSceneData[ 40 ];
			float4 TemporalAAParams;
			float2 ViewRectMin;
			float4 ViewSizeAndInvSize;
			float MaterialTextureDerivativeMultiply;
			uint StateFrameIndexMod8;
			float FrameNumber;
			float2 FieldOfViewWideAngles;
			float4 RuntimeVirtualTextureMipLevel;
			float PreExposure;
			float4 BufferBilinearUVMinMax;
		};
		struct ResolvedViewStruct
		{
		#ifdef UE5
			FLWCVector3 WorldCameraOrigin;
			FLWCVector3 PrevWorldCameraOrigin;
			FLWCVector3 PreViewTranslation;
			FLWCVector3 WorldViewOrigin;
		#else
			float3 WorldCameraOrigin;
			float3 PrevWorldCameraOrigin;
			float3 PreViewTranslation;
			float3 WorldViewOrigin;
		#endif
			float4 ScreenPositionScaleBias;
			float4x4 TranslatedWorldToView;
			float4x4 TranslatedWorldToCameraView;
			float4x4 TranslatedWorldToClip;
			float4x4 ViewToTranslatedWorld;
			float4x4 PrevViewToTranslatedWorld;
			float4x4 CameraViewToTranslatedWorld;
			float4 BufferBilinearUVMinMax;
			float4 XRPassthroughCameraUVs[ 2 ];
		};
		struct PrimitiveStruct
		{
			float4x4 WorldToLocal;
			float4x4 LocalToWorld;
		};

		ViewStruct View;
		ResolvedViewStruct ResolvedView;
		PrimitiveStruct Primitive;
		uniform float4 View_BufferSizeAndInvSize;
		uniform float4 LocalObjectBoundsMin;
		uniform float4 LocalObjectBoundsMax;
		uniform SamplerState Material_Wrap_WorldGroupSettings;
		uniform SamplerState Material_Clamp_WorldGroupSettings;
		
		#define PI UNITY_PI
		#include "UnrealCommon.cginc"

		MaterialStruct Material;
void InitializeExpressions()
{
	Material.VectorExpressions[0] = float4(0.000000,0.000000,0.000000,0.000000);//SelectionColor
	Material.VectorExpressions[1] = float4(0.000000,0.000000,0.000000,0.000000);//(Unknown)
	Material.VectorExpressions[2] = float4(0.000000,0.000000,0.000000,0.000000);//AdditionalWPO (V3)
	Material.VectorExpressions[3] = float4(0.000000,0.000000,0.000000,0.000000);//(Unknown)
	Material.VectorExpressions[4] = float4(0.000000,0.000000,-10.000000,0.000000);//(Unknown)
	Material.ScalarExpressions[0] = float4(0.040000,2.000000,0.000000,1.000000);//Sine Strength Emissive_Strength (Unknown) WindSpeed (S)
	Material.ScalarExpressions[1] = float4(1.000000,0.001000,1.000000,0.000000);//WindWeight (S) WindIntensity (S) WindSize RefractionDepthBias
}float3 GetMaterialWorldPositionOffset(FMaterialVertexParameters Parameters)
{
	#if USE_INSTANCING
		// skip if this instance is hidden
		if (Parameters.PerInstanceParams.z < 1.f)
		{
			return float3(0,0,0);
		}
	#endif
	MaterialFloat Local12 = (View.GameTime * Material.ScalarExpressions[0].w);
	MaterialFloat Local13 = (Local12 * -0.50000000);
	MaterialFloat Local14 = (MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).a * Local13);
	MaterialFloat3 Local15 = ((MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb / sqrt(dot(MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb,MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb))) * Local14);
	MaterialFloat3 Local16 = (GetWorldPosition(Parameters) / 1024.00000000);
	MaterialFloat3 Local17 = (Local15 + Local16);
	MaterialFloat3 Local18 = (Local17 + 0.50000000);
	MaterialFloat3 Local19 = frac(Local18);
	MaterialFloat3 Local20 = (Local19 * 2.00000000);
	MaterialFloat3 Local21 = (Local20 + -1.00000000);
	MaterialFloat3 Local22 = abs(Local21);
	MaterialFloat3 Local23 = (Local22 * 2.00000000);
	MaterialFloat3 Local24 = (3.00000000 - Local23);
	MaterialFloat3 Local25 = (Local24 * Local22);
	MaterialFloat3 Local26 = (Local25 * Local22);
	MaterialFloat Local27 = dot((MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb / sqrt(dot(MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb,MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb))), Local26);
	MaterialFloat3 Local28 = (GetWorldPosition(Parameters) / 200.00000000);
	MaterialFloat3 Local29 = (Local14 + Local28);
	MaterialFloat3 Local30 = (Local29 + 0.50000000);
	MaterialFloat3 Local31 = frac(Local30);
	MaterialFloat3 Local32 = (Local31 * 2.00000000);
	MaterialFloat3 Local33 = (Local32 + -1.00000000);
	MaterialFloat3 Local34 = abs(Local33);
	MaterialFloat3 Local35 = (Local34 * 2.00000000);
	MaterialFloat3 Local36 = (3.00000000 - Local35);
	MaterialFloat3 Local37 = (Local36 * Local34);
	MaterialFloat3 Local38 = (Local37 * Local34);
	MaterialFloat3 Local39 = (Local38 - 0.00000000);
	MaterialFloat Local40 = dot(Local39, Local39);
	MaterialFloat Local41 = sqrt(Local40);
	MaterialFloat Local42 = (Local27 + Local41);
	MaterialFloat Local43 = (Local42 * 6.28318548);
	MaterialFloat3 Local44 = RotateAboutAxis(MaterialFloat4(cross((MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb / sqrt(dot(MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb,MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb))),MaterialFloat3(0.00000000,0.00000000,1.00000000)),Local43),Material.VectorExpressions[4].rgb,Material.VectorExpressions[3].rgb);
	MaterialFloat3 Local45 = (Local44 * Material.ScalarExpressions[1].x);
	MaterialFloat3 Local46 = (Local45 * Material.ScalarExpressions[1].y);
	MaterialFloat3 Local47 = (Local46 + Material.VectorExpressions[3].rgb);
	MaterialFloat3 Local48 = (Local47 * Material.ScalarExpressions[1].z);
	MaterialFloat Local49 = (View.PrevFrameGameTime * Material.ScalarExpressions[0].w);
	MaterialFloat Local50 = (Local49 * -0.50000000);
	MaterialFloat Local51 = (MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).a * Local50);
	MaterialFloat3 Local52 = ((MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb / sqrt(dot(MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb,MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb))) * Local51);
	MaterialFloat3 Local53 = (GetPrevWorldPosition(Parameters) / 1024.00000000);
	MaterialFloat3 Local54 = (Local52 + Local53);
	MaterialFloat3 Local55 = (Local54 + 0.50000000);
	MaterialFloat3 Local56 = frac(Local55);
	MaterialFloat3 Local57 = (Local56 * 2.00000000);
	MaterialFloat3 Local58 = (Local57 + -1.00000000);
	MaterialFloat3 Local59 = abs(Local58);
	MaterialFloat3 Local60 = (Local59 * 2.00000000);
	MaterialFloat3 Local61 = (3.00000000 - Local60);
	MaterialFloat3 Local62 = (Local61 * Local59);
	MaterialFloat3 Local63 = (Local62 * Local59);
	MaterialFloat Local64 = dot((MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb / sqrt(dot(MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb,MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb))), Local63);
	MaterialFloat3 Local65 = (GetPrevWorldPosition(Parameters) / 200.00000000);
	MaterialFloat3 Local66 = (Local51 + Local65);
	MaterialFloat3 Local67 = (Local66 + 0.50000000);
	MaterialFloat3 Local68 = frac(Local67);
	MaterialFloat3 Local69 = (Local68 * 2.00000000);
	MaterialFloat3 Local70 = (Local69 + -1.00000000);
	MaterialFloat3 Local71 = abs(Local70);
	MaterialFloat3 Local72 = (Local71 * 2.00000000);
	MaterialFloat3 Local73 = (3.00000000 - Local72);
	MaterialFloat3 Local74 = (Local73 * Local71);
	MaterialFloat3 Local75 = (Local74 * Local71);
	MaterialFloat3 Local76 = (Local75 - 0.00000000);
	MaterialFloat Local77 = dot(Local76, Local76);
	MaterialFloat Local78 = sqrt(Local77);
	MaterialFloat Local79 = (Local64 + Local78);
	MaterialFloat Local80 = (Local79 * 6.28318548);
	MaterialFloat3 Local81 = RotateAboutAxis(MaterialFloat4(cross((MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb / sqrt(dot(MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb,MaterialFloat4(0.00000000,1.00000000,0.00000000,1.00000000).rgb))),MaterialFloat3(0.00000000,0.00000000,1.00000000)),Local80),Material.VectorExpressions[4].rgb,Material.VectorExpressions[3].rgb);
	MaterialFloat3 Local82 = (Local81 * Material.ScalarExpressions[1].x);
	MaterialFloat3 Local83 = (Local82 * Material.ScalarExpressions[1].y);
	MaterialFloat3 Local84 = (Local83 + Material.VectorExpressions[3].rgb);
	MaterialFloat3 Local85 = (Local84 * Material.ScalarExpressions[1].z);
	return Local48;;
}
void CalcPixelMaterialInputs(in out FMaterialPixelParameters Parameters, in out FPixelMaterialInputs PixelMaterialInputs)
{
	//WorldAligned texturing & others use normals & stuff that think Z is up
	Parameters.TangentToWorld[0] = Parameters.TangentToWorld[0].xzy;
	Parameters.TangentToWorld[1] = Parameters.TangentToWorld[1].xzy;
	Parameters.TangentToWorld[2] = Parameters.TangentToWorld[2].xzy;

	float3 WorldNormalCopy = Parameters.WorldNormal;

	// Initial calculations (required for Normal)

	// The Normal is a special case as it might have its own expressions and also be used to calculate other inputs, so perform the assignment here
	PixelMaterialInputs.Normal = MaterialFloat3(0.00000000,0.00000000,1.00000000);


	// Note that here MaterialNormal can be in world space or tangent space
	float3 MaterialNormal = GetMaterialNormal(Parameters, PixelMaterialInputs);

#if MATERIAL_TANGENTSPACENORMAL
#if SIMPLE_FORWARD_SHADING
	Parameters.WorldNormal = float3(0, 0, 1);
#endif

#if FEATURE_LEVEL >= FEATURE_LEVEL_SM4
	// Mobile will rely on only the final normalize for performance
	MaterialNormal = normalize(MaterialNormal);
#endif

	// normalizing after the tangent space to world space conversion improves quality with sheared bases (UV layout to WS causes shrearing)
	// use full precision normalize to avoid overflows
	Parameters.WorldNormal = TransformTangentNormalToWorld(Parameters.TangentToWorld, MaterialNormal);

#else //MATERIAL_TANGENTSPACENORMAL

	Parameters.WorldNormal = normalize(MaterialNormal);

#endif //MATERIAL_TANGENTSPACENORMAL

#if MATERIAL_TANGENTSPACENORMAL
	// flip the normal for backfaces being rendered with a two-sided material
	Parameters.WorldNormal *= Parameters.TwoSidedSign;
#endif

	Parameters.ReflectionVector = ReflectionAboutCustomWorldNormal(Parameters, Parameters.WorldNormal, false);

#if !PARTICLE_SPRITE_FACTORY
	Parameters.Particle.MotionBlurFade = 1.0f;
#endif // !PARTICLE_SPRITE_FACTORY

	// Now the rest of the inputs
	MaterialFloat Local0 = (View.GameTime * 0.20000000);
	MaterialFloat Local1 = (View.GameTime * 1.50000000);
	MaterialFloat2 Local2 = (MaterialFloat2(Local0,Local1) + Parameters.TexCoords[0].xy);
	MaterialFloat2 Local3 = (Local2 * 6.28318548);
	MaterialFloat2 Local4 = sin(Local3);
	MaterialFloat2 Local5 = (Local4 * Material.ScalarExpressions[0].x);
	MaterialFloat2 Local6 = (Parameters.TexCoords[0].xy + Local5);
	MaterialFloat Local7 = MaterialStoreTexCoordScale(Parameters, Local6, 0);
	MaterialFloat4 Local8 = ProcessMaterialColorTextureLookup(Texture2DSampleBias(Material_Texture2D_0, Material_Texture2D_0Sampler,Local6,View.MaterialTextureMipBias));
	MaterialFloat Local9 = MaterialStoreTexSample(Parameters, Local8, 0);
	MaterialFloat3 Local10 = (Local8.rgb * Material.ScalarExpressions[0].y);
	MaterialFloat3 Local11 = lerp(Local10,Material.VectorExpressions[1].rgb,MaterialFloat(Material.ScalarExpressions[0].z));

	PixelMaterialInputs.EmissiveColor = Local11;
	PixelMaterialInputs.Opacity = Local8.a;
	PixelMaterialInputs.OpacityMask = 1.00000000;
	PixelMaterialInputs.BaseColor = Local8.rgb;
	PixelMaterialInputs.Metallic = 0.00000000;
	PixelMaterialInputs.Specular = 0.50000000;
	PixelMaterialInputs.Roughness = 0.50000000;
	PixelMaterialInputs.Anisotropy = 0.00000000;
	PixelMaterialInputs.Tangent = MaterialFloat3(1.00000000,0.00000000,0.00000000);
	PixelMaterialInputs.Subsurface = 0;
	PixelMaterialInputs.AmbientOcclusion = 1.00000000;
	PixelMaterialInputs.Refraction = MaterialFloat2(MaterialFloat2(MaterialFloat2(1.00000000,0.00000000).r,0).r,Material.ScalarExpressions[1].w);
	PixelMaterialInputs.PixelDepthOffset = 0.00000000;
	PixelMaterialInputs.ShadingModel = 1;


#if MATERIAL_USES_ANISOTROPY
	Parameters.WorldTangent = CalculateAnisotropyTangent(Parameters, PixelMaterialInputs);
#else
	Parameters.WorldTangent = 0;
#endif
}
		void surf( Input In, inout SurfaceOutputStandard o )
		{
			InitializeExpressions();

			float3 Z3 = float3( 0, 0, 0 );
			float4 Z4 = float4( 0, 0, 0, 0 );

			float3 UnrealWorldPos = float3( In.worldPos.x, In.worldPos.y, In.worldPos.z );
			
			float3 UnrealNormal = In.normal2;

			FMaterialPixelParameters Parameters = (FMaterialPixelParameters)0;
			#if NUM_TEX_COORD_INTERPOLATORS > 0			
				Parameters.TexCoords[ 0 ] = float2( In.uv_MainTex.x, 1.0 - In.uv_MainTex.y );
			#endif
			#if NUM_TEX_COORD_INTERPOLATORS > 1
				Parameters.TexCoords[ 1 ] = float2( In.uv2_Material_Texture2D_0.x, 1.0 - In.uv2_Material_Texture2D_0.y );
			#endif
			#if NUM_TEX_COORD_INTERPOLATORS > 2
			for( int i = 2; i < NUM_TEX_COORD_INTERPOLATORS; i++ )
			{
				Parameters.TexCoords[ i ] = float2( In.uv_MainTex.x, 1.0 - In.uv_MainTex.y );
			}
			#endif
			Parameters.VertexColor = In.color;
			Parameters.WorldNormal = UnrealNormal;
			Parameters.ReflectionVector = half3( 0, 0, 1 );
			Parameters.CameraVector = normalize( _WorldSpaceCameraPos.xyz - UnrealWorldPos.xyz );
			//Parameters.CameraVector = mul( ( float3x3 )unity_CameraToWorld, float3( 0, 0, 1 ) ) * -1;
			Parameters.LightVector = half3( 0, 0, 0 );
			float4 screenpos = In.screenPos;
			screenpos /= screenpos.w;
			//screenpos.y = 1 - screenpos.y;
			Parameters.SvPosition = float4( screenpos.x, screenpos.y, 0, 0 );
			Parameters.ScreenPosition = Parameters.SvPosition;

			Parameters.UnMirrored = 1;

			Parameters.TwoSidedSign = 1;
			

			float3 InWorldNormal = UnrealNormal;
			float4 InTangent = In.tangent;
			float4 tangentWorld = float4( UnityObjectToWorldDir( InTangent.xyz ), InTangent.w );
			tangentWorld.xyz = normalize( tangentWorld.xyz );
			float3x3 OriginalTangentToWorld = CreateTangentToWorldPerVertex( InWorldNormal, tangentWorld.xyz, tangentWorld.w );
			Parameters.TangentToWorld = OriginalTangentToWorld;

			//WorldAlignedTexturing in UE relies on the fact that coords there are 100x larger, prepare values for that
			//but watch out for any computation that might get skewed as a side effect
			UnrealWorldPos = ToUnrealPos( UnrealWorldPos );
			
			Parameters.AbsoluteWorldPosition = UnrealWorldPos;
			Parameters.WorldPosition_CamRelative = UnrealWorldPos;
			Parameters.WorldPosition_NoOffsets = UnrealWorldPos;

			Parameters.WorldPosition_NoOffsets_CamRelative = Parameters.WorldPosition_CamRelative;
			Parameters.LightingPositionOffset = float3( 0, 0, 0 );

			Parameters.AOMaterialMask = 0;

			Parameters.Particle.RelativeTime = 0;
			Parameters.Particle.MotionBlurFade;
			Parameters.Particle.Random = 0;
			Parameters.Particle.Velocity = half4( 1, 1, 1, 1 );
			Parameters.Particle.Color = half4( 1, 1, 1, 1 );
			Parameters.Particle.TranslatedWorldPositionAndSize = float4( UnrealWorldPos, 0 );
			Parameters.Particle.MacroUV = half4(0,0,1,1);
			Parameters.Particle.DynamicParameter = half4(0,0,0,0);
			Parameters.Particle.LocalToWorld = float4x4( Z4, Z4, Z4, Z4 );
			Parameters.Particle.Size = float2(1,1);
			Parameters.Particle.SubUVCoords[ 0 ] = Parameters.Particle.SubUVCoords[ 1 ] = float2( 0, 0 );
			Parameters.Particle.SubUVLerp = 0.0;
			Parameters.TexCoordScalesParams = float2( 0, 0 );
			Parameters.PrimitiveId = 0;
			Parameters.VirtualTextureFeedback = 0;

			FPixelMaterialInputs PixelMaterialInputs = ( FPixelMaterialInputs)0;
			PixelMaterialInputs.Normal = float3( 0, 0, 1 );
			PixelMaterialInputs.ShadingModel = 0;
			PixelMaterialInputs.FrontMaterial = 0;

			View.GameTime = View.RealTime = _Time.y;// _Time is (t/20, t, t*2, t*3)
			View.PrevFrameGameTime = View.GameTime - unity_DeltaTime.x;//(dt, 1/dt, smoothDt, 1/smoothDt)
			View.PrevFrameRealTime = View.RealTime;
			View.DeltaTime = unity_DeltaTime.x;
			View.MaterialTextureMipBias = 0.0;
			View.TemporalAAParams = float4( 0, 0, 0, 0 );
			View.ViewRectMin = float2( 0, 0 );
			View.ViewSizeAndInvSize = View_BufferSizeAndInvSize;
			View.MaterialTextureDerivativeMultiply = 1.0f;
			View.StateFrameIndexMod8 = 0;
			View.FrameNumber = (int)_Time.y;
			View.FieldOfViewWideAngles = float2( PI * 0.42f, PI * 0.42f );//75degrees, default unity
			View.RuntimeVirtualTextureMipLevel = float4( 0, 0, 0, 0 );
			View.PreExposure = 0;
			View.BufferBilinearUVMinMax = float4(
				View_BufferSizeAndInvSize.z * ( 0 + 0.5 ),//EffectiveViewRect.Min.X
				View_BufferSizeAndInvSize.w * ( 0 + 0.5 ),//EffectiveViewRect.Min.Y
				View_BufferSizeAndInvSize.z * ( View_BufferSizeAndInvSize.x - 0.5 ),//EffectiveViewRect.Max.X
				View_BufferSizeAndInvSize.w * ( View_BufferSizeAndInvSize.y - 0.5 ) );//EffectiveViewRect.Max.Y

			for( int i2 = 0; i2 < 40; i2++ )
				View.PrimitiveSceneData[ i2 ] = float4( 0, 0, 0, 0 );

			float4x4 ViewMatrix = transpose( unity_MatrixV );
			float4x4 InverseViewMatrix = transpose( unity_MatrixInvV );
			float4x4 ViewProjectionMatrix = transpose( unity_MatrixVP );

			uint PrimitiveBaseOffset = Parameters.PrimitiveId * PRIMITIVE_SCENE_DATA_STRIDE;
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 0 ] = unity_ObjectToWorld[ 0 ];//LocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 1 ] = unity_ObjectToWorld[ 1 ];//LocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 2 ] = unity_ObjectToWorld[ 2 ];//LocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 3 ] = unity_ObjectToWorld[ 3 ];//LocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 6 ] = unity_WorldToObject[ 0 ];//WorldToLocal
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 7 ] = unity_WorldToObject[ 1 ];//WorldToLocal
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 8 ] = unity_WorldToObject[ 2 ];//WorldToLocal
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 9 ] = unity_WorldToObject[ 3 ];//WorldToLocal
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 10 ] = unity_WorldToObject[ 0 ];//PreviousLocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 11 ] = unity_WorldToObject[ 1 ];//PreviousLocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 12 ] = unity_WorldToObject[ 2 ];//PreviousLocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 13 ] = unity_WorldToObject[ 3 ];//PreviousLocalToWorld
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 18 ] = float4( ToUnrealPos( UNITY_MATRIX_M[ 3 ] ), 0 );//ActorWorldPosition
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 19 ] = LocalObjectBoundsMax - LocalObjectBoundsMin;//ObjectBounds
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 21 ] = mul( unity_ObjectToWorld, float3( 1, 0, 0 ) );//ObjectOrientation
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 23 ] = LocalObjectBoundsMin;//LocalObjectBoundsMin
			View.PrimitiveSceneData[ PrimitiveBaseOffset + 24 ] = LocalObjectBoundsMax;//LocalObjectBoundsMax

			#ifdef UE5
				ResolvedView.WorldCameraOrigin = LWCPromote( ToUnrealPos( _WorldSpaceCameraPos.xyz ) );
				ResolvedView.PreViewTranslation = LWCPromote( float3( 0, 0, 0 ) );
				ResolvedView.WorldViewOrigin = LWCPromote( float3( 0, 0, 0 ) );
			#else
				ResolvedView.WorldCameraOrigin = ToUnrealPos( _WorldSpaceCameraPos.xyz );
				ResolvedView.PreViewTranslation = float3( 0, 0, 0 );
				ResolvedView.WorldViewOrigin = float3( 0, 0, 0 );
			#endif
			ResolvedView.PrevWorldCameraOrigin = ResolvedView.WorldCameraOrigin;
			ResolvedView.ScreenPositionScaleBias = float4( 1, 1, 0, 0 );
			ResolvedView.TranslatedWorldToView = ViewMatrix;
			ResolvedView.TranslatedWorldToCameraView = ViewMatrix;
			ResolvedView.TranslatedWorldToClip = ViewProjectionMatrix;
			ResolvedView.ViewToTranslatedWorld = InverseViewMatrix;
			ResolvedView.PrevViewToTranslatedWorld = ResolvedView.ViewToTranslatedWorld;
			ResolvedView.CameraViewToTranslatedWorld = InverseViewMatrix;
			ResolvedView.BufferBilinearUVMinMax = View.BufferBilinearUVMinMax;
			ResolvedView.XRPassthroughCameraUVs[ 0 ] = ResolvedView.XRPassthroughCameraUVs[ 1 ] = float4( 0, 0, 1, 1 );
			Primitive.WorldToLocal = unity_WorldToObject;
			Primitive.LocalToWorld = unity_ObjectToWorld;
			CalcPixelMaterialInputs( Parameters, PixelMaterialInputs );

			#define HAS_WORLDSPACE_NORMAL 0
			#if HAS_WORLDSPACE_NORMAL
				PixelMaterialInputs.Normal = mul( PixelMaterialInputs.Normal, (MaterialFloat3x3)( transpose( OriginalTangentToWorld ) ) );
			#endif

			o.Albedo = PixelMaterialInputs.BaseColor.rgb;
			o.Alpha = PixelMaterialInputs.Opacity;
			//if( PixelMaterialInputs.OpacityMask < 0.333 ) discard;

			o.Metallic = PixelMaterialInputs.Metallic;
			o.Smoothness = 1.0 - PixelMaterialInputs.Roughness;
			o.Normal = normalize( PixelMaterialInputs.Normal );
			o.Emission = PixelMaterialInputs.EmissiveColor.rgb;
			o.Occlusion = PixelMaterialInputs.AmbientOcclusion;

			//BLEND_ADDITIVE o.Alpha = ( o.Emission.r + o.Emission.g + o.Emission.b ) / 3;
		}
		ENDCG
	}
	Fallback "Diffuse"
}