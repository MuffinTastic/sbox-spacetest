HEADER
{
	Description = "Blit portal views";
}

MODES
{
    Default();
    VrForward();
}

COMMON
{
    #include "system.fxc"
	#include "common.fxc"

	struct VS_INPUT
	{
		float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
		float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
	};

	struct PixelInput
	{
		float2 uv 	: TEXCOORD0;
#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs : SV_Position;
#endif
#if ( PROGRAM == VFX_PROGRAM_PS )
		float4 vPositionSs : SV_Position;
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
        //o.Position = float4(i.vPositionOs.xyz, 1.0f);
		o.vPositionPs = Position3WsToPs(i.vPositionOs);
		//o.vPositionSs = float4(i.vPositionOs.xyz, 1.0f);
        o.uv = i.vTexCoord;

		return o;
	}
}

PS
{
	DynamicCombo( D_FRONT, 0..1, Sys( PC ) );

	//Blend SrcAlpha OneMinusSrcAlpha
	RenderState( BlendEnable, false );

	//Cull Off ZWrite Off ZTest Always

	#if D_FRONT == 1
	RenderState( CullMode, FRONT );
	#else
	RenderState( CullMode, BACK );
	#endif

    RenderState( DepthWriteEnable, true );
    RenderState( DepthEnable, true );


	CreateTexture2D( g_tColorBuffer ) < Attribute( "ColorBuffer" ); SrgbRead( false ); AddressU( CLAMP ); AddressV( CLAMP ); >;

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 screenUv = CalculateViewportUv(i.vPositionSs.xy);
		float4 color = Tex2D(g_tColorBuffer, screenUv) * float4(0.1, 0.1, 0.1, 1.0);
		return color;// * float4(0.1, 0.1, 0.1, 1.0);
	}
}