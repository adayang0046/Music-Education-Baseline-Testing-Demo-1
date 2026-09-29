using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace XRMidi
{
    public sealed class QuestHandGestureProvider : MonoBehaviour, IGestureProvider
    {
        [SerializeField, Min(0.001f)] private float pressDistanceMeters = 0.025f;
        [SerializeField, Min(0.001f)] private float releaseDistanceMeters = 0.04f;
        [SerializeField, Min(0.05f)] private float staleAfterSeconds = 0.25f;
        private readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
        private XRHandSubsystem subsystem;
        private PinchGestureState left, right;
        private readonly Pose[,] poses = new Pose[2, 26];
        private readonly bool[,] valid = new bool[2, 26];
        private readonly double[] lastSample = { double.NegativeInfinity, double.NegativeInfinity };
        private readonly bool[] tracked = new bool[2];
        private readonly float[] distances = { float.NaN, float.NaN };
        private double nextSearch;
        private bool focused = true, suspended;

        public bool LeftTracked => tracked[0];
        public bool RightTracked => tracked[1];
        public bool SubsystemRunning => subsystem != null && subsystem.running;
        public event Action<GestureEvent> GestureStarted;
        public event Action<GestureEvent> GestureEnded;
        public bool IsPinching(Hand hand) => (hand == Hand.Left ? left : right)?.IsPinching ?? false;
        public float PinchDistance(Hand hand) => distances[(int)hand];
        public string Status => !focused || suspended ? "Application not focused / suspended" :
            SubsystemRunning ? "XR Hands subsystem running" : "No running XR Hands subsystem - check Android OpenXR features";

        private void Awake()
        {
            left = new PinchGestureState(Hand.Left, pressDistanceMeters, releaseDistanceMeters);
            right = new PinchGestureState(Hand.Right, pressDistanceMeters, releaseDistanceMeters);
            left.Started += EmitStarted; right.Started += EmitStarted;
            left.Ended += EmitEnded; right.Ended += EmitEnded;
        }
        private void EmitStarted(GestureEvent e) => GestureStarted?.Invoke(e);
        private void EmitEnded(GestureEvent e) => GestureEnded?.Invoke(e);
        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (subsystem != null && !subsystem.running) Detach();
            if (subsystem == null && now >= nextSearch)
            {
                nextSearch = now + 1;
                SubsystemManager.GetSubsystems(subsystems);
                foreach (var candidate in subsystems)
                    if (candidate.running)
                    {
                        subsystem = candidate;
                        subsystem.updatedHands += OnUpdated;
                        subsystem.trackingLost += OnTrackingLost;
                        break;
                    }
            }
            for (int i = 0; i < 2; i++)
                if (!focused || suspended || now - lastSample[i] > staleAfterSeconds) Clear((Hand)i);
        }
        private void OnUpdated(XRHandSubsystem source, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType update)
        {
            if (update != XRHandSubsystem.UpdateType.Dynamic || !focused || suspended) return;
            Capture(Hand.Left, source.leftHand, (flags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0);
            Capture(Hand.Right, source.rightHand, (flags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0);
        }
        private void Capture(Hand hand, XRHand data, bool jointsUpdated)
        {
            if (!data.isTracked || !jointsUpdated) { Clear(hand); return; }
            int h = (int)hand; tracked[h] = true; lastSample[h] = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < 26; i++)
                valid[h, i] = data.GetJoint(XRHandJointIDUtility.FromIndex(i)).TryGetPose(out poses[h, i]);
            int thumb = XRHandJointID.ThumbTip.ToIndex(), index = XRHandJointID.IndexTip.ToIndex();
            bool tipsValid = valid[h, thumb] && valid[h, index];
            distances[h] = tipsValid ? Vector3.Distance(poses[h, thumb].position, poses[h, index].position) : float.NaN;
            (hand == Hand.Left ? left : right).Update(tipsValid, distances[h]);
        }
        private void OnTrackingLost(XRHand hand) => Clear(hand.handedness == Handedness.Left ? Hand.Left : Hand.Right);
        private void Clear(Hand hand)
        {
            int h = (int)hand; tracked[h] = false; distances[h] = float.NaN;
            for (int i = 0; i < 26; i++) valid[h, i] = false;
            (hand == Hand.Left ? left : right)?.Cancel();
        }
        // Poses are in tracking-origin space, never world space. Apply the rig offset once.
        public bool TryGetJointPose(Hand hand, XRHandJointID joint, out Pose pose)
        {
            int h = (int)hand, i = joint.ToIndex();
            pose = default;
            if (i < 0 || i >= 26 || !tracked[h] || !valid[h, i] || Time.realtimeSinceStartupAsDouble - lastSample[h] > staleAfterSeconds) return false;
            pose = poses[h, i]; return true;
        }
        private void Detach()
        {
            if (subsystem != null) { subsystem.updatedHands -= OnUpdated; subsystem.trackingLost -= OnTrackingLost; subsystem = null; }
            Clear(Hand.Left); Clear(Hand.Right);
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) { Clear(Hand.Left); Clear(Hand.Right); } }
        private void OnApplicationPause(bool value) { suspended = value; if (value) { Clear(Hand.Left); Clear(Hand.Right); } }
        private void OnDisable() { Detach(); nextSearch = 0; }
    }
}
