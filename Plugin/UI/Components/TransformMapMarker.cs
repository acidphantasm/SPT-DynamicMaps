using System;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class TransformMapMarker : MapMarker
    {
        private static readonly float MaxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static readonly Vector2 Pivot = new(0.5f, 0.5f);

        private Transform FollowingTransform { get; set; }
        public MathUtils.RotationAxis RotationAxis { get; set; } = MathUtils.RotationAxis.Y;

        private float _callbackTime = MaxCallbackTime;  // make sure to start with a callback
        private bool _warnedAttachedIsDisabled;
        
        private Rigidbody _settlingBody;
        private float _stableTime;
        private float _settlingTime;
        private Vector3 _lastSettlingPosition;
        private float _lastSettlingRotation;
        private Action<TransformMapMarker> _onSettled;

        public static TransformMapMarker Create(Transform followingTransform, GameObject parent, string imagePath, Color color, string name, string category, Vector2 size, float degreesRotation, float scale, Sprite sprite = null)
        {
            var marker = Create<TransformMapMarker>(parent, name, category, imagePath, color, MathUtils.ConvertToMapPosition(followingTransform), size, Pivot, degreesRotation, scale, true, sprite);
            marker.IsDynamic = true;
            marker.FollowingTransform = followingTransform;

            return marker;
        }

        public TransformMapMarker()
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

        public void Settle(Rigidbody body, Action<TransformMapMarker> onSettled)
        {
            _settlingBody = body;
            _onSettled = onSettled;
            _lastSettlingPosition = FollowingTransform.position;
            _lastSettlingRotation = FollowingTransform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (FollowingTransform == null)
            {
                return;
            }

            if (!FollowingTransform.gameObject.activeInHierarchy)
            {
                if (!_warnedAttachedIsDisabled)
                {
                    Plugin.Log.LogWarning($"FollowingTransform for TransformMapMarker has been disabled without removing the map marker");
                    Color = Color.red;
                    _warnedAttachedIsDisabled = true;
                }
                return;
            }
            else
            {
                _warnedAttachedIsDisabled = false;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(FollowingTransform);
            var mapRotation = MathUtils.ConvertToMapRotation(FollowingTransform, RotationAxis);

            if (_onSettled != null)
            {
                if (_settlingBody == null)
                {
                    _settlingBody = FollowingTransform.GetComponent<Rigidbody>();
                }

                _settlingTime += Time.deltaTime;
                if ((FollowingTransform.position - _lastSettlingPosition).sqrMagnitude < 0.0004f && Mathf.Abs(Mathf.DeltaAngle(FollowingTransform.eulerAngles.y, _lastSettlingRotation)) < 1f)
                {
                    _stableTime += Time.deltaTime;
                }
                else
                {
                    _stableTime = 0f;
                }

                _lastSettlingPosition = FollowingTransform.position;
                _lastSettlingRotation = FollowingTransform.eulerAngles.y;
                
                if (_settlingTime >= 0.5f && _stableTime >= 0.75f && (_settlingBody == null || _settlingBody.IsSleeping() || _stableTime >= 1.5f))
                {
                    MoveAndRotate(mapPosition, mapRotation);
                    
                    var onSettled = _onSettled;
                    _onSettled = null;
                    onSettled(this);
                    return;
                }
            }

            // check if in exactly the same place and skip updating if it is
            if (MathUtils.ApproxEquals(Position.x, mapPosition.x)
             && MathUtils.ApproxEquals(Position.y, mapPosition.y)
             && MathUtils.ApproxEquals(Position.z, mapPosition.z)
             && MathUtils.ApproxEquals(Rotation, mapRotation))
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

            MoveAndRotate(mapPosition, mapRotation, callback);
        }
    }
}
