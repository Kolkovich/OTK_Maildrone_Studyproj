#version 330 core

uniform vec3 overlayColor;

out vec4 FragColor;

void main()
{
    FragColor = vec4(overlayColor, 1.0);
}