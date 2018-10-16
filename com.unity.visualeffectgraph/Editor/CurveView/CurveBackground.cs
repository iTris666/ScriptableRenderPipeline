using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.UI;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.CurveView
{
    class CurveBackground : VisualElement
    {
        CurveView m_View;
        public CurveBackground(CurveView view)
        {
            m_View = view;
        }
        protected override void DoRepaint(IStylePainter painter)
        {
        }
    }
}