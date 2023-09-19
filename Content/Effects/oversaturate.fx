#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct FlatToPixel
{
    float4 Position : SV_POSITION;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Texture Samplers --------

Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};
float4x4 xTranslate;

FlatToPixel FlatVS(float4 inPos : POSITION)
{
	FlatToPixel Output = (FlatToPixel)0;
    Output.Position = inPos;
    
	return Output;    
}

PixelToFrame FlatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
    float2 pos;
    pos[0] = PSIn.Position[0];
    pos[1] = PSIn.Position[1];	
	
	Output.Color = tex2D(TextureSampler, pos) * 0.5;

	return Output;
}

technique Flat
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL FlatPS();	
	}
}