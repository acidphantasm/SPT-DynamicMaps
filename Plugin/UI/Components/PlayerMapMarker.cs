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

        private float _callbackTime = MaxCallbackTime;  // make sure to start with a callback

        public static PlayerMapMarker Create(IPlayer player, GameObject parent, string imagePath, Color color, string category,
                                             Vector2 size, float degreesRotation, float scale)
        {
            var name = $"{player.Profile.GetCorrectedNickname()}";
            var marker = Create<PlayerMapMarker>(parent, name, category, imagePath, color,
                                                 MathUtils.ConvertToMapPosition(player.Position),
                                                 size, Pivot, degreesRotation, scale);
            marker.IsDynamic = true;
            marker.Player = player;

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
            if (Player?.Transform?.Original == null)
            {
                return;
            }

            // throttle callback, since that leads to a layer search which might be expensive
            _callbackTime += Time.deltaTime;
            var callback = _callbackTime >= MaxCallbackTime;
            if (callback)
            {
                _callbackTime = 0f;
            }

            MoveAndRotate(MathUtils.ConvertToMapPosition(Player.Position), -Player.Rotation.x, callback);
        }
    }
}
