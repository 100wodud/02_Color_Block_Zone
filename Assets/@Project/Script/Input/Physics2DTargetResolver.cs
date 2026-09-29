using System.Collections.Generic;
using UnityEngine;

namespace Areung_Plugin.Input
{
    public sealed class Physics2DTargetResolver : IPointerTargetResolver
    {
        private readonly string _tag;
        private readonly ContactFilter2D _filter;
        private readonly List<Collider2D> _results = new();

        public Physics2DTargetResolver(LayerMask mask, string requiredTag = "", bool includeTriggers = true)
        {
            _tag = string.IsNullOrEmpty(requiredTag) ? null : requiredTag;
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = includeTriggers,
                layerMask = mask
            };
        }

        public bool TryResolve(in PointerInput pointer, out Component target)
        {
            target = null;
            int count = Physics2D.OverlapPoint(pointer.World, _filter, _results);
            for (int i = 0; i < count; i++)
            {
                var col = _results[i];
                if (col == null) continue;
                if (_tag != null && !col.CompareTag(_tag)) continue;
                if (InputTarget.TryGet(col, out target)) return true;
            }

            return false;
        }
    }
}
