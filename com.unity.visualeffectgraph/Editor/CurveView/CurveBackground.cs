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

        Mesh m_HoriAxis;

        public CurveBackground()
        {
            CreateHoriAxis();
        }

        void CreateHoriAxis()
        {
            if(m_HoriAxis != null)
                return;
            m_HoriAxis = new Mesh();
            m_HoriAxis.vertices = new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0) };
            m_HoriAxis.colors32 = new Color32[] { Color.black, Color.black, Color.black, Color.black };
            m_HoriAxis.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
        }

        Mesh m_HorizontalLines;

        const int minSpace = 30;
        const int maxSpace = 100;
        float m_HorizontalMargin;

        void UpdateHorizontalLines()
        {
            if( m_HorizontalLines == null)
            {
                m_HorizontalLines = new Mesh();
            }
            float height = contentRect.height;
            int currentCount = m_HorizontalLines.vertexCount / 4;

            float units = 1;

            while(units * view.scale.y * height > maxSpace)
            {
                units /= 5;
            }
            while (units * view.scale.y * height < minSpace)
            {
                units *= 5;
            }

            m_HorizontalMargin = units * view.scale.y * height;

            int neededCount = (int)(height / m_HorizontalMargin) + 1;

            if (neededCount != currentCount)
            {

                m_HorizontalLines.SetIndices(null, MeshTopology.Lines, 0);
                var vertices = new Vector3[neededCount * 2];
                var colors = new Color32[neededCount * 2];

                for(int i = 0; i< neededCount; ++i)
                {
                    float y = i;
                    vertices[i * 2] = new Vector3(0,y,0);
                    vertices[i * 2 + 1] = new Vector3(1, y, 0);
                    colors[i * 2] = colors[i * 2 + 1] = new Color32(0, 0, 0, 128);
                }

                m_HorizontalLines.vertices = vertices;
                m_HorizontalLines.colors32 = colors;
                m_HorizontalLines.SetIndices(Enumerable.Range(0, neededCount * 2).ToArray(), MeshTopology.Lines, 0);
            }
        }

        Material s_Mat;
        protected override void DoRepaint(IStylePainter painter)
        {
            float height = contentRect.height;
            float width = contentRect.width;

            if (s_Mat == null)
            {
                s_Mat = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
            }

            s_Mat.SetPass(0);
            CreateHoriAxis();
            Graphics.DrawMeshNow(m_HoriAxis, Matrix4x4.TRS(new Vector3(0, view.offset.y + height * 0.5f, 0), Quaternion.identity, new Vector3(width, 3, 0)));

            UpdateHorizontalLines();
            Graphics.DrawMeshNow(m_HorizontalLines, Matrix4x4.TRS(new Vector3(0, Mathf.Repeat(view.offset.y + height * 0.5f, m_HorizontalMargin) , 0), Quaternion.identity, new Vector3(width, m_HorizontalMargin, 1)));
        }
    }
}
