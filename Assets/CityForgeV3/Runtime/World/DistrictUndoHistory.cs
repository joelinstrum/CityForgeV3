using System.Collections.Generic;

namespace CityForgeV3.World
{
    // Snapshots are immutable JSON strings; only the last five edit boundaries survive.
    public sealed class DistrictUndoHistory
    {
        public const int Capacity = 5;
        private readonly List<string> _previous = new();
        private string _current;
        public int Count => _previous.Count;
        public void Reset(string current)
        {
            _previous.Clear();
            _current = current;
        }
        public bool Commit(string current)
        {
            if (_current == current) return false;
            if (_current != null)
            {
                _previous.Add(_current);
                if (_previous.Count > Capacity) _previous.RemoveAt(0);
            }
            _current = current;
            return true;
        }
        public bool TryUndo(out string snapshot)
        {
            snapshot = null;
            if (_previous.Count == 0) return false;
            var last = _previous.Count - 1;
            snapshot = _previous[last];
            _previous.RemoveAt(last);
            _current = snapshot;
            return true;
        }
    }
}
