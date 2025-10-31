Shader "Custom/SkyrimGreaterWard"
{
    Properties
    {
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _CenterColor ("Center Color", Color) = (0.4, 0.7, 1.0, 0.3)
        _EdgeColor ("Edge Glow Color", Color) = (0.8, 0.95, 1.0, 1.0)
        _BrightGlowColor ("Bright Glow", Color) = (1.0, 1.0, 1.0, 1.0)
        
        // Animation
        _FlowSpeed ("Flow Speed", Range(0.0, 3.0)) = 1.0
        _RadialSpeed ("Radial Expansion Speed", Range(0.0, 2.0)) = 0.5
        _RotationSpeed ("Rotation Speed", Range(-2.0, 2.0)) = 0.3
        
        // Crystal/Ice Spikes
        _SpikeCount ("Spike Count", Range(8.0, 32.0)) = 16.0
        _SpikeSharpness ("Spike Sharpness", Range(0.1, 10.0)) = 3.0
        _SpikeLength ("Spike Length", Range(0.0, 0.5)) = 0.15
        _SpikeVariation ("Spike Variation", Range(0.0, 1.0)) = 0.3
        
        // Glow & Energy
        _EdgeGlowPower ("Edge Glow Power", Range(0.1, 10.0)) = 2.0
        _EdgeGlowIntensity ("Edge Glow Intensity", Range(0.0, 10.0)) = 5.0
        _InnerGlowRadius ("Inner Glow Radius", Range(0.0, 1.0)) = 0.4
        
        // Noise & Detail
        _NoiseScale ("Noise Scale", Range(1.0, 50.0)) = 15.0
        _NoiseIntensity ("Noise Intensity", Range(0.0, 2.0)) = 0.8
        _DetailScale ("Detail Scale", Range(1.0, 100.0)) = 30.0
        
        // Transparency
        _CenterOpacity ("Center Opacity", Range(0.0, 1.0)) = 0.2
        _EdgeOpacity ("Edge Opacity", Range(0.0, 1.0)) = 0.9
        
        // Distortion
        _Distortion ("Distortion Amount", Range(0.0, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };
            
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST;
            
            float4 _CenterColor;
            float4 _EdgeColor;
            float4 _BrightGlowColor;
            
            float _FlowSpeed;
            float _RadialSpeed;
            float _RotationSpeed;
            
            float _SpikeCount;
            float _SpikeSharpness;
            float _SpikeLength;
            float _SpikeVariation;
            
            float _EdgeGlowPower;
            float _EdgeGlowIntensity;
            float _InnerGlowRadius;
            
            float _NoiseScale;
            float _NoiseIntensity;
            float _DetailScale;
            
            float _CenterOpacity;
            float _EdgeOpacity;
            
            float _Distortion;
            
            // Rotate UV around center
            float2 rotateUV(float2 uv, float2 center, float angle)
            {
                float2 shifted = uv - center;
                float s = sin(angle);
                float c = cos(angle);
                float2 rotated = float2(
                    shifted.x * c - shifted.y * s,
                    shifted.x * s + shifted.y * c
                );
                return rotated + center;
            }
            
            // Convert to polar coordinates
            float2 cartesianToPolar(float2 uv, float2 center)
            {
                float2 delta = uv - center;
                float radius = length(delta);
                float angle = atan2(delta.y, delta.x);
                return float2(radius, angle);
            }
            
            // Random function for spike variation
            float random(float seed)
            {
                return frac(sin(seed * 12.9898) * 43758.5453);
            }
            
            // Create crystal spike pattern
            float createSpikes(float2 polar, float time)
            {
                float radius = polar.x;
                float angle = polar.y;
                
                // Normalize angle to 0-1 range
                float normalizedAngle = (angle + 3.14159265) / (2.0 * 3.14159265);
                
                // Create spikes using sine wave
                float spikePattern = sin(normalizedAngle * _SpikeCount * 3.14159265 * 2.0);
                
                // Add variation to each spike
                float spikeIndex = floor(normalizedAngle * _SpikeCount);
                float spikeRandom = random(spikeIndex);
                float spikeVariation = lerp(1.0, spikeRandom, _SpikeVariation);
                
                // Sharpen the spikes
                spikePattern = pow(abs(spikePattern), 1.0 / _SpikeSharpness) * sign(spikePattern);
                
                // Animate spikes - pulse outward
                float pulsate = sin(time * _RadialSpeed * 2.0 + spikeIndex * 0.5) * 0.5 + 0.5;
                float spikeLength = _SpikeLength * spikeVariation * (0.7 + pulsate * 0.3);
                
                // Apply spikes at the edge
                float edgeStart = 0.5;
                float edgeEnd = edgeStart + spikeLength;
                
                if (radius > edgeStart && radius < edgeEnd)
                {
                    float edgeProgress = (radius - edgeStart) / spikeLength;
                    float spikeMask = smoothstep(0.0, 0.2, spikePattern) * (1.0 - edgeProgress);
                    return spikeMask;
                }
                
                return 0.0;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTexture);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float time = _Time.y;
                
                // === RADIAL COORDINATES ===
                float2 centeredUV = i.uv - center;
                float radius = length(centeredUV);
                float2 polar = cartesianToPolar(i.uv, center);
                float angle = polar.y;
                
                // === ROTATION ===
                float2 rotatedUV = rotateUV(i.uv, center, time * _RotationSpeed);
                
                // === NOISE LAYERS ===
                // Primary noise - flowing outward
                float2 noiseUV1 = rotatedUV * _NoiseScale;
                noiseUV1 += float2(0, -time * _FlowSpeed * 0.5);
                float noise1 = tex2D(_NoiseTexture, noiseUV1).r;
                
                // Secondary noise - rotating
                float2 noiseUV2 = rotateUV(i.uv, center, time * _FlowSpeed * 0.3) * _NoiseScale * 0.7;
                float noise2 = tex2D(_NoiseTexture, noiseUV2).r;
                
                // Detail noise - fine crystalline structure
                float2 detailUV = i.uv * _DetailScale + time * _FlowSpeed * 0.2;
                float detailNoise = tex2D(_NoiseTexture, detailUV).r;
                
                // Combine noise
                float combinedNoise = (noise1 * 0.5 + noise2 * 0.3 + detailNoise * 0.2);
                
                // === DISTORTION ===
                float2 distortion = (combinedNoise - 0.5) * _Distortion;
                float2 distortedUV = i.uv + distortion;
                float2 distortedPolar = cartesianToPolar(distortedUV, center);
                float distortedRadius = distortedPolar.x;
                
                // === RADIAL FLOW PATTERN ===
                // Create expanding rings
                float rings = frac(distortedRadius * 8.0 - time * _RadialSpeed);
                rings = smoothstep(0.3, 0.7, rings);
                
                // === CRYSTAL SPIKES ===
                float spikes = createSpikes(polar, time);
                
                // === EDGE GLOW ===
                // Strong glow at edges (like the reference image)
                float edgeGlow = pow(radius, _EdgeGlowPower) * _EdgeGlowIntensity;
                edgeGlow *= smoothstep(0.3, 0.7, radius); // Only glow at outer areas
                
                // === INNER TRANSLUCENT AREA ===
                float innerArea = 1.0 - smoothstep(0.0, _InnerGlowRadius, radius);
                
                // === FRESNEL (View-dependent glow) ===
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(fresnel, 2.0) * 2.0;
                
                // === COLOR MIXING ===
                // Base color gradient from center to edge
                float4 baseColor = lerp(_CenterColor, _EdgeColor, smoothstep(0.2, 0.8, radius));
                
                // Add noise variation to color
                baseColor.rgb *= (0.8 + combinedNoise * _NoiseIntensity * 0.4);
                
                // Add flowing energy rings
                baseColor.rgb += rings * _EdgeColor.rgb * 0.3;
                
                // Add bright edge glow
                float4 glowColor = _BrightGlowColor * edgeGlow;
                baseColor += glowColor;
                
                // Add spike glow (bright white/cyan at crystal edges)
                baseColor.rgb += spikes * _BrightGlowColor.rgb * 3.0;
                
                // Add fresnel rim
                baseColor.rgb += fresnel * _EdgeColor.rgb * 0.5;
                
                // === ALPHA CALCULATION ===
                // Center is more transparent, edges are more opaque
                float alpha = lerp(_CenterOpacity, _EdgeOpacity, smoothstep(0.1, 0.8, radius));
                
                // Reduce alpha in very center (portal-like)
                alpha *= smoothstep(0.0, 0.15, radius);
                
                // Add noise variation to alpha
                alpha *= (0.7 + combinedNoise * 0.3);
                
                // Spikes are more opaque
                alpha += spikes * 0.7;
                
                // Edge glow increases opacity
                alpha += edgeGlow * 0.2;
                
                // Flowing rings affect alpha
                alpha += rings * 0.2;
                
                // Fresnel affects alpha at edges
                alpha += fresnel * 0.3 * smoothstep(0.4, 1.0, radius);
                
                // Fade out at extreme outer edge for soft blend
                alpha *= 1.0 - smoothstep(0.75, 1.0, radius);
                
                baseColor.a = saturate(alpha);
                
                return baseColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}