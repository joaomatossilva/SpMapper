using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.SharePoint;

namespace SpMapper {

	public static class ListExtensions {

		public static IEnumerable<T> Query<T>(this SPList list, string camlQuery) where T : new() {
			var query = new SPQuery { Query = camlQuery };
			return Query<T>(list, query);
		}

		public static IEnumerable<T> Query<T>(this SPList list, SPQuery query) where T : new() {
			var items = list.GetItems(query);
			var settablePropertiesOfType = DiscoverSettableProperties(typeof(T));
			foreach (SPListItem item in items) {
				yield return Buildobject<T>(settablePropertiesOfType, item);
			}
		}

		private static PropertyInfo[] DiscoverSettableProperties(Type type) {
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
			return properties;
		}

		private static T Buildobject<T>(PropertyInfo[] settableProperties, SPListItem item) where T : new() {
			T obj = new T();
			foreach (var settableProperty in settableProperties ) {
				if (item.Fields.ContainsField(settableProperty.Name)) {
					var propertyType = settableProperty.PropertyType;
					object settablePropertyValue;
					if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
						Type underlyingType = Nullable.GetUnderlyingType(propertyType);
						settablePropertyValue = Convert.ChangeType(item[settableProperty.Name], underlyingType);
					} else {
						settablePropertyValue = Convert.ChangeType(item[settableProperty.Name], propertyType);						
					}
					settableProperty.SetValue(obj, settablePropertyValue, null);
				}
			}
			return obj;
		}
	}
}
