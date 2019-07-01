using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEditor.VFX;
using UnityEngine.Rendering;
using System.Reflection;
using UnityEngine.VFX;
using UnityEditor.VFX.SG;


namespace UnityEditor.VFX.SG
{ 
    [VFXInfo]
    class VFXMeshShaderGraphOutput : VFXShaderGraphOutput
    {

        public class InputProperties
        {
            [Tooltip("Mesh to be used for particle rendering.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
            [Tooltip("Define a bitmask to control which submeshes are rendered.")]
            public uint subMeshMask = 0xffffffff;
        }

        public override VFXTaskType taskType { get { return VFXTaskType.ParticleMeshOutput; } }

        public override string name { get { return "Shader Graph Mesh Output"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                    mapper.AddExpression(inputSlots.First(s => s.name == "mesh").GetExpression(), "mesh", -1);
                    mapper.AddExpression(inputSlots.First(s => s.name == "subMeshMask").GetExpression(), "subMeshMask", -1);
                    break;
            }

            return mapper;
        }
    }
}
