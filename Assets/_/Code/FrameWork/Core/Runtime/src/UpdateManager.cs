using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// Central update loop. Objects implementing <see cref="IUpdatable"/> register
    /// here and are ticked from a single place. This is the only class allowed to
    /// use MonoBehaviour.Update, and it carries loop plumbing only, no game logic.
    /// </summary>
    public sealed class UpdateManager : MonoBehaviour
    {
        #region Public API

        /// <summary>Registers an updatable so it starts receiving ticks.</summary>
        public void Register(IUpdatable updatable)
        {
            if (updatable == null) return;
            if (_updatables.Contains(updatable)) return;
            if (_pendingAdd.Contains(updatable)) return;

            _pendingAdd.Add(updatable);
        }

        /// <summary>Unregisters an updatable so it stops receiving ticks.</summary>
        public void Unregister(IUpdatable updatable)
        {
            if (updatable == null) return;

            _pendingRemove.Add(updatable);
        }

        #endregion


        #region Unity Callbacks

        private void Update()
        {
            FlushPending();

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].Tick(deltaTime);
            }
        }

        #endregion


        #region Private Fields

        private readonly List<IUpdatable> _updatables = new();
        private readonly List<IUpdatable> _pendingAdd = new();
        private readonly List<IUpdatable> _pendingRemove = new();

        #endregion


        #region Private Methods

        // Applies buffered registrations/removals between frames so the active
        // list is never mutated while it is being iterated.
        private void FlushPending()
        {
            if (_pendingRemove.Count > 0)
            {
                for (int i = 0; i < _pendingRemove.Count; i++)
                {
                    _updatables.Remove(_pendingRemove[i]);
                }

                _pendingRemove.Clear();
            }

            if (_pendingAdd.Count > 0)
            {
                _updatables.AddRange(_pendingAdd);
                _pendingAdd.Clear();
            }
        }

        #endregion
    }
}