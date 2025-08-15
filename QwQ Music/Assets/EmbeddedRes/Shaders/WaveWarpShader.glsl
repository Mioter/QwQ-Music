uniform vec3 iResolution;
uniform float iTime;
uniform float iTimeDelta;
uniform float iFrameRate;
uniform int iFrame;
uniform vec4 iMouse;

// 自定义颜色
uniform vec4 iColor0;
uniform vec4 iColor1;
uniform vec4 iColor2;
uniform vec4 iColor3;
uniform int iColorCount;

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

// 获取自定义颜色或默认颜色
vec3 getColor(int index) {
    if (iColorCount <= 0) {
        // 默认颜色
        if (index == 0) return vec3(0.957, 0.804, 0.623); // colorYellow
        if (index == 1) return vec3(0.192, 0.384, 0.933); // colorDeepBlue
        if (index == 2) return vec3(0.910, 0.510, 0.8);   // colorRed
        return vec3(0.350, 0.71, 0.953);                  // colorBlue
    }
    
    // 使用自定义颜色
    if (index == 0) return iColor0.rgb;
    if (index == 1 && iColorCount > 1) return iColor1.rgb;
    if (index == 2 && iColorCount > 2) return iColor2.rgb;
    if (index == 3 && iColorCount > 3) return iColor3.rgb;
    
    // 如果索引超出范围，使用固定逻辑代替循环
    // 由于我们最多支持4种颜色，所以可以用硬编码方式处理
    if (iColorCount == 1) {
        return iColor0.rgb; // 只有一种颜色时全部返回第一种
    } else if (iColorCount == 2) {
        // 两种颜色时交替使用
        if (index == 2 || index == 0) return iColor0.rgb;
        return iColor1.rgb;
    } else if (iColorCount == 3) {
        // 三种颜色时循环使用
        if (index == 3 || index == 0) return iColor0.rgb;
        if (index == 4 || index == 1) return iColor1.rgb;
        return iColor2.rgb;
    } else {
        // 四种颜色时循环使用
        if (index == 4 || index == 0) return iColor0.rgb;
        if (index == 5 || index == 1) return iColor1.rgb;
        if (index == 6 || index == 2) return iColor2.rgb;
        return iColor3.rgb;
    }
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
    
    // draw the image using custom colors
    vec3 colorYellow = getColor(0);
    vec3 colorDeepBlue = getColor(1);
    vec3 layer1 = mix(colorYellow, colorDeepBlue, smoothstep(-0.3, 0.2, (tuv*Rot(radians(-5.0))).x));
    
    vec3 colorRed = getColor(2);
    vec3 colorBlue = getColor(3);
    vec3 layer2 = mix(colorRed, colorBlue, smoothstep(-0.3, 0.2, (tuv*Rot(radians(-5.0))).x));
    
    vec3 finalComp = mix(layer1, layer2, smoothstep(0.5, -0.3, tuv.y));
    
    vec3 col = finalComp;
    
    return vec4(col, 1.0);
}