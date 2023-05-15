﻿namespace NCneticCore.View
{
    internal class VSFS
    {
        internal static string vs = "#version 120\r\n\r\n\r\nvarying vec4 sColor;\r\n\r\nattribute vec3 vPosition;\r\nattribute vec3 vNormal;\r\nattribute vec3 vColor;\r\n\r\nuniform int vRenderMode;\r\nuniform vec3 vCustomColor;\r\n\r\nuniform mat4 vProjection;\r\nuniform mat4 vModel;\r\nuniform mat4 vView;\r\n\r\nconst float lightAway = 1000;\r\nconst float kd = 0.9;\r\n \r\nvoid main()\r\n{\r\n\tgl_Position = vProjection * vView * vModel * vec4(vPosition, 1.0);\r\n\tsColor = vec4(vColor[0], vColor[1], vColor[2], 1.0);\r\n\r\n\tif (vRenderMode == 1 || vRenderMode == 2)  // LIGHT , ARROW\r\n\t{\r\n\t\tvec3 eyepos = -lightAway * vView[3][3] * vec3(vView[0][2],vView[1][2],vView[2][2]);\r\n\t\tvec3 worldpos = mat3(vModel) * vPosition;\r\n\t\tvec3 V = normalize( eyepos - worldpos);\r\n\r\n\t\tvec3 worldnorm1 = normalize(mat3(vModel) * vNormal);\r\n\t\tvec3 worldnorm2 = normalize(mat3(vModel) * -vNormal);\r\n\r\n\t\tfloat diffuse1 = kd * max(0, dot(V,worldnorm1));\r\n\t\tfloat diffuse2 = kd * max(0, dot(V,worldnorm2));\r\n\r\n\t\tfloat light = 0;\r\n\r\n\t\tif (diffuse2 > diffuse1)\r\n\t\t{\r\n\t\t\tlight = diffuse2;\t\r\n\t\t}\r\n\t\telse\r\n\t\t{\r\n\t\t\tlight = diffuse1;\r\n\t\t}\r\n\r\n\t\tif (vRenderMode == 1)\r\n\t\t{\r\n\t\t\tsColor = vec4(light*vColor[0], light*vColor[1], light*vColor[2], sColor[3]);\r\n\t\t}\r\n\t\telse\r\n\t\t{\r\n\t\t\tsColor = vec4(light*vCustomColor[0], light*vCustomColor[1], light*vCustomColor[2], sColor[3]);\r\n\t\t}\r\n\t}\r\n\telse if (vRenderMode == 3) // INVISIBLE\r\n\t{\r\n\t\tgl_Position = vec4(0.0, 0.0, 0.0, 0.0);\r\n\t\tsColor[3] = 0.0;\r\n\t}\r\n\telse if (vRenderMode == 4) // SEL_OVER\r\n\t{\r\n\t\tsColor = vec4(min(1, 1.5*vColor[0]), min(1, 1.5*vColor[1]), min(1, 1.5*vColor[2]), sColor[3]);\r\n\t}\r\n\telse if (vRenderMode == 5) // SEL_PTS\r\n\t{\r\n\t\tsColor = vec4(vCustomColor[0], vCustomColor[1], vCustomColor[2], 1.0);\r\n\t}\r\n}";
        internal static string fs = "#version 120\r\n\r\n\r\nvarying vec4 sColor;\r\n\r\nuniform int sWidth;\r\nuniform int sHeight;\r\n\r\nuniform int sRenderMode;\r\n\r\nvoid main()\r\n{\r\n\tgl_FragColor = sColor;\r\n\t\r\n\tif (sRenderMode == 1) // TRANSPARENT\r\n\t{\r\n\t\tgl_FragColor[3] = 0.5;\r\n\t}\r\n\telse if (sRenderMode == 2) // AFTERSEL\r\n\t{\r\n\t\tgl_FragColor[3] = 0.25;\r\n\t}\r\n}";
    }
}
