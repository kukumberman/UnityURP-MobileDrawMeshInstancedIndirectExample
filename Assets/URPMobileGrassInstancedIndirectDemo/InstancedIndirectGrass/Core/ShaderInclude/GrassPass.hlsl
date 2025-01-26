half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
{
    half3 H = normalize(light.direction + V);

    //direct diffuse 
    half directDiffuse = dot(N, light.direction) * 0.5 + 0.5; //half lambert, to fake grass SSS

    //direct specular
    float directSpecular = saturate(dot(N, H));
    //pow(directSpecular,8)
    directSpecular *= directSpecular;
    directSpecular *= directSpecular;
    directSpecular *= directSpecular;
    //directSpecular *= directSpecular; //enable this line = change to pow(directSpecular,16)

    //add direct directSpecular to result
    directSpecular *= 0.1 * positionOSY; //only apply directSpecular to grass's top area, to simulate grass AO

    half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
    half3 result = (albedo * directDiffuse + directSpecular) * lighting;
    return result;
}

float GetWind(float3 perGrassPivotPosWS)
{
    float wind = 0;
    wind += (sin(_Time.y * _WindAFrequency + perGrassPivotPosWS.x * _WindATiling.x + perGrassPivotPosWS.z * _WindATiling.y) * _WindAWrap.x + _WindAWrap.y) * _WindAIntensity;
    wind += (sin(_Time.y * _WindBFrequency + perGrassPivotPosWS.x * _WindBTiling.x + perGrassPivotPosWS.z * _WindBTiling.y) * _WindBWrap.x + _WindBWrap.y) * _WindBIntensity;
    wind += (sin(_Time.y * _WindCFrequency + perGrassPivotPosWS.x * _WindCTiling.x + perGrassPivotPosWS.z * _WindCTiling.y) * _WindCWrap.x + _WindCWrap.y) * _WindCIntensity;
    return wind;
}

Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
{
    Varyings OUT;

    //we pre-transform to posWS in C# now
    uint positionId = _VisibleInstancesOnlyPosWSIDBuffer[instanceID];
    float3 perGrassPivotPosWS = _AllInstancesPosWSBuffer[positionId];

    float perGrassHeight = lerp(2, 5, (sin(perGrassPivotPosWS.x * 23.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55)) * _GrassHeight;

    //get "is grass stepped" data(bending) from RT
    float2 grassBendingUV = ((perGrassPivotPosWS.xz - _PivotPosWS.xz) / _BoundSize.xz) * 0.5 + 0.5; //claculate where is this grass inside bound (can optimize to 2 MAD)
    float stepped = tex2Dlod(_GrassBendingRT, float4(grassBendingUV, 0, 0)).x;

    //rotation(make grass LookAt() camera just like a billboard)
    //=========================================
    float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz; //UNITY_MATRIX_V[0].xyz == world space camera Right unit vector
    float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz; //UNITY_MATRIX_V[1].xyz == world space camera Up unit vector
    float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz; //UNITY_MATRIX_V[2].xyz == -1 * world space camera Forward unit vector

    float randomWidth = 1;
#if APPLY_RANDOM_WIDTH_ON
    //Expand Billboard (billboard Left+right)
    randomWidth = sin(perGrassPivotPosWS.x * 95.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55; //random width from posXZ, min 0.1
#endif
    float3 positionOS = IN.positionOS.x * cameraTransformRightWS * _GrassWidth * randomWidth;

    //Expand Billboard (billboard Up)
    positionOS += IN.positionOS.y * cameraTransformUpWS;
    //=========================================

    //bending by RT (hard code)
    float3 bendDir = cameraTransformForwardWS;
    bendDir.xz *= 0.5; //make grass shorter when bending, looks better
    bendDir.y = min(-0.5, bendDir.y); //prevent grass become too long if camera forward is / near parallel to ground
    positionOS = lerp(positionOS.xyz + bendDir * positionOS.y / -bendDir.y, positionOS.xyz, stepped * 0.95 + 0.05); //don't fully bend, will produce ZFighting

    // Dima: not use it
    //per grass height scale
    // positionOS.y *= perGrassHeight;
    positionOS.y *= _GrassHeight;

    //camera distance scale (make grass width larger if grass is far away to camera, to hide smaller than pixel size triangle flicker)        
    float3 viewWS = _WorldSpaceCameraPos - perGrassPivotPosWS;
    float ViewWSLength = length(viewWS);
    positionOS += cameraTransformRightWS * IN.positionOS.x * max(0, ViewWSLength * 0.0225);

    //move grass posOS -> posWS
    float3 positionWS = positionOS + perGrassPivotPosWS;

    //wind animation (biilboard Left Right direction only sin wave)            
    float wind = GetWind(perGrassPivotPosWS);
    wind *= IN.positionOS.y; //wind only affect top region, don't affect root region
    float3 windOffset = cameraTransformRightWS * wind; //swing using billboard left right direction
    positionWS.xyz += windOffset;

    //vertex position logic done, complete posWS -> posCS
    OUT.positionCS = TransformWorldToHClip(positionWS);

    /////////////////////////////////////////////////////////////////////
    //lighting & color
    /////////////////////////////////////////////////////////////////////

    //lighting data
    Light mainLight;
#if _MAIN_LIGHT_SHADOWS
    mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
#else
    mainLight = GetMainLight();
#endif
    //random normal per grass
    half3 randomAddToN = (_RandomNormal * sin(perGrassPivotPosWS.x * 82.32523 + perGrassPivotPosWS.z) + wind * -0.25) * cameraTransformRightWS;

    //default grass's normal is pointing 100% upward in world space, it is an important but simple grass normal trick
    //-apply random to normal else lighting is too uniform
    //-apply cameraTransformForwardWS to normal because grass is billboard
    half3 N = normalize(half3(0, 1, 0) + randomAddToN - cameraTransformForwardWS * 0.5);

    half3 V = viewWS / ViewWSLength;

    half3 baseColor = tex2Dlod(_BaseColorTexture, float4(TRANSFORM_TEX(positionWS.xz, _BaseColorTexture), 0, 0)).rgb * _BaseColor; //sample mip 0 only
    half3 albedo = lerp(_GroundColor, baseColor, IN.positionOS.y);

    //indirect
    half3 lightingResult = SampleSH(0) * albedo;

    //main direct light
    lightingResult += ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);

    // Additional lights loop
#if _ADDITIONAL_LIGHTS

    // Returns the amount of lights affecting the object being renderer.
    // These lights are culled per-object in the forward renderer
    int additionalLightsCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightsCount; ++i)
    {
        // Similar to GetMainLight, but it takes a for-loop index. This figures out the
        // per-object light index and samples the light buffer accordingly to initialized the
        // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
        Light light = GetAdditionalLight(i, positionWS);

        // Same functions used to shade the main light.
        lightingResult += ApplySingleDirectLight(light, N, V, albedo, positionOS.y);
    }
#endif

    //fog
    float fogFactor = ComputeFogFactor(OUT.positionCS.z);
    
    // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
    // with a custom one.
    OUT.color = MixFog(lightingResult, fogFactor);

    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.fogFactor = fogFactor;

    return OUT;
}

half4 frag_color(Varyings IN) : SV_Target
{
    return half4(IN.color, 1);
}

half4 frag_texture(Varyings IN) : SV_Target
{
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
    color.rgb = MixFog(color.rgb, IN.fogFactor);
    clip(color.a - _AlphaClip);
    return color;
}

half4 frag(Varyings IN) : SV_Target
{
#if USE_VERTEX_COLOR_ON
    return frag_color(IN);
#else
    return frag_texture(IN);
#endif
}
