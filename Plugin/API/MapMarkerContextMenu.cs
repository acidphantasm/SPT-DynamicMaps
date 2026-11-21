using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicMaps.API
{
    using MapMarker = UI.Components.MapMarker;

    public sealed class MapMarkerContextAction
    {
        public string Id { get; }
        public int Order { get; }

        private readonly Func<MapMarker, string> _getLabel;
        private readonly Func<MapMarker, bool> _isAvailable;
        private readonly Action<MapMarker> _execute;

        public MapMarkerContextAction(string id, Func<MapMarker, string> getLabel, Action<MapMarker> execute,
                                      Func<MapMarker, bool> isAvailable = null, int order = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Action id cannot be empty", nameof(id));

            Id = id;
            Order = order;
            _getLabel = getLabel ?? throw new ArgumentNullException(nameof(getLabel));
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _isAvailable = isAvailable;
        }

        public MapMarkerContextAction(string id, string label, Action<MapMarker> execute,
                                      Func<MapMarker, bool> isAvailable = null, int order = 0)
            : this(id, _ => label, execute, isAvailable, order)
        {
        }

        internal string GetLabel(MapMarker marker) => _getLabel(marker);
        internal bool IsAvailable(MapMarker marker) => _isAvailable == null || _isAvailable(marker);
        internal void Execute(MapMarker marker) => _execute(marker);
    }

    /// <summary>
    /// Lets other mods add entries to the right-click menu of map markers
    /// </summary>
    public static class MapMarkerContextMenu
    {
        private static readonly Dictionary<string, MapMarkerContextAction> Actions = new(StringComparer.Ordinal);

        /// <summary>
        /// Registering an action with an existing id replaces it
        /// </summary>
        public static void RegisterAction(MapMarkerContextAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Actions[action.Id] = action;
        }

        public static bool UnregisterAction(string id)
        {
            return id != null && Actions.Remove(id);
        }

        public static bool IsRegistered(string id)
        {
            return id != null && Actions.ContainsKey(id);
        }

        internal static List<MapMarkerContextAction> GetAvailableActions(MapMarker marker)
        {
            var available = new List<MapMarkerContextAction>();
            if (marker == null)
                return available;

            foreach (var action in Actions.Values.OrderBy(a => a.Order).ThenBy(a => a.Id, StringComparer.Ordinal))
            {
                try
                {
                    if (action.IsAvailable(marker))
                        available.Add(action);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Context menu action '{action.Id}' threw in IsAvailable: {e}");
                }
            }

            return available;
        }
    }
}
