using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using UnityEngine;

namespace DynamicMaps.ExternalModSupport
{
    internal static class FreecamSupport
    {
        private static GameWorld _world;
        private static Camera _camera;
        
        private static MonoBehaviour _fikaFreecam;
        private static MonoBehaviour _sptFreecam;
        private static MonoBehaviour _freecamController;
        
        private static PropertyInfo _fikaActiveProperty;
        private static PropertyInfo _freecamActiveProperty;
        private static float _nextSearch;

        internal static MapViewpoint GetViewpoint(IPlayer player)
        {
            TryGetViewpoint(player, out var viewpoint);
            return viewpoint;
        }

        internal static bool TryGetViewpoint(IPlayer player, out MapViewpoint viewpoint)
        {
            if (TryGetCameraViewpoint(out viewpoint))
            {
                return true;
            }

            if (player?.Transform?.Original == null)
            {
                return false;
            }

            viewpoint = new MapViewpoint(player.Position, -player.Rotation.x);
            return true;
        }

        internal static bool TryGetCameraViewpoint(out MapViewpoint viewpoint)
        {
            viewpoint = default;
            if (!ModDetection.FikaLoaded && !ModDetection.FreecamLoaded || !CameraManager.Exist)
            {
                return false;
            }

            var camera = CameraManager.Instance.Camera;
            if (camera == null || !IsFreecamActive(camera))
            {
                return false;
            }

            viewpoint = new MapViewpoint(camera.transform.position, -camera.transform.eulerAngles.y, true);
            return true;
        }

        internal static void Clear()
        {
            _world = null;
            _camera = null;
            _fikaFreecam = null;
            _sptFreecam = null;
            _freecamController = null;
            _fikaActiveProperty = null;
            _freecamActiveProperty = null;
            _nextSearch = 0f;
        }

        private static bool IsFreecamActive(Camera camera)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null)
            {
                return false;
            }

            if (_world != world || _camera != camera)
            {
                Clear();
                _world = world;
                _camera = camera;
            }

            if (Time.unscaledTime >= _nextSearch && (ModDetection.FikaLoaded && _fikaFreecam == null || ModDetection.FreecamLoaded && (_sptFreecam == null || _freecamController == null)))
            {
                if (ModDetection.FikaLoaded && _fikaFreecam == null || ModDetection.FreecamLoaded && _sptFreecam == null)
                {
                    foreach (var component in camera.GetComponents<MonoBehaviour>())
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        if (ModDetection.FikaLoaded && component.GetType().FullName == "Fika.Core.Main.FreeCamera.FreeCamera")
                        {
                            _fikaFreecam = component;
                            _fikaActiveProperty = component.GetType().GetProperty("IsActive");
                        }
                        else if (ModDetection.FreecamLoaded && component.GetType().FullName == "Terkoiz.Freecam.Freecam")
                        {
                            _sptFreecam = component;
                        }
                    }
                }

                if (ModDetection.FreecamLoaded && _freecamController == null)
                {
                    foreach (var component in world.GetComponents<MonoBehaviour>())
                    {
                        if (component != null && component.GetType().FullName == "Terkoiz.Freecam.FreecamController")
                        {
                            _freecamController = component;
                            _freecamActiveProperty = component.GetType().GetProperty("IsFreecamActive");
                            break;
                        }
                    }
                }

                _nextSearch = Time.unscaledTime + 1f;
            }

            if (_fikaFreecam != null && _fikaActiveProperty?.GetValue(_fikaFreecam) is true)
            {
                return true;
            }

            return _sptFreecam != null && _freecamController != null && _freecamActiveProperty?.GetValue(_freecamController) is true;
        }
    }

    internal readonly struct MapViewpoint
    {
        private const float FreecamLayerHeightOffset = 1.5f;
        internal readonly Vector3 Position;
        internal readonly float Rotation;
        private readonly bool _isCamera;

        internal Vector3 LayerPosition
        {
            get => _isCamera ? Position - Vector3.up * FreecamLayerHeightOffset : Position;
        }

        internal MapViewpoint(Vector3 position, float rotation, bool isCamera = false)
        {
            Position = position;
            Rotation = rotation;
            _isCamera = isCamera;
        }
    }
}
