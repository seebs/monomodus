#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct TxToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
    float2 TextureCoords: TEXCOORD1;
};

struct FlatToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Texture Samplers --------

Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};
float4x4 xTranslate;
float xAlpha; // global multiplier to apply to everything in this drawing path

// okay let's see
TxToPixel TxVS( float4 inPos : POSITION, float4 inColor: COLOR, float2 inTexCoords: TEXCOORD0)
{
	TxToPixel Output = (TxToPixel)0;
	Output.Position = mul(inPos, xTranslate);
	Output.Color = inColor;
	Output.TextureCoords = inTexCoords;
    
	return Output;    
}

PixelToFrame TxPS(TxToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = PSIn.Color * tex2D(TextureSampler, PSIn.TextureCoords) * xAlpha;

	return Output;
}

FlatToPixel FlatVS(float4 inPos : POSITION, float4 inColor: COLOR)
{
	FlatToPixel Output = (FlatToPixel)0;
	Output.Position = mul(inPos, xTranslate);
	Output.Color = inColor;
    
	return Output;    
}

PixelToFrame FlatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = PSIn.Color * xAlpha;

	return Output;
}

technique Tx
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL TxVS();
		PixelShader  = compile PS_SHADERMODEL TxPS();	
	}
}

technique Flat
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL FlatPS();	
	}
}