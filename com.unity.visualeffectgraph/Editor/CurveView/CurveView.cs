using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX.UI;
using UnityEngine.UIElements;

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

            rootVisualElement.Add(m_CurveView);
        }
    }

    class CurveView : VisualElement, IControlledElement<CurveViewController>
    {
        class Curve : IUserCurve
        {
            public string name { get { return "curve"; } }

            public AnimationCurve curve { get { return m_Curve; } }

            public Color defaultColor { get { return m_Color; } }

            public void OnCurveChanged(AnimationCurve newValue)
            {
                CurveController.CopyCurve(newValue, m_Curve);
            }

            public Curve(AnimationCurve curve,Color defaultColor)
            {
                m_Curve = curve;
                m_Color = defaultColor;
            }

            AnimationCurve m_Curve;
            Color m_Color;
        }

        CurveViewController m_Controller;


        Vector2 m_Scale = Vector2.one;
        Vector2 m_Offset = Vector2.zero;

        public Vector2 scale
        {
            get{return m_Scale;}
        }

        public Vector2 offset
        {
            get{return m_Offset;}
        }

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
                CurveDisplay newCurve = new CurveDisplay(this);

                newCurve.controller = curve;
                m_Curves[curve] = newCurve;

                m_CurveContainer.Add(newCurve);
            }
        }

        Dictionary<CurveController, CurveDisplay> m_Curves= new Dictionary<CurveController, CurveDisplay>();

        public CurveView()
        {
            var uxml = Resources.Load<VisualTreeAsset>("uxml/CurveView");
            uxml.CloneTree(this);

            m_CurveContainer = this.Query("curve-container");
            var newController = new CurveViewController();

            newController.AddCurve(new Curve(AnimationCurve.EaseInOut(0, 0, 1, 1),Color.red));
            newController.AddCurve(new Curve(AnimationCurve.Linear(0, 0, 1, 1),Color.yellow));

            this.AddStyleSheetPathWithSkinVariant("CurveView",true);

            controller = newController;


            RegisterCallback<WheelEvent>(OnWheel);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        Vector2 m_StartDrag;
        Vector2 m_StartOffset;
        bool m_Dragging;

        void OnMouseDown(MouseDownEvent e)
        {
            if(e.clickCount == 1 && ((e.button == 0 && e.altKey && ! e.shiftKey && !e.actionKey ) ||(e.button == 2 && !e.altKey && !e.shiftKey && !e.actionKey)))
            {
                if( !m_Dragging) // just in case we lost a MouseUp event prevent the event from beeing registered twice
                    RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_Dragging = true;
                m_StartDrag = e.mousePosition;
                m_StartOffset = m_Offset;
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            m_Dragging = false;
            UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            m_Offset = m_StartOffset + e.mousePosition - m_StartDrag;


            if (m_Offset.x > 0)
                m_Offset.x = 0;
            foreach (var curve in m_Curves.Values)
            {
                curve.OffsetChanged();
            }
        }

        void OnWheel(WheelEvent e)
        {
            if( ! e.altKey  )
                m_Scale.x *= 1 + (e.delta.y) / 100;
            
            if( !e.actionKey)
                m_Scale.y *= 1 + (e.delta.y) / 100;

            foreach( var curve in m_Curves.Values)
            {
                curve.ScaleChanged();
            }
        }
        CurveBackground m_CurveBackground;
        VisualElement m_CurveContainer;
    }
}
