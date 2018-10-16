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

    interface IUserCurve
    {
        string name { get; }

        Color defaultColor { get; }

        AnimationCurve curve { get; }

        void OnCurveChanged(AnimationCurve newValue);
    }

    class CurveViewController : Controller
    {
        public void AddCurve(IUserCurve curve)
        {
            var controller = new CurveController(curve);
            m_CurveControllers.Add(controller);
        }

        public void RemoveCurve(IUserCurve handle)
        {
            m_CurveControllers.RemoveAt(m_CurveControllers.FindIndex(t =>t.userCurve == handle));
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
        public class Change
        {
            public const int Color = 1;
        }

        public string name
        {
            get { return m_UserCurve.name; }
        }

        public AnimationCurve curve
        {
            get { return m_UserCurve.curve; }
        }

        public static void CopyCurve(AnimationCurve source, AnimationCurve target)
        {
            target.keys = source.keys;
            target.postWrapMode = source.postWrapMode;
            target.preWrapMode = source.preWrapMode;
        }

        IUserCurve m_UserCurve;
        AnimationCurve m_Cache;

        Action<ICurveHandle> m_OnChanged;

        public IUserCurve userCurve { get { return m_UserCurve; } }

        public CurveController(IUserCurve curve)
        {
            m_Cache = new AnimationCurve();
            m_UserCurve = curve;
            m_Color = curve.defaultColor;

            CopyCurve(curve.curve, m_Cache);
        }

        Color m_Color;

        public Color color
        {
            get{return m_Color;}
            set
            {
                m_Color = value;
                NotifyChange(Change.Color);
            }
        }


        public void ApplyToCurve(AnimationCurve curve)
        {
            CopyCurve(m_Cache, curve);
        }

        public override void ApplyChanges()
        {
            NotifyChange(AnyThing);
            m_UserCurve.OnCurveChanged(m_Cache);
        }
    }
}
