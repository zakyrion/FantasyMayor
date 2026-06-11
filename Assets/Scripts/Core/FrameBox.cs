using System;
using UnityEngine;

namespace Core
{
    // Zero-allocation, value-type container for state that is valid only for a bounded number of frames.
    // It is stamped with the frame it was captured on (UnityEngine.Time.frameCount); Exist stays true only
    // while within its frame budget, so a stale cross-frame read fails loud (throws) instead of silently
    // returning outdated data. Manual Dispose invalidates it early and drops the payload — use it on teardown
    // / reload so a held reference (e.g. a Camera) does not dangle past its owner's lifetime.
    public struct FrameBox<T>
    {
        private T _value;
        private int _capturedFrame;
        private int _budget;
        private bool _filled;

        public bool Exist => _filled && Time.frameCount - _capturedFrame < _budget;

        public T Value => Exist
            ? _value
            : throw new InvalidOperationException($"FrameBox<{typeof(T).Name}> is stale or empty.");

        public static FrameBox<T> OneFrame(in T value)
        {
            return ForFrames(value, 1);
        }

        public static FrameBox<T> TwoFrames(in T value)
        {
            return ForFrames(value, 2);
        }

        // Named ForFrames rather than FrameBox(int): a member cannot share its enclosing type's name (CS0542).
        public static FrameBox<T> ForFrames(in T value, int frameCount)
        {
            if (frameCount < 1)
                throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount,
                    "FrameBox budget must be at least one frame.");

            return new FrameBox<T>
            {
                _value = value,
                _capturedFrame = Time.frameCount,
                _budget = frameCount,
                _filled = true
            };
        }

        public void Dispose()
        {
            _filled = false;
            _value = default;
        }
    }
}
