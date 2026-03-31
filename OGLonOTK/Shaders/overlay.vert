#version 330 core

layout(location = 0) in vec2 aPosition;

uniform mat4 projection;
uniform mat4 model;

void main()
{
    gl_Position = vec4(aPosition, 0.0, 1.0) * model * projection;
}