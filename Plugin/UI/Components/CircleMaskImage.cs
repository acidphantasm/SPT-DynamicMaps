using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    internal sealed class CircleMaskImage : Image
    {
        private bool _circle;

        internal bool Circle
        {
            get => _circle;
            set
            {
                if (_circle == value)
                {
                    return;
                }
                _circle = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            if (!_circle)
            {
                base.OnPopulateMesh(vertices);
                return;
            }

            vertices.Clear();
            var rect = rectTransform.rect;
            
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            const int segments = 128;
            vertices.AddVert(rect.center, color, Vector2.zero);
            
            for (var segment = 0; segment < segments; segment++)
            {
                var angle = segment * Mathf.PI * 2f / segments;
                vertices.AddVert(new Vector3(rect.center.x + Mathf.Cos(angle) * radius, rect.center.y + Mathf.Sin(angle) * radius), color, Vector2.zero);
            }
            
            for (var segment = 0; segment < segments; segment++)
            {
                vertices.AddTriangle(0, segment + 1, (segment + 1) % segments + 1);
            }
        }
    }
}
