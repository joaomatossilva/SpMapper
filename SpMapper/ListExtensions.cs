using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
#if SPCLIENT2010
using Microsoft.SharePoint.Client;
#else
using Microsoft.SharePoint;
#endif

namespace SpMapper {

	public interface ISpEntity {
		int Id { get; }
	}

	public static class ListExtensions {

		class PropertyMap {
			public PropertyInfo Property { get; set; }
			public Type PropertyType { get; set; }
			public bool IsNullableType { get; set; }
			public string MapToFieldName { get; set; }
			public bool ReadOnly { get; set; }
		}

#if SPCLIENT2010
		public static IEnumerable<T> Query<T>(this List list, string camlQuery, ClientContext context) where T : new() {
			var query = new CamlQuery { ViewXml = camlQuery };
			return Query<T>(list, query, context);
		}

		public static IEnumerable<T> Query<T>(this List list, CamlQuery query, ClientContext context) where T : new() {
			var items = list.GetItems(query);
			context.Load(items);
			context.Load(list.Fields);
			context.ExecuteQuery();
			var map = BuildMap(typeof(T), list.Fields.ToList().Select(field => field.InternalName));
			foreach (ListItem item in items) {
				yield return BuildObject<T>(map, fieldName => item[fieldName]);
			}
		}

		public static void Insert<T>(this List list, T itemToInsert, ClientContext context) where T : ISpEntity {
			Insert(list, (IEnumerable<T>)new[] { itemToInsert }, context);
		}

		public static void Insert<T>(this List list, IEnumerable<T> itemsToInsert, ClientContext context) where T : ISpEntity {
			context.Load(list.Fields);
			context.ExecuteQuery();
			var map = BuildMap(typeof(T), list.Fields.ToList().Select(field => field.InternalName));
			var creatItemInfo = new ListItemCreationInformation();
			foreach (var itemToInsert in itemsToInsert) {
				var item = list.AddItem(creatItemInfo);
				SetItemValues(map, itemToInsert, (fieldName, value) => { item[fieldName] = value; });
				item.Update();
			}
			context.ExecuteQuery();
		}

		public static void Update<T>(this List list, T itemToUpdate, ClientContext context) where T : ISpEntity {
			Update(list, (IEnumerable<T>)new[] { itemToUpdate }, context);
		}

		public static void Update<T>(this List list, IEnumerable<T> itemsToUpdate, ClientContext context) where T : ISpEntity {
			context.Load(list.Fields);
			context.ExecuteQuery();
			var map = BuildMap(typeof(T), list.Fields.ToList().Select(field => field.InternalName));
			foreach (var itemToUpdate in itemsToUpdate) {
				var item = list.GetItemById(itemToUpdate.Id);
				SetItemValues(map, itemToUpdate, (fieldName, value) => { item[fieldName] = value; });
				item.Update();
			}
			context.ExecuteQuery();
		}

		public static void Delete<T>(this List list, T itemToDelete, ClientContext context) where T : ISpEntity {
			Delete(list, (IEnumerable<T>)new[] { itemToDelete }, context);
		}

		public static void Delete<T>(this List list, IEnumerable<T> itemsToDelete, ClientContext context) where T : ISpEntity {
			foreach (var itemToDelete in itemsToDelete) {
				var item = list.GetItemById(itemToDelete.Id);
				item.DeleteObject();
			}
			context.ExecuteQuery();
		}
#else
		public static IEnumerable<T> Query<T>(this SPList list, string camlQuery) where T : new() {
			var query = new SPQuery { Query = camlQuery };
			return Query<T>(list, query);
		}
		
		public static IEnumerable<T> Query<T>(this SPList list, SPQuery query) where T : new() {
			var items = list.GetItems(query);
			var map = BuildMap(typeof(T), GetFieldNames(list));
			foreach (SPListItem item in items) {
				yield return BuildObject<T>(map, fieldName => item[fieldName]);
			}
		}

		public static void Insert<T>(this SPList list, T itemToInsert) where T : ISpEntity {
			Insert(list, (IEnumerable<T>)new[] { itemToInsert });
		}

		public static void Insert<T>(this SPList list, IEnumerable<T> itemsToInsert) where T : ISpEntity {
			var map = BuildMap(typeof(T), GetFieldNames(list));
			foreach (var itemToInsert in itemsToInsert) {
#if SP2007
				SPItem item = list.Items.Add();
#else
				SPItem item = list.AddItem();
#endif
				SetItemValues(map, itemToInsert, (fieldName, value) => { item[fieldName] = value; });
				item.Update();				
			}
		}

		public static void Update<T>(this SPList list, T itemToUpdate) where T : ISpEntity {
			Update(list, (IEnumerable<T>)new[] { itemToUpdate });
		}

		public static void Update<T>(this SPList list, IEnumerable<T> itemsToUpdate) where T : ISpEntity {
			var map = BuildMap(typeof(T), GetFieldNames(list));
			foreach (var itemToUpdate in itemsToUpdate) {
				SPItem item = list.GetItemById(itemToUpdate.Id);
				SetItemValues(map, itemToUpdate, (fieldName, value) => { item[fieldName] = value; });
				item.Update();
			}
		}

		public static void Delete<T>(this SPList list, T itemToDelete) where T : ISpEntity {
			Delete(list, (IEnumerable<T>)new[] { itemToDelete });
		}

		public static void Delete<T>(this SPList list, IEnumerable<T> itemsToDelete ) where T : ISpEntity {
			foreach (var itemToDelete in itemsToDelete) {
				SPItem item = list.GetItemById(itemToDelete.Id);
				item.Delete();
				item.Update();
			}
		}

		private static IEnumerable<string> GetFieldNames(SPList list) {
			var fieldNames = new List<string>();
			foreach (SPField field in list.Fields) {
				fieldNames.Add(field.InternalName);
			}
			return fieldNames;
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
					if (fieldName.Equals("ID", StringComparison.InvariantCultureIgnoreCase)) {
						propertyMap.ReadOnly = true;
					}
					typeMap.Add(propertyMap);
				}
			}
			return typeMap;
		}

		private static T BuildObject<T>(IEnumerable<PropertyMap> typeMap, Func<string, object> getValue) where T : new() {
			var obj = new T();
			foreach (var propertyMap in typeMap) {
				object propertyValue = getValue(propertyMap.MapToFieldName);
				if(propertyMap.IsNullableType && propertyValue == null) {
					propertyMap.Property.SetValue(obj, null, null);
				}else {
					object settablePropertyValue = Convert.ChangeType(propertyValue, propertyMap.PropertyType);
					propertyMap.Property.SetValue(obj, settablePropertyValue, null);
				}
			}
			return obj;
		}

		private static void SetItemValues<T>(IEnumerable<PropertyMap> typeMap, T item, Action<string, object> setValue) where T : ISpEntity {
			foreach (var propertyMap in typeMap.Where( tMap => tMap.ReadOnly == false)) {
				object value = propertyMap.Property.GetValue(item, null);
				setValue(propertyMap.MapToFieldName, value);
			}
		}
	}
}
