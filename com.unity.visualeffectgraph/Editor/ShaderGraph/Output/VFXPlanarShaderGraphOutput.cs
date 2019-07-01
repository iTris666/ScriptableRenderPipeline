using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEditor.VFX;
using UnityEngine.Rendering;
using System.Reflection;
using UnityEngine.VFX;
using UnityEditor.VFX;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.SG
{
    [VFXInfo]
    class VFXPlanarShaderGraphOutput : VFXShaderGraphOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VFXPrimitiveType primitiveType = VFXPrimitiveType.Quad;

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                if (primitiveType == VFXPrimitiveType.Octagon)
                    properties = properties.Concat(PropertiesFromType(typeof(VFXPlanarPrimitiveHelper.OctagonInputProperties)));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (primitiveType == VFXPrimitiveType.Octagon)
                yield return slotExpressions.First(o => o.name == "cropFactor");
        }

        public override string name { get { return "Shader Graph " + primitiveType.ToString() + " Output"; } }
        public override VFXTaskType taskType
        {
            get
            {
                return VFXPlanarPrimitiveHelper.GetTaskType(primitiveType);
            }
        }
    }
}
