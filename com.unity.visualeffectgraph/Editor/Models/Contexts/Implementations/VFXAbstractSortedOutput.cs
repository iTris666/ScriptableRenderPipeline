using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractSortedOutput : VFXContext, IVFXSubRenderer
    {
        public enum SortMode
        {
            Auto,
            Off,
            On
        }

        [VFXSetting, SerializeField]
        protected bool useSoftParticle = false; 

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, Header("Rendering Options")]
        protected int sortPriority = 0;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool castShadows = false;

        // IVFXSubRenderer interface
        public virtual bool hasShadowCasting { get { return castShadows; } }

        public virtual bool HasSorting()        { return false; }

        public virtual bool HasIndirectDraw() { return false; }

        public virtual bool NeedsDeadListCount() { return false; }
        int IVFXSubRenderer.sortPriority
        {
            get {
                return sortPriority;
            }
            set {
                if(sortPriority != value)
                {
                    sortPriority = value;
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }

        protected VFXAbstractSortedOutput(VFXDataType dataType) : base(VFXContextType.Output, dataType, VFXDataType.None) {}

        public override bool codeGeneratorCompute { get { return false; } }

        public virtual bool supportSoftParticles { get { return useSoftParticle && !isBlendModeOpaque; } }

        public virtual bool isBlendModeOpaque { get { return false; } }

        protected virtual IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            if (supportSoftParticles)
            {
                var softParticleFade = slotExpressions.First(o => o.name == "softParticlesFadeDistance");
                var invSoftParticleFade = (VFXValue.Constant(1.0f) / softParticleFade.exp);
                yield return new VFXNamedExpression(invSoftParticleFade, "invSoftParticlesFadeDistance");
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.GPU)
            {
                var gpuMapper = VFXExpressionMapper.FromBlocks(activeChildrenWithImplicit);
                gpuMapper.AddExpressions(CollectGPUExpressions(GetExpressionsFromSlots(this)), -1);
                return gpuMapper;
            }
            return new VFXExpressionMapper();
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in PropertiesFromType(GetInputPropertiesTypeName()))
                    yield return property;

                if (supportSoftParticles)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "softParticlesFadeDistance", VFXPropertyAttribute.Create(new MinAttribute(0.001f))), 1.0f);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (isBlendModeOpaque)
                    yield return "IS_OPAQUE_PARTICLE";
                else
                    yield return "IS_TRANSPARENT_PARTICLE";

                VisualEffectResource asset = GetResource();
                if (asset != null)
                {
                    var settings = asset.rendererSettings;
                    if (settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object)
                        yield return "USE_MOTION_VECTORS_PASS";
                    if (hasShadowCasting)
                        yield return "USE_CAST_SHADOWS_PASS";
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (isBlendModeOpaque)
                {
                    yield return "useSoftParticle";
                }
            }
        }

        public override IEnumerable<VFXMapping> additionalMappings
        {
            get
            {
                yield return new VFXMapping("sortPriority", sortPriority);
            }
        }
    }
}
