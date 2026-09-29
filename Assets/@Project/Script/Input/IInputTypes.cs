using UnityEngine;

namespace Areung_Plugin.Input
{
    public interface IDragTarget
    {
        void OnSelect(Vector3 worldPos);
        void OnMove(Vector3 worldPos);
        void OnDeselect(Vector3 worldPos);
    }

    public interface ITapTarget
    {
        void OnTap(Vector3 worldPos);
    }

    public readonly struct PointerInput
    {
        public readonly Vector2 Screen;
        public readonly Vector3 World;
        public readonly Ray Ray;

        public PointerInput(Vector2 screen, Vector3 world, Ray ray)
        {
            Screen = screen;
            World = world;
            Ray = ray;
        }
    }

    public interface IPointerTargetResolver
    {
        bool TryResolve(in PointerInput sample, out Component target);
    }

    public static class InputTarget
    {
        public static bool TryGet(Component hit, out Component target)
        {
            var drag = hit.GetComponentInParent<IDragTarget>();
            if (drag != null)
            {
                target = (Component)drag;
                return true;
            }

            var tap = hit.GetComponentInParent<ITapTarget>();
            if (tap != null)
            {
                target = (Component)tap;
                return true;
            }

            target = null;
            return false;
        }
    }
}
