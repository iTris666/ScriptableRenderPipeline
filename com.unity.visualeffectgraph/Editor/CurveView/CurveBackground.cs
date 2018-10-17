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

        Mesh m_HorizontalLinesMesh;

        public CurveBackground()
        {
            m_HorizontalLinesMesh = new Mesh();

            m_HorizontalLinesMesh.vertices = new Vector3[] { new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0) };
            m_HorizontalLinesMesh.colors32 = new Color32[] { Color.black, Color.black };
            m_HorizontalLinesMesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
            
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

            Graphics.DrawMeshNow(m_HorizontalLinesMesh, Matrix4x4.TRS(new Vector3(0,view.offset.y,0),Quaternion.identity,new Vector3(width, height, 0)));
        }
    }
}
