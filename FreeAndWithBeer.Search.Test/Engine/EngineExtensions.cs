﻿namespace FreeAndWithBeer.Search
{
    using System;
    using System.Linq;

    internal static class EngineExtensions
    {
        internal static FilterParameter<int> CreateRandomFilterExactParameter(this Engine<EngineItem, int> engine, ICatalogHandle<int> catalog, Random random, int max)
        {
            int exact = random.Next(max);

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, exact)
                : (FilterParameter<int>)FilterParameter.Create(engine, catalog.Name, exact);
        }

        internal static FilterParameter<int> CreateRandomFilterEnumerableParameter(this Engine<EngineItem, int> engine, ICatalogHandle<int> catalog, Random random, int max)
        {
            int[] enumerable = Enumerable.Range(0, random.Next(max))
                .Select(i => random.Next(max))
                .Distinct()
                .ToArray();

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, enumerable)
                : (FilterParameter<int>)FilterParameter.Create(engine, catalog.Name, enumerable);
        }

        internal static FilterParameter<int> CreateRandomFilterRangeParameter(this Engine<EngineItem, int> engine, ICatalogHandle<int> catalog, Random random, int max)
        {
            int val1 = random.Next(max);
            int val2 = random.Next(max);

            if (val1 == val2)
                val2++;

            int rangeMin = Math.Min(val1, val2);
            int rangeMax = Math.Max(val1, val2);

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, rangeMin, rangeMax)
                : (FilterParameter<int>)FilterParameter.Create(engine, catalog.Name, rangeMin, rangeMax);
        }

        internal static FilterParameter<DateTime> CreateRandomFilterExactParameter(this Engine<EngineItem, int> engine, ICatalogHandle<DateTime> catalog, Random random, int max)
        {
            DateTime exact = new DateTime(2011, 1, 1).AddDays(random.Next(max));

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, exact)
                : (FilterParameter<DateTime>)FilterParameter.Create(engine, catalog.Name, exact);
        }

        internal static FilterParameter<DateTime> CreateRandomFilterEnumerableParameter(this Engine<EngineItem, int> engine, ICatalogHandle<DateTime> catalog, Random random, int max)
        {
            DateTime[] enumerable = Enumerable.Range(0, random.Next(max))
                .Select(i => random.Next(max))
                .Distinct()
                .Select(i => new DateTime(2011, 1, 1).AddDays(i))
                .ToArray();

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, enumerable)
                : (FilterParameter<DateTime>)FilterParameter.Create(engine, catalog.Name, enumerable);
        }

        internal static FilterParameter<DateTime> CreateRandomFilterRangeParameter(this Engine<EngineItem, int> engine, ICatalogHandle<DateTime> catalog, Random random, int max)
        {
            int val1 = random.Next(max);
            int val2 = random.Next(max);

            if (val1 == val2)
                val2++;

            DateTime rangeMin = new DateTime(2011, 1, 1).AddDays(Math.Min(val1, val2));
            DateTime rangeMax = new DateTime(2011, 1, 1).AddDays(Math.Max(val1, val2));

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, rangeMin, rangeMax)
                : (FilterParameter<DateTime>)FilterParameter.Create(engine, catalog.Name, rangeMin, rangeMax);
        }

        internal static FilterParameter<string> CreateRandomFilterExactParameter(this Engine<EngineItem, int> engine, ICatalogHandle<string> catalog, Random random, int max)
        {
            string exact = random.Next(max).ToString();

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, exact)
                : (FilterParameter<string>)FilterParameter.Create(engine, catalog.Name, exact);
        }

        public static FilterParameter<string> CreateRandomFilterEnumerableParameter(this Engine<EngineItem, int> engine, ICatalogHandle<string> catalog, Random random, int max)
        {
            string[] enumerable = Enumerable.Range(0, random.Next(max))
                .Select(i => random.Next(max))
                .Distinct()
                .Select(i => i.ToString())
                .ToArray();

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, enumerable)
                : (FilterParameter<string>)FilterParameter.Create(engine, catalog.Name, enumerable);
        }

        internal static FilterParameter<string> CreateRandomFilterRangeParameter(this Engine<EngineItem, int> engine, ICatalogHandle<string> catalog, Random random, int max)
        {
            int val1 = random.Next(max);
            int val2 = random.Next(max);

            if (val1 == val2)
                val2++;

            string rangeMin = val1.ToString();
            string rangeMax = val2.ToString();

            if (rangeMin.CompareTo(rangeMax) > 0)
            {
                string s = rangeMin;
                rangeMin = rangeMax;
                rangeMax = s;
            }

            return random.Next() % 2 == 0
                ? FilterParameter.Create(catalog, rangeMin, rangeMax)
                : (FilterParameter<string>)FilterParameter.Create(engine, catalog.Name, rangeMin, rangeMax);
        }
    }
}