
HEADER
{
	Description = "Space Sky";
}

MODES
{
	Default();
	VrForward();
}

COMMON
{
	#include "common/shared.hlsl" // This should always be the first include in COMMON
	#include "system.fxc"
	#include "common.fxc"

	struct VS_INPUT
	{
		float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
		float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
	};

	struct PixelInput
	{
		float3 vPositionWs : TEXCOORD1;

#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs : SV_Position;
#endif
#if ( PROGRAM == VFX_PROGRAM_PS )
		float4 vPositionSs : SV_ScreenPosition;
#endif
	
#if ( PROGRAM != VFX_PROGRAM_PS ) // VS or GS only
	#if ( D_MULTIVIEW_INSTANCING == 1 )
		float vClip0 : SV_ClipDistance0;
	#elif ( D_MULTIVIEW_INSTANCING == 2 )
		float2 vClip0 : SV_ClipDistance0;
	#endif
#endif
	};
}

VS
{
	//
	// Main
	//
	PixelInput MainVs( VS_INPUT i )
	{
		PixelInput o;

		o.vPositionWs = i.vPositionOs.xyz;

		float flSkyboxScale = g_flNearPlane + g_flFarPlane;
		float3 vPositionWs = g_vCameraPositionWs.xyz + i.vPositionOs.xyz * flSkyboxScale;

		o.vPositionPs.xyzw = Position3WsToPs(vPositionWs.xyz);
		o.vPositionWs.xyz = vPositionWs;

		return o;
	}
}

PS
{
	RenderState( CullMode, NONE );
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, true );
	//RenderState( DepthFunc, LESS_EQUAL );
	
	float g_Seed					<					UiGroup( "Sky,10/General,0/0" );		Default( 68.89 );						Attribute( "Seed" );					>;
	float4 g_SkyColor				< UiType( Color );	UiGroup( "Sky,10/General,0/1" );		Default4( 0.0, 0.06, 0.12, 1.0 );		Attribute( "SkyColor" );				>;

	float4 g_StarColor				< UiType( Color );	UiGroup( "Sky,10/Stars,1/0" );			Default4( 1.0, 1.0, 1.0, 1.0 );			Attribute( "StarColor" );				>;
	float2 g_StarSizeRange			<					UiGroup( "Sky,10/Stars,1/1" );			Default2( 0.6, 0.9 );					Attribute( "StarSizeRange" );			>;

	float g_Layers					<					UiGroup( "Sky,10/Density,2/0" );		Default( 5.0 );							Attribute( "Layers" );					>;
	float g_Density					<					UiGroup( "Sky,10/Density,2/1" );		Default( 1.78 );						Attribute( "Density" );					>;
	float g_DensityMod				<					UiGroup( "Sky,10/Density,2/2" );		Default( 1.75 );						Attribute( "DensityMod" );				>;
	
	float g_Brightness				<					UiGroup( "Sky,10/Brightness,3/0" );		Default( 2.89 );						Attribute( "Brightness" );				>;
	float g_BrightnessMod			<					UiGroup( "Sky,10/Brightness,3/1" );		Default( 3.0 );							Attribute( "BrightnessMod" );			>;

	bool g_EnableBackgroundNoise	<					UiGroup( "Sky,10/Fog,4/0" );			Default( 1 );							Attribute( "EnableBackgroundNoise" );	>;
	float4 g_SkyFogColor1			< UiType( Color );	UiGroup( "Sky,10/Fog,4/1" );			Default4( 0.0, 0.33, 0.34, 1.0 );		Attribute( "SkyFogColor1" );			>;
	float4 g_SkyFogColor2			< UiType( Color );	UiGroup( "Sky,10/Fog,4/2" );			Default4( 0.0, 0.33, 0.34, 1.0 );		Attribute( "SkyFogColor2" );			>;
	float g_SkyFogSeed2				<					UiGroup( "Sky,10/Fog,4/3" );			Default( 48.89 );						Attribute( "SkyFogSeed2" );				>;
	float g_FogNoiseDensity1		<					UiGroup( "Sky,10/Fog,4/4" );			Default( 8.6 );							Attribute( "FogNoiseDensity1" );		>;
	float g_FogNoiseDensity2		<					UiGroup( "Sky,10/Fog,4/5" );			Default( 6.9 );							Attribute( "FogNoiseDensity2" );		>;
	float4 g_FogNoiseParams			<					UiGroup( "Sky,10/Fog,4/6" );			Default4( 0.75, 6.0, 0.795, 2.08 );		Attribute( "FogNoiseParams" );			>;
	float4 g_FogNoiseMaskParams		<					UiGroup( "Sky,10/Fog,4/7" );			Default4( 0.33, 6.0, 0.628, 2.11 );		Attribute( "FogNoiseMaskParams" );		>;
	float4 g_FogNoiseMaskParams2	<					UiGroup( "Sky,10/Fog,4/8" );			Default4( 0.07, -0.001, 0.51, 2.5 );	Attribute( "FogNoiseMaskParams2" );		>;


	#define PI 3.141592653589793238462

	float hash13(float3 p3)
	{
		p3 = frac(p3 * .1031);
		p3 += dot(p3, p3.zyx + 31.32);
		return frac((p3.x + p3.y) * p3.z);
	}

	float2 hash22(float2 p)
	{
		float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
		p3 += dot(p3, p3.yzx + 33.33);
		return frac((p3.xx + p3.yz) * p3.zy);
	}

	float noise13(float3 x)
	{
		float3 root = floor(x);
			
		float3 f = smoothstep(0.0, 1.0, frac(x));

		float n000 = hash13(root + float3(0, 0, 0));
		float n001 = hash13(root + float3(0, 0, 1));
		float n010 = hash13(root + float3(0, 1, 0));
		float n011 = hash13(root + float3(0, 1, 1));
		float n100 = hash13(root + float3(1, 0, 0));
		float n101 = hash13(root + float3(1, 0, 1));
		float n110 = hash13(root + float3(1, 1, 0));
		float n111 = hash13(root + float3(1, 1, 1));

		float n00 = lerp(n000, n001, f.z);
		float n01 = lerp(n010, n011, f.z);
		float n10 = lerp(n100, n101, f.z);
		float n11 = lerp(n110, n111, f.z);

		float n0 = lerp(n00, n01, f.y);
		float n1 = lerp(n10, n11, f.y);

		float n = lerp(n0, n1, f.x);

		return n;
	}


	float layeredNoise13(float3 x, float iterations, float alphaMod, float sizeMod)
	{
		float noise = 0.0;
		float maximum = 0.0;
		for (float i = 0.0; i <= iterations; i += 1.0)
		{
			noise += noise13(x * pow(sizeMod, i)) * pow(alphaMod, i);
			maximum += pow(alphaMod, i);
		}

		return noise / maximum;
	}



	float stars(float3 rayDir, float sphereRadius, float sizeMod, float layer)
	{
		float3 spherePoint = rayDir * sphereRadius;

		float upAtan = atan2(spherePoint.y, length(spherePoint.xz)) + 4.0 * PI;

		float starSpaces = 1.0 / sphereRadius;
		float starSize = (sphereRadius * 0.0015) * fwidth(upAtan) * 1000.0 * sizeMod;
		upAtan -= fmod(upAtan, starSpaces) - starSpaces * 0.5;

		float numberOfStars = floor(sqrt(pow(sphereRadius, 2.0) * (1.0 - pow(sin(upAtan), 2.0))) * 3.0);
			
		float planeAngle = atan2(spherePoint.z, spherePoint.x) + 4.0 * PI;
		planeAngle = planeAngle - fmod(planeAngle, PI / numberOfStars);

    float2 randomPosition = hash22(float2(planeAngle, upAtan) + g_Seed);

		float starLevel = sin(upAtan + starSpaces * (randomPosition.y - 0.5) * (1.0 - starSize)) * sphereRadius;
		float starDistanceToYAxis = sqrt(sphereRadius * sphereRadius - starLevel * starLevel);
		float starAngle = planeAngle + (PI * (randomPosition.x * (1.0 - starSize) + starSize * 0.5) / numberOfStars);
		float3 starCenter = float3(cos(starAngle) * starDistanceToYAxis, starLevel, sin(starAngle) * starDistanceToYAxis);

		float star = smoothstep(starSize, 0.0, distance(starCenter, spherePoint));

		return star;
	}

	float starModFromI(float i)
	{
		return lerp(g_StarSizeRange.y, g_StarSizeRange.x, smoothstep(1.0, g_Layers, i));
	}

	float fog(float3 ray, float seed, float density)
	{
		float3 p = ray * density + seed;
		float noise = layeredNoise13(p * g_FogNoiseParams.x, g_FogNoiseParams.y, g_FogNoiseParams.z, g_FogNoiseParams.w);
		float noise2 = layeredNoise13(p * g_FogNoiseMaskParams.x * 0.05 + 21.32, g_FogNoiseMaskParams.y , g_FogNoiseMaskParams.z , g_FogNoiseMaskParams.w);
		noise2 = pow(smoothstep(g_FogNoiseMaskParams2.x, g_FogNoiseMaskParams2.y, abs(noise2 - g_FogNoiseMaskParams2.z)), g_FogNoiseMaskParams2.w);
	
		return noise2 * noise;
	}

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		// Generate Object->World matrix and animation scale
		float3 vPositionWs = i.vPositionWs.xyz;
		float3 vPositionSs = i.vPositionSs.xyz;
		float3 vRay = normalize(vPositionWs - g_vCameraPositionWs);
		//float3 vCamDir = g_vCameraDirWs;
	
	
		float star = 0.0;
		for (float i = 1.0; i <= g_Layers; i += 1.0)
		{
			star += stars(vRay, g_Density * pow(g_DensityMod, i), starModFromI(i), i) * (1.0 / pow(g_BrightnessMod, i));
	    }
	
		half4 skyColor = g_SkyColor / 255.0;
		half4 skyFogColor1 = g_SkyFogColor1 / 255.0;
		half4 skyFogColor2 = g_SkyFogColor2 / 255.0;
	
		if ( g_EnableBackgroundNoise )
		{	  
			float f1 = fog( vRay, g_Seed, g_FogNoiseDensity1 );
			float f2 = fog( vRay, g_SkyFogSeed2, g_FogNoiseDensity2 );
			skyColor += skyFogColor1 * f1 + skyFogColor2 * f2 * 2.0;
		}
	
		float4 sky = g_StarColor * star * g_Brightness + skyColor;
	
		return sky; //float4(hash22(vPositionSs.xy), 0.0, 1.0);

	}
}