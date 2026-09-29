using UnityEngine;

namespace Areung_Plugin.Input
{
    public sealed class Physics3DTargetResolver : IPointerTargetResolver
    {
        private readonly string _tag;
        private readonly LayerMask _mask;
        private readonly float _maxDistance;
        private readonly RaycastHit[] _buffer;

        public Physics3DTargetResolver(LayerMask mask, string requiredTag = "",
            float maxDistance = Mathf.Infinity, int bufferSize = 16)
        {
            _mask = mask;
            _tag = string.IsNullOrEmpty(requiredTag) ? null : requiredTag;
            _maxDistance = maxDistance;
            _buffer = new RaycastHit[Mathf.Max(1, bufferSize)];
        }

        public bool TryResolve(in PointerInput pointer, out Component target)
        {
            target = null;
            int count = Physics.RaycastNonAlloc(pointer.Ray, _buffer, _maxDistance, _mask);

            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = _buffer[i];
                if (hit.collider == null) continue;
                if (hit.distance >= best) continue;
                if (_tag != null && !hit.collider.CompareTag(_tag)) continue;
                if (InputTarget.TryGet(hit.collider, out var t))
                {
                    best = hit.distance;
                    target = t;
                }
            }

            return target != null;
        }
    }
}
