{
    "VertexCode": "#version 440 core\n\nlayout(location=0) in vec3 aPos;\n\nlayout(location=1) in vec2 aUV;\nout vec2 pass_uv;\nuniform mat4 projection;\nuniform mat4 view;\n\nvoid main(){\n    gl_Position = projection * view * vec4(aPos, 1.0);\n    pass_uv = aUV;\n}",
    "FragmentCode": "#version 440 core\n\nin vec2 pass_uv;\nout vec4 FragColour;\nuniform float time;\n\nvoid main(){\n   FragColour = vec4(pass_uv,sin(time) * 0.5 + 0.5,1.0);\n}"
}