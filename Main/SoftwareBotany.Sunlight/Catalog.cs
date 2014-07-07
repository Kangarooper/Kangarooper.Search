﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SoftwareBotany.Sunlight
{
    public sealed partial class Catalog<TKey> : ICatalog
      where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        public Catalog(string name, bool allowUnsafe, VectorCompression compression)
        {
            _name = name;
            _allowUnsafe = allowUnsafe;
            _compression = compression;
        }

        #region Optimize

        void ICatalog.OptimizeReadPhase(int[] bitPositionShifts)
        {
            foreach (Entry entry in _keyToEntryMap.Values)
                entry.IsVectorOptimizedAlive = entry.Vector.OptimizeReadPhase(bitPositionShifts, out entry.VectorOptimized);
        }

        void ICatalog.OptimizeWritePhase()
        {
            List<TKey> deadKeys = new List<TKey>();

            foreach (var keyAndEntry in _keyToEntryMap)
            {
                if (keyAndEntry.Value.IsVectorOptimizedAlive)
                    keyAndEntry.Value.Vector = keyAndEntry.Value.VectorOptimized;
                else
                    deadKeys.Add(keyAndEntry.Key);

                keyAndEntry.Value.VectorOptimized = null;
                keyAndEntry.Value.IsVectorOptimizedAlive = false;
            }

            foreach (TKey key in deadKeys)
            {
                _keys.Remove(key);
                _keyToEntryMap.Remove(key);
            }
        }

        #endregion

        public string Name { get { return _name; } }
        private readonly string _name;

        public bool AllowUnsafe { get { return _allowUnsafe; } }
        private readonly bool _allowUnsafe;

        public VectorCompression Compression { get { return _compression; } }
        private readonly VectorCompression _compression;

        private SortedSet<TKey> _keys = new SortedSet<TKey>();
        private Dictionary<TKey, Entry> _keyToEntryMap = new Dictionary<TKey, Entry>();

        // TODO : Better OOP... right now this is just serving as a mutable Tuple which is probably
        // not the best design.
        private class Entry
        {
            internal Entry(Vector vector) { Vector = vector; }

            internal Vector Vector;
            internal Vector VectorOptimized;
            internal bool IsVectorOptimizedAlive;
        }

        #region Set

        void ICatalog.Set(object key, int bitPosition, bool value)
        {
            if (key is TKey)
                Set((TKey)key, bitPosition, value);
            else
                Set((IEnumerable<TKey>)key, bitPosition, value);
        }

        public void Set(TKey key, int bitPosition, bool value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            Contract.EndContractBlock();

            Entry entry;

            if (!_keyToEntryMap.TryGetValue(key, out entry))
            {
                // TODO : This will use a VectorFactory pattern shortly.
                entry = new Entry(new Vector(_allowUnsafe, _compression));

                _keys.Add(key);
                _keyToEntryMap.Add(key, entry);
            }

            entry.Vector[bitPosition] = value;
        }

        public void Set(IEnumerable<TKey> keys, int bitPosition, bool value)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");

            Contract.EndContractBlock();

            foreach (TKey key in keys)
                Set(key, bitPosition, value);
        }

        #endregion

        #region Filter

        void ICatalog.FilterExact(Vector vector, object key) { Filter(vector, (TKey)key); }

        public void Filter(Vector vector, TKey key)
        {
            if (vector == null)
                throw new ArgumentNullException("vector");

            if (key == null)
                throw new ArgumentNullException("key");

            Contract.EndContractBlock();

            FilterImpl(vector, new[] { Lookup(key) });
        }

        void ICatalog.FilterEnumerable(Vector vector, object keys) { Filter(vector, (IEnumerable<TKey>)keys); }

        public void Filter(Vector vector, IEnumerable<TKey> keys)
        {
            if (vector == null)
                throw new ArgumentNullException("vector");

            if (keys == null)
                throw new ArgumentNullException("keys");

            if (keys.Any(key => key == null))
                throw new ArgumentNullException("keys", "All keys must be non-null.");

            Contract.EndContractBlock();

            FilterImpl(vector, keys.Distinct().Select(key => Lookup(key)));
        }

        void ICatalog.FilterRange(Vector vector, object keyMin, object keyMax) { Filter(vector, (TKey)keyMin, (TKey)keyMax); }

        public void Filter(Vector vector, TKey keyMin, TKey keyMax)
        {
            if (vector == null)
                throw new ArgumentNullException("vector");

            if (keyMin == null && keyMax == null)
                throw new ArgumentNullException("keyMin", "Either keyMin or keyMax must be non-null.");

            if (keyMin != null && keyMax != null && keyMin.CompareTo(keyMax) > 0)
                throw new ArgumentOutOfRangeException("keyMin", "keyMin must be <= keyMax.");

            Contract.EndContractBlock();

            if (keyMin == null)
                keyMin = _keys.Min;

            if (keyMax == null)
                keyMax = _keys.Max;

            FilterImpl(vector, _keys.Count == 0 ? new Vector[0] : _keys.GetViewBetween(keyMin, keyMax).Select(key => _keyToEntryMap[key].Vector));
        }

        private static void FilterImpl(Vector vector, IEnumerable<Vector> lookups)
        {
            Vector[] lookupsArray = lookups.Where(lookup => lookup != null).ToArray();

            if (lookupsArray.Length == 0)
                vector.WordsClear();
            else if (lookupsArray.Length == 1)
                vector.And(lookupsArray[0]);
            else
                vector.And(Vector.CreateUnion(lookupsArray));
        }

        private Vector Lookup(TKey key)
        {
            Entry entry;
            _keyToEntryMap.TryGetValue(key, out entry);

            return entry == null ? null : entry.Vector;
        }

        #endregion

        #region Facet

        IFacet ICatalog.Facet(Vector vector, bool disableParallel, bool shortCircuitCounting) { return Facet(vector, disableParallel, shortCircuitCounting); }

        public Facet<TKey> Facet(Vector vector, bool disableParallel = false, bool shortCircuitCounting = false)
        {
            if (vector == null)
                throw new ArgumentNullException("vector");

            Contract.EndContractBlock();

            var keyAndEntries = _keyToEntryMap.AsParallel();

            if (disableParallel)
                keyAndEntries = keyAndEntries.WithDegreeOfParallelism(1);

            var categories = shortCircuitCounting
                ? keyAndEntries.Where(keyAndEntry => vector.AndPopulationAny(keyAndEntry.Value.Vector)).Select(keyAndEntry => new FacetCategory<TKey>(keyAndEntry.Key, 1))
                : keyAndEntries.Select(keyAndEntry => new FacetCategory<TKey>(keyAndEntry.Key, vector.AndPopulation(keyAndEntry.Value.Vector)));

            return new Facet<TKey>(categories);
        }

        #endregion

        #region Sort

        ICatalogSortResult ICatalog.SortBitPositions(Vector vector, bool value, bool ascending) { return SortBitPositions(vector, value, ascending); }

        public CatalogSortResult<TKey> SortBitPositions(Vector vector, bool value, bool ascending)
        {
            if (vector == null)
                throw new ArgumentNullException("vector");

            Contract.EndContractBlock();

            // This uses SortedSet<T>.Reverse() and not the IEnumerable<T> extension method that suffers from greedy enumeration.
            var keys = ascending ? _keys : _keys.Reverse();

            var partialSortResults = keys.Where(key => vector.AndPopulationAny(_keyToEntryMap[key].Vector))
                .Select(key => new CatalogPartialSortResult<TKey>(key, vector.AndFilterBitPositions(_keyToEntryMap[key].Vector, value)));

            return new CatalogSortResult<TKey>(partialSortResults);
        }

        #endregion
    }
}