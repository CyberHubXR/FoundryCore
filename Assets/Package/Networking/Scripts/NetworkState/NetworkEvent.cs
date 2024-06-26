using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Foundry.Core.Serialization;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Foundry
{
    public enum NetEventSource
    {
        Local,
        Remote
    }

    [Serializable]
    public class NetworkEventBase
    {
        
    }
    
    /// <summary>
    /// This event is synced across the network. In most ways it functions identically to UnityEvents, but it is even less
    /// performant because of the serialization and network overhead. Use only for events that need to be synced across the network.
    /// </summary>
    /// <typeparam name="T">T must be a commonly serializable type or implement IFoundrySerializable</typeparam>
    [Serializable]
    public class NetworkEvent<T> : NetworkEventBase, INetworkEvent
    {
        private uint maxQueueLength = 5;
        
        private Queue<T> callArgs = new();
        [SerializeField]
        private UnityEvent<NetEventSource, T> _event = new();
        
        private IFoundrySerializer tSerializer;
        
        /// <summary>
        /// The max amount of events that may be queued up between serializations. If this is exceeded, the oldest events will be removed.
        /// </summary>
        public uint MaxQueueLength
        {
            get => maxQueueLength;
            set
            {
                maxQueueLength = value;
                
                // Remove the oldest events if we are over the max queue length
                while (callArgs.Count > maxQueueLength)
                {
                    Debug.LogWarning("NetworkEvent queue exceeded max length, removing oldest event");
                    callArgs.Dequeue();
                }
            }
        }
        
        public int EventCount => callArgs.Count;
        
        public IFoundrySerializer ArgSerializer => tSerializer;

        public bool TryDequeue(out object callArgs)
        {
            if (this.callArgs.Count > 0)
            {
                callArgs = this.callArgs.Dequeue();
                return true;
            }

            callArgs = null;
            return false;
        }
        
        public void DeserializeEvent(BinaryReader reader)
        {
            T argValue = default;
            try
            {
                tSerializer ??= SetSerializer(argValue);
                object v = argValue;
                tSerializer.Deserialize(ref v, reader);
                argValue = (T)v;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                _event.Invoke(NetEventSource.Remote, argValue);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void EnqueueNetCall(T arg)
        {
            callArgs.Enqueue(arg);

            tSerializer ??= SetSerializer(arg);

            // Remove the oldest events if we are over the max queue length
            while (callArgs.Count > maxQueueLength)
            {
                Debug.LogWarning("NetworkEvent queue exceeded max length, removing oldest event");
                callArgs.Dequeue();
            }
        }
        
        IFoundrySerializer SetSerializer(T value)
        {
            if (value is IFoundrySerializable serializable)
                tSerializer = serializable.GetSerializer();
            else
                tSerializer = FoundrySerializerFinder.GetSerializer(typeof(T));
            Debug.Assert(tSerializer != null, $"Serializer for {typeof(T)} was null");
            return tSerializer;
        }
        
        /// <summary>
        /// Add a listener to this event. This is called when the event is invoked. Use NetEventSource to filter between local and remote calls.
        /// </summary>
        /// <param name="call"></param>
        public void AddListener(UnityAction<NetEventSource, T> call)
        {
            _event.AddListener(call);
        }
        
        public void RemoveListener(UnityAction<NetEventSource, T> call)
        {
            _event.RemoveListener(call);
        }
        
        /// <summary>
        /// Invoke this event. This will call all listeners on all clients.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="arg"></param>
        public void Invoke(T arg)
        {
            EnqueueNetCall(arg);
            _event.Invoke(NetEventSource.Local, arg);
        }

        /// <summary>
        /// Invoke this event locally. This will only call listeners on the local client.
        /// </summary>
        /// <param name="arg"></param>
        public void InvokeLocal(T arg)
        {
            _event.Invoke(NetEventSource.Local, arg);
        }
        
        /// <summary>
        /// Invoke this event remotely. This will only call listeners on remote clients.
        /// </summary>
        /// <param name="arg"></param>
        public void InvokeRemote(T arg)
        {
            EnqueueNetCall(arg);
        }
    }
    
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(NetworkEventBase), true)]
    public class NetworkEventDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_event"), label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_event"), label);
            EditorGUI.EndProperty();
        }
    }
    #endif
}
