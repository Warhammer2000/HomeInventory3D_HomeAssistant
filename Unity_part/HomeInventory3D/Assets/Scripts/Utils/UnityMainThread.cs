using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace HomeInventory3D.Utils
{
    /// <summary>
    /// Dispatcher for executing actions on Unity's main thread from background threads.
    /// </summary>
    public class UnityMainThread : MonoBehaviour
    {
        private static UnityMainThread _instance;
        private readonly ConcurrentQueue<Action> _actions = new();

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (_instance == null)
            {
                Debug.LogWarning("UnityMainThread not initialized. Creating instance.");
                var go = new GameObject("[UnityMainThread]");
                _instance = go.AddComponent<UnityMainThread>();
                DontDestroyOnLoad(go);
            }

            _instance._actions.Enqueue(action);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
