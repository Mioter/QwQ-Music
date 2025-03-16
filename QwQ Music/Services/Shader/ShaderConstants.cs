namespace QwQ_Music.Services.Shader;

/// <summary>
/// 着色器常量定义
/// </summary>
public static class ShaderConstants
{
    /// <summary>
    /// 波浪扭曲着色器
    /// </summary>
    public const string WaveWarpShader = """

                                         uniform vec3 iResolution;
                                         uniform float iTime;
                                         uniform float iTimeDelta;
                                         uniform float iFrameRate;
                                         uniform int iFrame;
                                         uniform vec4 iMouse;

                                         // 平滑过渡函数
                                         float smoothstep(float edge0, float edge1, float x) {
                                             float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                                             return t * t * (3.0 - 2.0 * t);
                                         }

                                         #define S(a,b,t) smoothstep(a,b,t)

                                         mat2 Rot(float a)
                                         {
                                             float s = sin(a);
                                             float c = cos(a);
                                             return mat2(c, -s, s, c);
                                         }

                                         // Created by inigo quilez - iq/2014
                                         // License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
                                         vec2 hash(vec2 p)
                                         {
                                             p = vec2(dot(p,vec2(2127.1,81.17)), dot(p,vec2(1269.5,283.37)));
                                             return fract(sin(p)*43758.5453);
                                         }

                                         float noise(in vec2 p)
                                         {
                                             vec2 i = floor(p);
                                             vec2 f = fract(p);
                                             
                                             vec2 u = f*f*(3.0-2.0*f);
                                         
                                             float n = mix(
                                                         mix(
                                                             dot(-1.0+2.0*hash(i + vec2(0.0,0.0)), f - vec2(0.0,0.0)),
                                                             dot(-1.0+2.0*hash(i + vec2(1.0,0.0)), f - vec2(1.0,0.0)),
                                                             u.x
                                                         ),
                                                         mix(
                                                             dot(-1.0+2.0*hash(i + vec2(0.0,1.0)), f - vec2(0.0,1.0)),
                                                             dot(-1.0+2.0*hash(i + vec2(1.0,1.0)), f - vec2(1.0,1.0)),
                                                             u.x
                                                         ),
                                                         u.y
                                                       );
                                             return 0.5 + 0.5*n;
                                         }

                                         vec4 main(vec2 fragCoord) {
                                             vec2 uv = fragCoord/iResolution.xy;
                                             float ratio = iResolution.x / iResolution.y;
                                         
                                             vec2 tuv = uv;
                                             tuv -= 0.5;
                                         
                                             // rotate with Noise
                                             float degree = noise(vec2(iTime*0.1, tuv.x*tuv.y));
                                         
                                             tuv.y *= 1.0/ratio;
                                             tuv *= Rot(radians((degree-0.5)*720.0+180.0));
                                             tuv.y *= ratio;
                                         
                                             // Wave warp with sin
                                             float frequency = 5.0;
                                             float amplitude = 30.0;
                                             float speed = iTime * 2.0;
                                             tuv.x += sin(tuv.y*frequency+speed)/amplitude;
                                             tuv.y += sin(tuv.x*frequency*1.5+speed)/(amplitude*0.5);
                                             
                                             // draw the image
                                             vec3 colorYellow = vec3(0.957, 0.804, 0.623);
                                             vec3 colorDeepBlue = vec3(0.192, 0.384, 0.933);
                                             vec3 layer1 = mix(colorYellow, colorDeepBlue, S(-0.3, 0.2, (tuv*Rot(radians(-5.0))).x));
                                             
                                             vec3 colorRed = vec3(0.910, 0.510, 0.8);
                                             vec3 colorBlue = vec3(0.350, 0.71, 0.953);
                                             vec3 layer2 = mix(colorRed, colorBlue, S(-0.3, 0.2, (tuv*Rot(radians(-5.0))).x));
                                             
                                             vec3 finalComp = mix(layer1, layer2, S(0.5, -0.3, tuv.y));
                                             
                                             vec3 col = finalComp;
                                             
                                             return vec4(col, 1.0);
                                         }

                                         """;
}