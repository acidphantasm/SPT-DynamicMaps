using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    internal sealed class CircleMapOutline : MaskableGraphic
    {
        private float _thickness = 3f;

        internal float Thickness
        {
            get => _thickness;
            set
            {
                if (Mathf.Approximately(_thickness, value))
                {
                    return;
                }
                _thickness = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();

            var rect = rectTransform.rect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            const int segments = 128;
            var innerRadius = radius - _thickness * 0.75f;
            var outerRadius = innerRadius + _thickness;
            var radii = new[] { innerRadius - 0.5f, innerRadius, outerRadius, outerRadius + 0.5f };
            var opaque = (Color32)color;
            var transparent = opaque;
            transparent.a = 0;

            for (var ring = 0; ring < radii.Length; ring++)
            {
                var vertexColor = ring == 0 || ring == radii.Length - 1 ? transparent : opaque;
                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = segment * Mathf.PI * 2f / segments;
                    vertices.AddVert(new Vector3(rect.center.x + Mathf.Cos(angle) * radii[ring], rect.center.y + Mathf.Sin(angle) * radii[ring]), vertexColor, Vector2.zero);
                }
            }

            for (var ring = 0; ring < radii.Length - 1; ring++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var next = (segment + 1) % segments;
                    var inner = ring * segments + segment;
                    var innerNext = ring * segments + next;
                    var outer = (ring + 1) * segments + segment;
                    var outerNext = (ring + 1) * segments + next;
                    
                    vertices.AddTriangle(inner, outer, innerNext);
                    vertices.AddTriangle(innerNext, outer, outerNext);
                }
            }
        }
    }
}
