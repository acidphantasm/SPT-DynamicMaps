using DynamicMaps.ExternalModSupport;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class PlayerMapMarker : MapMarker
    {
        private static readonly float MaxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static readonly Vector2 Pivot = new Vector2(0.5f, 0.5f);

        private IPlayer Player { get; set; }
        private bool UseViewpoint { get; set; }
        private bool _showingCamera;

        private float _callbackTime = MaxCallbackTime;  // make sure to start with a callback

        public static PlayerMapMarker Create(IPlayer player, GameObject parent, string imagePath, Color color, string category,
                                             Vector2 size, float degreesRotation, float scale, bool useViewpoint)
        {
            var name = player?.Profile?.GetCorrectedNickname() ?? "Camera";
            var position = useViewpoint ? FreecamSupport.GetViewpoint(player).Position : player.Position;
            
            var marker = Create<PlayerMapMarker>(parent, name, category, imagePath, color, MathUtils.ConvertToMapPosition(position), size, Pivot, degreesRotation, scale);
            marker.IsDynamic = true;
            marker.Player = player;
            marker.UseViewpoint = useViewpoint;

            return marker;
        }

        public PlayerMapMarker()
        {
            ImageAlphaLayerStatus[LayerStatus.Hidden] = 0.25f;
            ImageAlphaLayerStatus[LayerStatus.Underneath] = 0.25f;
            ImageAlphaLayerStatus[LayerStatus.OnTop] = 1f;
            ImageAlphaLayerStatus[LayerStatus.FullReveal] = 1f;

            LabelAlphaLayerStatus[LayerStatus.Hidden] = 0.0f;
            LabelAlphaLayerStatus[LayerStatus.Underneath] = 0.0f;
            LabelAlphaLayerStatus[LayerStatus.OnTop] = 0.0f;
            LabelAlphaLayerStatus[LayerStatus.FullReveal] = 1.00f;
        }

        private void LateUpdate()
        {
            Vector3 position;
            float rotation;
            
            if (UseViewpoint)
            {
                var player = GameUtils.GetMainPlayer();
                var alive = player != null && player.HealthController?.IsAlive == true;
                var cameraActive = FreecamSupport.TryGetCameraViewpoint(out var cameraViewpoint);
                SetGraphicVisibility(alive || cameraActive);
                if (_showingCamera != cameraActive)
                {
                    SetImagePath(cameraActive ? "Markers/camera.png" : "Markers/arrow.png");
                    _showingCamera = cameraActive;
                }
                if (!alive && !cameraActive)
                {
                    return;
                }
                var viewpoint = cameraActive ? cameraViewpoint : FreecamSupport.GetViewpoint(player);
                position = viewpoint.Position;
                rotation = viewpoint.Rotation;
            }
            else
            {
                if (Player?.Transform?.Original == null)
                {
                    return;
                }
                position = Player.Position;
                rotation = -Player.Rotation.x;
            }

            // throttle callback, since that leads to a layer search which might be expensive
            _callbackTime += Time.deltaTime;
            var callback = _callbackTime >= MaxCallbackTime;
            if (callback)
            {
                _callbackTime = 0f;
            }

            MoveAndRotate(MathUtils.ConvertToMapPosition(position), rotation, callback);
            
            if (_showingCamera && transform.GetSiblingIndex() != transform.parent.childCount - 1)
            {
                transform.SetAsLastSibling();
            }
        }
    }
}
