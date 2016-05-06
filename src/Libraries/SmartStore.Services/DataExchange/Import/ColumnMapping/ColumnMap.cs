﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SmartStore.Services.DataExchange.Import
{
	public class ColumnMap
	{
		// maps source column to property
		private readonly Dictionary<string, ColumnMappingValue> _map = new Dictionary<string, ColumnMappingValue>(StringComparer.OrdinalIgnoreCase);

		private static bool IsIndexed(string name)
		{
			return (name.EmptyNull().EndsWith("]") && name.EmptyNull().Contains("["));
		}

		private static string CreateSourceName(string name, string index)
		{
			if (index.HasValue())
			{
				name += String.Concat("[", index, "]");
			}

			return name;
		}

		public IReadOnlyDictionary<string, ColumnMappingValue> Mappings
		{
			get { return _map; }
		}

		public static bool ParseSourceName(string sourceName, out string nameWithoutIndex, out string index)
		{
			nameWithoutIndex = sourceName;
			index = null;

			var result = true;

			if (sourceName.HasValue() && IsIndexed(sourceName))
			{
				var x1 = sourceName.IndexOf('[');
				var x2 = sourceName.IndexOf(']', x1);

				if (x1 != -1 && x2 != -1 && x2 > x1)
				{
					nameWithoutIndex = sourceName.Substring(0, x1);
					index = sourceName.Substring(x1 + 1, x2 - x1 - 1);
				}
				else
				{
					result = false;
				}
			}

			return result;
		}

		//public IEnumerable<KeyValuePair<string, ColumnMappingValue>> GetInvalidMappings()
		//{
		//	var mappings = Mappings.Where(x => 
		//		x.Value.Property.HasValue() &&
		//		Mappings.Count(y => y.Value.Property.IsCaseInsensitiveEqual(x.Value.Property)) > 1
		//	);

		//	return mappings;
		//}

		public bool AddMapping(string sourceName, string mappedName, string defaultValue = null)
		{
			return AddMapping(sourceName, null, mappedName, defaultValue);
        }

		public bool AddMapping(string sourceName, string index, string mappedName, string defaultValue = null)
		{
			Guard.ArgumentNotEmpty(() => sourceName);
			Guard.ArgumentNotEmpty(() => mappedName);

			var isAlreadyMapped = (mappedName.HasValue() && _map.Any(x => x.Value.MappedName.IsCaseInsensitiveEqual(mappedName)));

			if (isAlreadyMapped)
				return false;

			_map[CreateSourceName(sourceName, index)] = new ColumnMappingValue
			{
				MappedName = mappedName,
				Default = defaultValue
			};

			return true;
		}

		/// <summary>
		/// Gets a mapped column value
		/// </summary>
		/// <param name="sourceName">The name of the column to get a mapped value for.</param>
		/// <param name="index">The column index, e.g. a language code (de, en etc.)</param>
		/// <returns>The mapped column value OR - if the name is unmapped - a value with the passed <paramref name="sourceName"/>[<paramref name="index"/>]</returns>
		public ColumnMappingValue GetMapping(string sourceName, string index)
		{
			return GetMapping(CreateSourceName(sourceName, index));
		}

		/// <summary>
		/// Gets a mapped column value
		/// </summary>
		/// <param name="sourceName">The name of the column to get a mapped value for.</param>
		/// <returns>The mapped column value OR - if the name is unmapped - the value of the passed <paramref name="sourceName"/></returns>
		public ColumnMappingValue GetMapping(string sourceName)
		{
			ColumnMappingValue result;

			if (_map.TryGetValue(sourceName, out result))
			{
				return result;
			}

			return new ColumnMappingValue { MappedName = sourceName };
		}
	}


	[JsonObject(MemberSerialization.OptIn)]
	public class ColumnMappingValue
	{
		/// <summary>
		/// The mapped name
		/// </summary>
		[JsonProperty]
		public string MappedName { get; set; }

		/// <summary>
		/// An optional default value
		/// </summary>
		[JsonProperty]
		public string Default { get; set; }

		/// <summary>
		/// Indicates whether to explicitly ignore this property
		/// </summary>
		public bool IgnoreProperty
		{
			get { return Default != null && Default == "[IGNOREPROPERTY]"; }
		}
	}
}
