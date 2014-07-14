﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace SoftwareBotany.Sunlight
{
    public sealed class Query<TPrimaryKey>
    {
        internal Query(IEngine<TPrimaryKey> engine)
        {
            _engine = engine;
        }

        private readonly IEngine<TPrimaryKey> _engine;
        private int _executed = 0;

        public TimeSpan? ExecuteElapsedTime { get; private set; }

        private void ThrowIfEngineMismatch(ICatalogHandle catalog)
        {
            if (!_engine.HasCatalog(catalog))
                throw new ArgumentException("Catalog belongs to a different Engine.", "catalog");
        }

        #region Amongst

        public IEnumerable<TPrimaryKey> AmongstPrimaryKeys { get { return _amongstPrimaryKeys; } }
        private HashSet<TPrimaryKey> _amongstPrimaryKeys = new HashSet<TPrimaryKey>();

        public void AddAmongstPrimaryKeys(IEnumerable<TPrimaryKey> primaryKeys)
        {
            if (primaryKeys == null)
                throw new ArgumentNullException("primaryKeys");

            Contract.EndContractBlock();

            _amongstPrimaryKeys.UnionWith(primaryKeys);
        }

        #endregion

        #region Filter

        public IEnumerable<IFilterParameter> FilterParameters { get { return _filterParameters; } }
        private readonly List<IFilterParameter> _filterParameters = new List<IFilterParameter>();

        public FilterParameter<TKey> AddFilterExactParameter<TKey>(ICatalogHandle<TKey> catalog, TKey exact)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            ThrowIfFilterInvalid(catalog);

            var filterParameter = new FilterParameter<TKey>(catalog, exact);
            _filterParameters.Add(filterParameter);

            return filterParameter;
        }

        public IFilterParameter AddFilterExactParameter(string name, object exact)
        {
            var catalog = _engine.GetCatalog(name);

            ThrowIfFilterInvalid(catalog);

            var filterParameter = catalog.CreateFilterParameter(exact);
            _filterParameters.Add(filterParameter);

            return filterParameter;
        }

        public FilterParameter<TKey> AddFilterEnumerableParameter<TKey>(ICatalogHandle<TKey> catalog, IEnumerable<TKey> enumerable)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            ThrowIfFilterInvalid(catalog);

            var filterParameter = new FilterParameter<TKey>(catalog, enumerable);
            _filterParameters.Add(filterParameter);

            return filterParameter;
        }

        public IFilterParameter AddFilterEnumerableParameter(string name, IEnumerable enumerable)
        {
            var catalog = _engine.GetCatalog(name);

            ThrowIfFilterInvalid(catalog);

            var filterParameter = catalog.CreateFilterParameter(enumerable);
            _filterParameters.Add(filterParameter);

            return filterParameter;
        }

        public FilterParameter<TKey> AddFilterRangeParameter<TKey>(ICatalogHandle<TKey> catalog, TKey rangeMin, TKey rangeMax)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            ThrowIfFilterInvalid(catalog);

            var filterParameter = new FilterParameter<TKey>(catalog, rangeMin, rangeMax);
            _filterParameters.Add(filterParameter);

            return filterParameter;
        }

        public IFilterParameter AddFilterRangeParameter(string name, object rangeMin, object rangeMax)
        {
            var catalog = _engine.GetCatalog(name);

            ThrowIfFilterInvalid(catalog);

            var filterParameter = catalog.CreateFilterParameter(rangeMin, rangeMax);
            _filterParameters.Add(filterParameter);

            return filterParameter;
        }

        private void ThrowIfFilterInvalid(ICatalogHandle catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException("catalog");

            Contract.EndContractBlock();

            ThrowIfEngineMismatch(catalog);
            ThrowIfFilterDuplicate(catalog);
        }

        private void ThrowIfFilterDuplicate(ICatalogHandle catalog)
        {
            if (catalog.IsOneToOne && _filterParameters.Any(parameter => parameter.Catalog == catalog))
                throw new NotSupportedException("Can only add 1 Filter Parameter per one-to-one Catalog.");
        }

        #endregion

        #region Sort

        public IEnumerable<ISortParameter> SortParameters { get { return _sortParameters; } }
        private readonly List<ISortParameter> _sortParameters = new List<ISortParameter>();

        public SortParameter<TKey> AddSortParameter<TKey>(ICatalogHandle<TKey> catalog, bool ascending)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            ThrowIfSortInvalid(catalog);

            var sortParameter = new SortParameter<TKey>(catalog, ascending);
            _sortParameters.Add(sortParameter);

            return sortParameter;
        }

        public ISortParameter AddSortParameter(string name, bool ascending)
        {
            var catalog = _engine.GetCatalog(name);

            ThrowIfSortInvalid(catalog);

            var sortParameter = catalog.CreateSortParameter(ascending);
            _sortParameters.Add(sortParameter);

            return sortParameter;
        }

        private bool? _sortPrimaryKeyAscending;

        public bool? SortPrimaryKeyAscending
        {
            get { return _sortPrimaryKeyAscending; }
            set
            {
                ThrowIfSortPrimaryKeyExists();

                _sortPrimaryKeyAscending = value;
            }
        }

        private void ThrowIfSortInvalid(ICatalogHandle catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException("catalog");

            Contract.EndContractBlock();

            ThrowIfEngineMismatch(catalog);
            ThrowIfSortPrimaryKeyExists();
            ThrowIfSortDuplicate(catalog);
        }

        private void ThrowIfSortPrimaryKeyExists()
        {
            if (SortPrimaryKeyAscending.HasValue)
                throw new NotSupportedException("Cannot modify Sort Parameters after a Sort Primary Key has been set.");
        }

        private void ThrowIfSortDuplicate(ICatalogHandle catalog)
        {
            if (_sortParameters.Any(parameter => parameter.Catalog == catalog))
                throw new NotSupportedException("Can only add 1 Sort Parameter per Catalog.");
        }

        #endregion

        #region Facet

        public bool FacetDisableParallel { get; set; }
        public bool FacetShortCircuitCounting { get; set; }

        public IEnumerable<IFacetParameter> FacetParameters { get { return _facetParameters; } }
        internal IEnumerable<IFacetParameterInternal> FacetParametersInternal { get { return _facetParameters; } }
        private readonly List<IFacetParameterInternal> _facetParameters = new List<IFacetParameterInternal>();

        public FacetParameter<TKey> AddFacetParameter<TKey>(ICatalogHandle<TKey> catalog)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            ThrowIfFacetInvalid(catalog);

            var facetParameter = new FacetParameter<TKey>(catalog);
            _facetParameters.Add(facetParameter);

            return facetParameter;
        }

        public IFacetParameter AddFacetParameter(string name)
        {
            var catalog = _engine.GetCatalog(name);

            ThrowIfFacetInvalid(catalog);

            var facetParameter = catalog.CreateFacetParameter();
            _facetParameters.Add(facetParameter);

            return facetParameter;
        }

        private void ThrowIfFacetInvalid(ICatalogHandle catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException("catalog");

            Contract.EndContractBlock();

            ThrowIfEngineMismatch(catalog);
            ThrowIfFacetDuplicate(catalog);
        }

        private void ThrowIfFacetDuplicate(ICatalogHandle catalog)
        {
            if (_facetParameters.Any(parameter => parameter.Catalog == catalog))
                throw new NotSupportedException("Can only add 1 Facet Parameter per Catalog.");
        }

        #endregion

        public TPrimaryKey[] Execute(int skip, int take, out int totalCount)
        {
            if (Interlocked.CompareExchange(ref _executed, 1, 0) > 0)
                throw new NotSupportedException("Query already executed.");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            TPrimaryKey[] results = _engine.ExecuteQuery(this, skip, take, out totalCount);

            watch.Stop();
            ExecuteElapsedTime = watch.Elapsed;

            return results;
        }
    }
}