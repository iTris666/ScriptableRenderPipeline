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
    class CurveViewWindow : EditorWindow
    {

        [MenuItem("Window/Rendering/Curve View")]
        public static void CreateCurveView()
        {
            EditorWindow.GetWindow<CurveViewWindow>();
        }
        CurveView m_CurveView;

        public void OnEnable()
        {
            m_CurveView = new CurveView();
            m_CurveView.StretchToParentSize();

            this.GetRootVisualContainer().Add(m_CurveView);
        }
    }


    class CurveView : VisualElement, IControlledElement<CurveViewController>
    {
        class Curve : IUserCurve
        {
            public string name { get { return "curve"; } }

            public AnimationCurve curve { get { return m_Curve; } }

            public Color defaultColor { get { return Color.red; } }

            public void OnCurveChanged(AnimationCurve newValue)
            {
                CurveController.CopyCurve(newValue, m_Curve);
            }

            public Curve(AnimationCurve curve)
            {
                m_Curve = curve;
            }

            AnimationCurve m_Curve;
        }

        CurveViewController m_Controller;

        public CurveViewController controller
        {
            get { return m_Controller; }
            set {
                if( m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        Controller IControlledElement.controller { get { return m_Controller; } }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            foreach (var curve in m_Curves.Keys.Except(controller.curveControllers))
            {
                CurveDisplay removedCurve = m_Curves[curve];

                removedCurve.RemoveFromHierarchy();
            }

            foreach (var curve in controller.curveControllers.Except(m_Curves.Keys))
            {
                CurveDisplay newCurve = new CurveDisplay();

                newCurve.controller = curve;
                m_Curves[curve] = newCurve;

                m_CurveContainer.Add(newCurve);
            }
        }

        Dictionary<CurveController, CurveDisplay> m_Curves= new Dictionary<CurveController, CurveDisplay>();

        public CurveView()
        {
            var uxml = Resources.Load<VisualTreeAsset>("uxml/CurveView");
            uxml.CloneTree(this, null);

            m_CurveContainer = this.Query("curve-container");
            var newController = new CurveViewController();

            newController.AddCurve(new Curve(AnimationCurve.EaseInOut(0, 0, 1, 1)));
            newController.AddCurve(new Curve(AnimationCurve.Linear(0, 0, 1, 1)));

            this.AddStyleSheetPathWithSkinVariant("CurveView",true);

            controller = newController;
        }

        VisualElement m_CurveContainer;
    }

    class CurveDisplay : VisualElement, IControlledElement<CurveController>
    {

        public CurveDisplay()
        {
            m_Content = this;
            m_CurrentCurveResolution = 0;

            curveColor = Color.green;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        CurveController m_Controller;

        public CurveController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        static Material s_Mat;


        public Color curveColor { get; set; }

        protected override void DoRepaint(IStylePainter painter)
        {
            FillCurveData(16,false);

            if (s_Mat == null)
            {
                s_Mat = new Material(EditorGUIUtility.LoadRequired("Shaders/UIElements/AACurveField.shader") as Shader);

                s_Mat.hideFlags = HideFlags.HideAndDontSave;
            }

            float scale = worldTransform.MultiplyVector(Vector3.one).x;

            float realWidth = m_EdgeWidth;
            if (realWidth * scale < k_MinEdgeWidth)
            {
                realWidth = k_MinEdgeWidth / scale;
            }

            // Send the view zoom factor so that the antialias width do not grow when zooming in.
            s_Mat.SetFloat("_ZoomFactor", scale * realWidth / m_EdgeWidth * EditorGUIUtility.pixelsPerPoint);

            // Send the view zoom correction so that the vertex shader can scale the edge triangles when below m_MinWidth.
            s_Mat.SetFloat("_ZoomCorrection", realWidth / m_EdgeWidth);

            s_Mat.SetColor("_Color", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? curveColor.gamma : curveColor);

            s_Mat.SetPass(0);

            Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
        }

        Controller IControlledElement.controller { get { return m_Controller; } }

        Mesh m_Mesh;
        int m_CurrentCurveResolution;

        VisualElement m_Content;


        float[] m_ValueCache;
        float[] m_TimeCache;
        float m_MinValue;
        float m_MaxValue;

        const float m_EdgeWidth = 2;
        const float k_MinEdgeWidth = 1.75f;

        float halfwidth
        {
            get { return m_EdgeWidth * 0.5f; }
        }
        float vertexHalfWidth
        {
            get { return halfwidth + 1.0f; }
        }

        void FillCurveData(int askedResolution, bool force)
        {
            AnimationCurve curve = controller.curve;

            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                m_Mesh.hideFlags = HideFlags.HideAndDontSave;
            }

            if (curve.keys.Length < 2)
                return;

            float startTime = curve.keys[0].time;
            float endTime = curve.keys[curve.keys.Length - 1].time;
            float duration = endTime - startTime;

            Vector3[] vertices = m_Mesh.vertices;
            Vector3[] normals = m_Mesh.normals;
            int[] indices = null;
            if (askedResolution != m_CurrentCurveResolution || force)
            {
                m_CurrentCurveResolution = askedResolution;
                if (vertices == null || vertices.Length != m_CurrentCurveResolution * 2)
                {
                    vertices = new Vector3[m_CurrentCurveResolution * 2];
                    normals = new Vector3[m_CurrentCurveResolution * 2];
                    m_ValueCache = new float[m_CurrentCurveResolution];
                    m_TimeCache = new float[m_CurrentCurveResolution];
                }


                m_MinValue = Mathf.Infinity;
                m_MaxValue = -Mathf.Infinity;

                int keyCount = curve.keys.Length;
                int noKeySampleCount = m_CurrentCurveResolution - keyCount;

                m_TimeCache[0] = curve.keys[0].time;

                int usedSamples = 1;
                for (int k = 1; k < keyCount; ++k)
                {
                    float sliceStartTime = m_TimeCache[usedSamples - 1];
                    float sliceEndTime = curve.keys[k].time;
                    float sliceDuration = sliceEndTime - sliceStartTime;
                    int sliceSampleCount = Mathf.FloorToInt((float)noKeySampleCount * sliceDuration / duration);
                    if (k == keyCount - 1)
                    {
                        sliceSampleCount = m_CurrentCurveResolution - usedSamples - 1;
                    }

                    for (int i = 1; i < sliceSampleCount + 1; ++i)
                    {
                        float time = sliceStartTime + i * sliceDuration / (sliceSampleCount + 1);
                        m_TimeCache[usedSamples + i - 1] = time;
                    }

                    m_TimeCache[usedSamples + sliceSampleCount] = curve.keys[k].time;
                    usedSamples += sliceSampleCount + 1;
                }

                for (int i = 0; i < m_CurrentCurveResolution; ++i)
                {
                    float ct = m_TimeCache[i];

                    float currentValue = curve.Evaluate(ct);

                    if (currentValue > m_MaxValue)
                    {
                        m_MaxValue = currentValue;
                    }
                    if (currentValue < m_MinValue)
                    {
                        m_MinValue = currentValue;
                    }

                    m_ValueCache[i] = currentValue;
                }

                //fill triangle indices as it is a triangle strip
                indices = m_Mesh.triangles;
                if( indices == null || indices.Length != (m_CurrentCurveResolution * 2 - 2) * 3)
                {
                    indices = new int[(m_CurrentCurveResolution * 2 - 2) * 3];
                }
                

                for (int i = 0; i < m_CurrentCurveResolution * 2 - 2; ++i)
                {
                    if ((i % 2) == 0)
                    {
                        indices[i * 3] = i;
                        indices[i * 3 + 1] = i + 1;
                        indices[i * 3 + 2] = i + 2;
                    }
                    else
                    {
                        indices[i * 3] = i + 1;
                        indices[i * 3 + 1] = i;
                        indices[i * 3 + 2] = i + 2;
                    }
                }


            }

            float width = m_Content.layout.width;
            if ( 1 > width || float.IsNaN(width))
            {
                width = 1;
            }
            float height = m_Content.layout.height;
            if( 1 > height || float.IsNaN(height))
            {
                height = 1;
            }
            if (vertices == null || vertices.Length != m_CurrentCurveResolution * 2)
            {
                vertices = new Vector3[m_CurrentCurveResolution * 2];
                normals = new Vector3[m_CurrentCurveResolution * 2];
            }

            Vector3 scale = new Vector3(width,height);

            vertices[0] = vertices[1] = Vector3.Scale(new Vector3(0, 1 - Mathf.InverseLerp(m_MinValue, m_MaxValue, m_ValueCache[0]), 0), scale);

            Vector3 secondPoint = Vector3.Scale(new Vector3(1.0f / m_CurrentCurveResolution, 1 - Mathf.InverseLerp(m_MinValue, m_MaxValue, m_ValueCache[1]), 0), scale);
            Vector3 prevDir = (secondPoint - vertices[0]).normalized;

            Vector3 norm = new Vector3(prevDir.y, -prevDir.x, 1);

            normals[0] = -norm * vertexHalfWidth;
            normals[1] = norm * vertexHalfWidth;

            Vector3 currentPoint = secondPoint;

            for (int i = 1; i < m_CurrentCurveResolution - 1; ++i)
            {
                vertices[i * 2] = vertices[i * 2 + 1] = currentPoint;

                Vector3 nextPoint = Vector3.Scale(new Vector3(Mathf.InverseLerp(startTime, endTime, m_TimeCache[i + 1]), 1 - Mathf.InverseLerp(m_MinValue, m_MaxValue, m_ValueCache[i + 1]), 0), scale);

                Vector3 nextDir = (nextPoint - currentPoint).normalized;
                Vector3 dir = (prevDir + nextDir).normalized;
                norm = new Vector3(dir.y, -dir.x, 1);
                normals[i * 2] = -norm * vertexHalfWidth;
                normals[i * 2 + 1] = norm * vertexHalfWidth;

                currentPoint = nextPoint;
                prevDir = nextDir;
            }

            vertices[(m_CurrentCurveResolution - 1) * 2] = vertices[(m_CurrentCurveResolution - 1) * 2 + 1] = currentPoint;

            norm = new Vector3(prevDir.y, -prevDir.x, 1);
            normals[(m_CurrentCurveResolution - 1) * 2] = -norm * vertexHalfWidth;
            normals[(m_CurrentCurveResolution - 1) * 2 + 1] = norm * vertexHalfWidth;

            m_Mesh.vertices = vertices;
            m_Mesh.normals = normals;
            if( indices != null)
                m_Mesh.triangles = indices;
        }

        List<VisualElement> m_Keys = new List<VisualElement>();


        void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateKeys();
        }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (!float.IsNaN(layout.width))
                FillCurveData(16, true);
            var keys = controller.curve.keys;
            int keyCount = keys.Length;

            while (m_Keys.Count < keyCount)
            {
                var newKey = new VisualElement() { name = "key" };
                m_Keys.Add(newKey);
                Add(newKey);
            }
            while (m_Keys.Count > keyCount)
            {
                m_Keys[m_Keys.Count - 1].RemoveFromHierarchy();
                m_Keys.RemoveAt(m_Keys.Count - 1);
            }

            UpdateKeys();

        }

        void UpdateKeys()
        {
            var keys = controller.curve.keys;
            int keyCount = keys.Length;

            Rect bounds = contentRect;

            float timeStart = keys[0].time;
            float timeEnd = keys[keys.Length -1].time;
            float length = timeEnd - timeStart;

            float maxValue = keys.Select(t => t.value).Max();
            float minValue = keys.Select(t => t.value).Min();
            float range = maxValue - minValue;

            for (int i = 0; i < controller.curve.keys.Length; ++i)
            {
                float timeNormalized = (controller.curve.keys[i].time - timeStart) / length;
                m_Keys[i].style.positionLeft = timeNormalized * bounds.width - m_Keys[i].style.width * 0.5f;
                float valueNormalized =  (controller.curve.keys[i].value - minValue) / range;
                m_Keys[i].style.positionTop = bounds.height - valueNormalized * bounds.height - m_Keys[i].style.height * 0.5f;
            }
        }
    }
}
