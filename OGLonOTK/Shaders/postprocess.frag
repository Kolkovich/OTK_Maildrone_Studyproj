#version 330 core

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform bool blurEnabled;
uniform vec2 texelSize;

void main()
{
    if (!blurEnabled)
    {
        FragColor = texture(screenTexture, vTexCoord);
        return;
    }

    vec3 result = vec3(0.0);

    result += texture(screenTexture, vTexCoord + texelSize * vec2(-1, -1)).rgb;
    result += texture(screenTexture, vTexCoord + texelSize * vec2( 0, -1)).rgb;
    result += texture(screenTexture, vTexCoord + texelSize * vec2( 1, -1)).rgb;

    result += texture(screenTexture, vTexCoord + texelSize * vec2(-1,  0)).rgb;
    result += texture(screenTexture, vTexCoord + texelSize * vec2( 0,  0)).rgb;
    result += texture(screenTexture, vTexCoord + texelSize * vec2( 1,  0)).rgb;

    result += texture(screenTexture, vTexCoord + texelSize * vec2(-1,  1)).rgb;
    result += texture(screenTexture, vTexCoord + texelSize * vec2( 0,  1)).rgb;
    result += texture(screenTexture, vTexCoord + texelSize * vec2( 1,  1)).rgb;

    result /= 9.0;

    FragColor = vec4(result, 1.0);
}