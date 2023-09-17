#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
    float2 TextureCoords: TEXCOORD1;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Texture Samplers --------

Texture xTexture;
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};


// okay let's see
VertexToPixel okayVS( float4 inPos : POSITION, float4 inColor: COLOR, float2 inTexCoords: TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;
	Output.Position = inPos;
	Output.Color = inColor;
	Output.TextureCoords = inTexCoords;
    
	return Output;    
}

PixelToFrame okayPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = PSIn.Color * tex2D(TextureSampler, PSIn.TextureCoords);

	return Output;
}

technique Okay
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL okayVS();
		PixelShader  = compile PS_SHADERMODEL okayPS();	
	}
}