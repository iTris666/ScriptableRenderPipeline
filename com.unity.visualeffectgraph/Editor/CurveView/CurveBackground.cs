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
    class VFXCurveBackgroundFactory : UxmlFactory<CurveBackground>
    { }
    class CurveBackground : VisualElement
    {
        CurveView m_View;


        CurveView view
        {
            get
            {
                if (m_View == null)
                    m_View = GetFirstAncestorOfType<CurveView>();
                return m_View;
            }
        }

        Mesh m_LinesMesh;

        public CurveBackground()
        {
            m_LinesMesh = new Mesh();

            m_LinesMesh.vertices = new Vector3[] { new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0) };
            m_LinesMesh.colors32 = new Color32[] { Color.black, Color.black };
            m_LinesMesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
            
            if (s_Mat == null)
            {
                s_Mat = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
            }
        }

        Material s_Mat;
        protected override void DoRepaint(IStylePainter painter)
        {
            float height = contentRect.height;
            float width = contentRect.width;

            s_Mat.SetPass(0);

            Graphics.DrawMeshNow(m_LinesMesh, Matrix4x4.Scale(new Vector3(width, height, 0)));
        }
    }
}
