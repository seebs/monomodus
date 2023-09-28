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
    float2 ColorCoords	: TEXCOORD0;
    float2 TextureCoords: TEXCOORD1;
};

struct FlatToPixel
{
    float4 Position   	: POSITION;    
    float2 ColorCoords	: TEXCOORD0;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Texture Samplers --------

Texture xPalette;
sampler PaletteSampler = sampler_state { texture = <xPalette>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = wrap; AddressV = clamp;};
Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};
float4x4 xTranslate;
float xAlpha; // global multiplier to apply to everything in this drawing path
// we want 0 to be the center of the "top left" pixel, and 1 to be the center
// of the bottom right. for instance, for alpha, where there's two pixels, the
// top pixel is 0-0.5, so the center is 0.25, and the bottom pixel is 0.5-1.0,
// so the center is 0.75. for color, where there's 6, the leftmost pixel is 0-1/6,
// so we want 0 to be 1/12, and the rightmost is 5/6-1, so we want the highest
// value to be 11/12. but we're getting in integers scaled to an arbitrary
// palette size, so if the palette size is 6, we want 0-5 to cover that, and
// if it's 12, we want 0-11 to cover that, and so on. so we want 5 times color
// divided by palette size, plus 1/12.... i think.
// if we're getting in an input color, we want to convert it into a 1-N range,
float xPaletteSize;

// okay let's see
TxToPixel TxVS( float2 inPos : POSITION, float2 inColor: TEXCOORD0, float2 inTexCoords: TEXCOORD1)
{
	TxToPixel Output = (TxToPixel)0;
	float4 inPos4 = float4(inPos[0], inPos[1], 0, 1);
	Output.Position = mul(inPos4, xTranslate);
	Output.ColorCoords[0] = (inColor[0] * 5 / xPaletteSize) + (1.0 / 12.0);
	Output.ColorCoords[1] = inColor[1] / 2 + 0.25;
	Output.TextureCoords = inTexCoords;
    
	return Output;    
}

PixelToFrame TxPS(TxToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = tex2D(PaletteSampler, PSIn.ColorCoords) * tex2D(TextureSampler, PSIn.TextureCoords) * xAlpha;

	return Output;
}

FlatToPixel FlatVS(float2 inPos : POSITION, float2 inColor: TEXCOORD0)
{
	FlatToPixel Output = (FlatToPixel)0;
	float4 inPos4 = float4(inPos[0], inPos[1], 0, 1);
	Output.Position = mul(inPos4, xTranslate);
	Output.ColorCoords[0] = (inColor[0] * 5 / xPaletteSize) + (1.0 / 12.0);
	Output.ColorCoords[1] = inColor[1] / 2 + 0.25;
    
	return Output;    
}

PixelToFrame FlatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
	
	Output.Color = tex2D(PaletteSampler, PSIn.ColorCoords) * xAlpha;

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