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
    float2 texCoord : TEXCOORD0;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Texture Samplers --------

Texture2D xTexture : register(s0);
Texture2D xHighlightTexture : register(s1);
Texture2D xBlurTexture : register(s2);
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = CLAMP; AddressV = CLAMP;};
sampler HighlightSampler  = sampler_state { texture = <xHighlightTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = CLAMP; AddressV = CLAMP;};
sampler BlurSampler  = sampler_state { texture = <xBlurTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = CLAMP; AddressV = CLAMP;};
float4x4 xTranslate;
float2 xScale;
float xAlpha;

FlatToPixel FlatVS(float4 inPos : POSITION)
{
	FlatToPixel Output = (FlatToPixel)0;
    Output.Position = mul(inPos, xTranslate);
    float2 coord = (((float2) inPos + 1) / 2);
    coord[1] = 1 - coord[1];
    Output.texCoord = coord;    
	return Output;    
}

PixelToFrame FlatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
	
	Output.Color = tex2D(TextureSampler, PSIn.texCoord);

	return Output;
}

PixelToFrame DesatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
	
    float4 c = tex2D(TextureSampler, PSIn.texCoord);
	float scale = max(max(c.r, c.g), max(c.b, 1));
    Output.Color.rgb = c.rgb / scale;
    if (scale > 1) {
        Output.Color += scale / 20;
    }
    Output.Color.a = c.a;

	return Output;
}

PixelToFrame ExtractPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
	
    float4 c = tex2D(TextureSampler, PSIn.texCoord);
	float scale = max(max(c.r, c.g), c.b);
	if (scale > 0.8) {
    	Output.Color = (scale - 0.8) / 2;
	}
    Output.Color.a = 1;
	return Output;
}

PixelToFrame BlurXPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;

    float4 cZ = tex2D(TextureSampler, PSIn.texCoord);
    float4 cm3 = tex2D(TextureSampler, PSIn.texCoord + float2(-3, 0) * xScale);
    float4 cm2 = tex2D(TextureSampler, PSIn.texCoord + float2(-2, 0) * xScale);
    float4 cm1 = tex2D(TextureSampler, PSIn.texCoord + float2(-1, 0) * xScale);
	float4 cp3 = tex2D(TextureSampler, PSIn.texCoord + float2(3, 0) * xScale);
    float4 cp2 = tex2D(TextureSampler, PSIn.texCoord + float2(2, 0) * xScale);
    float4 cp1 = tex2D(TextureSampler, PSIn.texCoord + float2(1, 0) * xScale);
    float4 c = ((cm3 + cp3) * 0.0625 + (cm2 + cp2) * 0.125 + (cm1 + cp1) * 0.25 + cZ * 0.3) / 2;
	float scale = max(max(c.r, c.g), max(c.b, 1));
	Output.Color = c / scale;
    Output.Color.a = 1;
	return Output;
}

PixelToFrame BlurYPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;

    float4 cZ = tex2D(TextureSampler, PSIn.texCoord);
    float4 cm3 = tex2D(TextureSampler, PSIn.texCoord + float2(0, -3) * xScale);
    float4 cm2 = tex2D(TextureSampler, PSIn.texCoord + float2(0, -2) * xScale);
    float4 cm1 = tex2D(TextureSampler, PSIn.texCoord + float2(0, -1) * xScale);
	float4 cp3 = tex2D(TextureSampler, PSIn.texCoord + float2(0, 3) * xScale);
    float4 cp2 = tex2D(TextureSampler, PSIn.texCoord + float2(0, 2) * xScale);
    float4 cp1 = tex2D(TextureSampler, PSIn.texCoord + float2(0, 1) * xScale);
    Output.Color = (cm3 + cp3) * 0.0625 + (cm2 + cp2) * 0.125 + (cm1 + cp1) * 0.25 + cZ * 0.3;
    Output.Color.a = 1;
	return Output;
}

PixelToFrame CombinePS(FlatToPixel PSIn) 
{
    PixelToFrame Output = (PixelToFrame)0;
	
    float4 base = tex2D(TextureSampler, PSIn.texCoord);
    float scale = max(max(base.r, base.g), max(base.b, 1));
    base /= scale;
    float4 highlight = tex2D(HighlightSampler, PSIn.texCoord);
    float4 blur = tex2D(BlurSampler, PSIn.texCoord);
    float grey = highlight.r / 6;
    blur *= 1 - saturate(base);
    Output.Color = base + grey + blur;
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

technique Desat
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL DesatPS();	
	}
}

technique ExtractHighlight
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL ExtractPS();	
	}
}

technique BlurX
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL BlurXPS();	
	}
}

technique BlurY
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL BlurYPS();	
	}
}

technique Combine
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL CombinePS();	
	}
}