HEADER
{
	Description = "Blit camera views";
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
	#include "vr_common.fxc"

	struct VS_INPUT
	{
		float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
		float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
	};

	struct PixelInput
	{
		float2 uv 	: TEXCOORD0;
		float4 Position		: SV_POSITION;
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
        o.Position = float4(i.vPositionOs.xyz, 1.0f);
		//o.vPositionSs = float4(i.vPositionOs.xyz, 1.0f);
        o.uv = i.vTexCoord;

		return o;
	}
}

PS
{
	//Blend SrcAlpha OneMinusSrcAlpha
	//RenderState( BlendEnable, false );

	//Cull Off ZWrite Off ZTest Always
	RenderState( CullMode, NONE );
    RenderState( DepthWriteEnable, false );
    RenderState( DepthEnable, false );

	CreateTexture2D( _ColorBuffer ) < Attribute( "ColorBuffer" ); SrgbRead( false ); AddressU( CLAMP ); AddressV( CLAMP ); >;
	CreateTexture2D( _DepthTexture ) < Attribute( "DepthTexture" ); SrgbRead( true ); Filter( POINT ); >;

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		return Tex2D(_ColorBuffer, i.uv);
	}
}