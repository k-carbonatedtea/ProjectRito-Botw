﻿#version 330 core

in vec3 vPosition;
in vec3 vNormalWorld;
in vec4 vVertexColor;

flat out vec3 v_NormalWorld;
flat out vec4 v_VertexColor;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    v_NormalWorld = vNormalWorld;
    v_VertexColor = vVertexColor;

    gl_Position = mtxCam * mtxMdl * vec4(vPosition, 1.0);
}