using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
#if SPCLIENT2010
using Microsoft.SharePoint.Client;
#else
using Microsoft.SharePoint;
#endif

namespace SpMapper {

	public static class ListExtensions {

		class PropertyMap {
			public PropertyInfo Property { get; set; }
			public Type PropertyType { get; set; }
			public bool IsNullableType { get; set; }
			public string MapToFieldName { get; set; }
		}

#if SPCLIENT2010
		public static IEnumerable<T> Query<T>(this List list, string camlQuery) where T : new() {
			var query = new CamlQuery { ViewXml = camlQuery };
			return Query<T>(list, query);
		}

		public static IEnumerable<T> Query<T>(this List list, CamlQuery query) where T : new() {
			var items = list.GetItems(query);
			var map = BuildMap(typeof(T), list.Fields.Select( field => field.InternalName));
			foreach (ListItem item in items) {
				yield return BuildObject<T>(map, fieldName => item[fieldName]);
			}
		}
#else
		public static IEnumerable<T> Query<T>(this SPList list, string camlQuery) where T : new() {
			var query = new SPQuery { Query = camlQuery };
			return Query<T>(list, query);
		}

		public static IEnumerable<T> Query<T>(this SPList list, SPQuery query) where T : new() {
			var items = list.GetItems(query);
			var fieldNames = new List<string>();
			foreach (SPField field in list.Fields) {
				fieldNames.Add(field.InternalName);
			}
			var map = BuildMap(typeof(T), fieldNames);
			foreach (SPListItem item in items) {
				yield return BuildObject<T>(map, fieldName => item[fieldName]);
			}
		}
#endif

		private static IEnumerable<PropertyMap> BuildMap(Type type, IEnumerable<string> fieldNames) {
			var typeMap = new List<PropertyMap>();
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
			foreach (var property in properties) {
				var fieldName = fieldNames.FirstOrDefault(field => field.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));
				if (!string.IsNullOrEmpty(fieldName)) {
					var propertyMap = new PropertyMap { Property = property, MapToFieldName = fieldName };
					var propertyType = property.PropertyType;
					if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
						propertyMap.IsNullableType = true;
						propertyMap.PropertyType = Nullable.GetUnderlyingType(propertyType);
					} else {
						propertyMap.PropertyType = propertyType;
					}
					typeMap.Add(propertyMap);
				}
			}
			return typeMap;
		}



		private static T BuildObject<T>(IEnumerable<PropertyMap> typeMap, Func<string, object> getValue) where T : new() {
			var obj = new T();
			foreach (var propertyMap in typeMap) {
				object settablePropertyValue = Convert.ChangeType(getValue(propertyMap.MapToFieldName), propertyMap.PropertyType);
				propertyMap.Property.SetValue(obj, settablePropertyValue, null);
			}
			return obj;
		}
	}
}
