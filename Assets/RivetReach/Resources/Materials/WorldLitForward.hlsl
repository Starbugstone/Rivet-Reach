            float4 _RRFogColour,_RRFogRange;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;};
            V Vert(A i)
            {
                UNITY_SETUP_INSTANCE_ID(i);V o;o.positionWS=TransformObjectToWorld(i.positionOS);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.tangentWS=float4(TransformObjectToWorldDir(i.tangentOS.xyz),i.tangentOS.w*GetOddNegativeScale());o.uv=i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;return o;
            }
            half4 Frag(V i):SV_Target
            {
                #if defined(RR_MACHINE_NEAR) || defined(RR_MACHINE_FAR)
                RRPresentationClip(i.positionCS,i.positionWS);
                #endif
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(albedo.a-_Cutoff);
                #endif
                float3 n=normalize(i.normalWS);
                #if defined(_NORMALMAP)
                half3 nt=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                float3 tangent=normalize(i.tangentWS.xyz);n=normalize(mul(nt,float3x3(tangent,cross(n,tangent)*i.tangentWS.w,n)));
                #endif
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);input.shadowMask=half4(1,1,1,1);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=albedo.rgb;surface.alpha=albedo.a;surface.metallic=_Metallic;surface.smoothness=_Smoothness;surface.occlusion=1;
                #if defined(_METALLICSPECGLOSSMAP)
                half4 metal=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);surface.metallic=metal.r;surface.smoothness*=metal.a;
                #endif
                #if defined(_EMISSION)
                surface.emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb;
                #endif
                half4 colour=RRFragmentPBR(input,surface);
                float fog=smoothstep(_RRFogRange.x,_RRFogRange.y,distance(i.positionWS,GetCameraPositionWS()));
                // The fog sample has a different offset from surface lighting. Preserve it
                // wherever fog contributes, and skip its buffer reads at zero weight.
                [branch] if(fog>0)colour.rgb=lerp(colour.rgb,RRCaveFog(_RRFogColour.rgb,i.positionWS+n*.035),fog);return colour;
            }
