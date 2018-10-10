using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.UI;
using System.Collections.ObjectModel;

namespace UnityEditor.VFX.CurveView
{
    interface ICurveHandle
    {
        void ApplyToCurve(AnimationCurve curve);
    }
    class CurveViewController : Controller
    {
        public ICurveHandle AddCurve(AnimationCurve curve, Action<ICurveHandle> onCurveChanged)
        {
            var controller = new CurveController(curve,onCurveChanged);
            m_CurveControllers.Add(controller);
            return controller;
        }

        public void RemoveCurve(ICurveHandle handle)
        {
            m_CurveControllers.Remove(handle as CurveController);
        }

        public override void ApplyChanges()
        {
            NotifyChange(AnyThing);

            foreach(var controller in m_CurveControllers)
            {
                controller.ApplyChanges();
            }
        }

        List<CurveController> m_CurveControllers = new List<CurveController>();

        public ReadOnlyCollection<CurveController> curveControllers
        {
            get { return m_CurveControllers.AsReadOnly(); }
        }
    }

    class CurveController : Controller, ICurveHandle
    {

        public AnimationCurve curve
        {
            get { return m_Curve; }
        }

        void CopyCurve(AnimationCurve source, AnimationCurve target)
        {
            target.keys = source.keys;
            target.postWrapMode = source.postWrapMode;
            target.preWrapMode = source.preWrapMode;
        }

        AnimationCurve m_Curve;

        Action<ICurveHandle> m_OnChanged;

        public CurveController(AnimationCurve curve, Action<ICurveHandle> onChanged)
        {
            m_Curve = new AnimationCurve();
            m_OnChanged = onChanged;

            CopyCurve(curve, m_Curve);
        }

        public void ApplyToCurve(AnimationCurve curve)
        {
            CopyCurve(m_Curve, curve);
        }

        public override void ApplyChanges()
        {
            NotifyChange(AnyThing);
        }
    }
}
