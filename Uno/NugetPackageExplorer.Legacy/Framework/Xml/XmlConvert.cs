using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace NupkgExplorer.Framework.Xml
{
	/// <summary>
	/// Simplified version of XmlSerializer, with case-insensitive name mappings and more lenient xml attribute requirements.
	/// </summary>
	/// <remarks>Xml attributes may not be used for its original purpose.</remarks>
	public static class XmlConvert
	{
		private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(typeof(XmlConvert).Log);

		/// <summary>
		/// Deserializes the XElement to the specified .NET type.
		/// </summary>
		public static T DeserializeObject<T>(this XElement element) where T : new() => element.PopulateTo(new T());

		/// <summary>
		/// Populates the object with values from the XElement.
		/// </summary>
		public static T PopulateTo<T>(this XElement element, T instance) => (T)element.PopulateTo((object)instance);


		/// <summary>
		/// Populates the object with values from the <see cref="XElement"/>
		/// </summary>
		/// <param name="element"></param>
		/// <param name="instance">The target object to populate the values onto</param>
		/// <returns></returns>
		private static object PopulateTo(this XElement element, object instance)
		{
			var properties = instance.GetType().GetProperties()
				.Where(x => x.SetMethod != null);

			// DAPs are nested directly under the parent element without any array wrapper
			var directArrayProperties = properties
				.Where(x => x.PropertyType.IsArray)
				.Select(x => new
				{
					Property = x,
					ElementType = x.PropertyType.GetElementType(),
					Constructors = x.GetCustomAttributes<XmlElementAttribute>()
						.ToDictionary(
							y => y.ElementName ?? x.Name,
							y => (y.Type ?? x.PropertyType.GetElementType()) is var type && type.GetConstructor(Type.EmptyTypes) != null
								? (Func<object>)(() => Activator.CreateInstance(type))
								: throw new MissingMethodException($"No parameterless ctor defined for {type}"),
							StringComparer.InvariantCultureIgnoreCase),
					Buffer = new List<object>(),
				})
				.Where(x => x.Constructors.Any())
				.ToArray();

			foreach (var attribute in element.Attributes())
			{
				var property = properties.FirstOrDefault(p => p.Name.Equals(attribute.Name.LocalName, StringComparison.InvariantCultureIgnoreCase));
				if (property != null)
				{
					var result = TryParseValue(property.PropertyType, attribute.Value, throwException: true);
					if (result.Success == true)
					{
						property.SetValue(instance, result.Value);
					}
					else
					{
						_logger.Value.Error($"Unknown property type: {property.PropertyType} <- @{attribute.Name.LocalName}");
					}
				}
				else
				{
					_logger.Value.Error($"Unmapped property: @{attribute.Name.LocalName}");
				}
			}
			foreach (var child in element.Elements())
			{
				var property = properties.FirstOrDefault(p => p.Name.Equals(child.Name.LocalName, StringComparison.InvariantCultureIgnoreCase));
				if (property != null)
				{
					if (property.PropertyType.IsArray)
					{
						var elementType = property.PropertyType.GetElementType();
						if (property.GetCustomAttributes<XmlArrayItemAttribute>() is var arrayItemAttributes && arrayItemAttributes.Any())
						{
							// map array with multiple item types
							var ctors = arrayItemAttributes.ToDictionary(
								x => x.ElementName,
								x => x.Type.GetConstructor(Type.EmptyTypes) != null
									? (Func<object>)(() => Activator.CreateInstance(x.Type))
									: throw new MissingMethodException($"No parameterless ctor defined for {x.Type}"),
								StringComparer.InvariantCultureIgnoreCase);

							var array = child.Elements()
								.Select(x => ctors.TryGetValue(x.Name.LocalName, out var ctor)
									? x.PopulateTo(ctor())
									: throw new Exception($"Cannot parse XElement '{x.Name.LocalName}' into {elementType} (property: {property.Name})"))
								.ToTypeArray(elementType);

							property.SetValue(instance, array);
						}
						else if (elementType.GetConstructor(Type.EmptyTypes) != null)
						{
							// map array with single item type
							var array = child.Elements()
								.Select(x => x.PopulateTo(Activator.CreateInstance(elementType)))
								.ToTypeArray(elementType);

							property.SetValue(instance, array);
						}
						else
						{
							throw new MissingMethodException($"No parameterless ctor defined for {elementType}");
						}
					}
					else if (property.GetCustomAttribute<XmlElementAttribute>() != null)
					{
						if (property.PropertyType.GetConstructor(Type.EmptyTypes) != null)
						{
							// map complex type
							var innerInstance = Activator.CreateInstance(property.PropertyType);
							child.PopulateTo(innerInstance);

							property.SetValue(instance, innerInstance);
						}
						else
						{
							throw new MissingMethodException($"No parameterless ctor defined for {property.PropertyType}");
						}
					}
					else if (TryParseValue(property.PropertyType, child.Value, throwException: true) is var result && result.Success == true)
					{
						// map simple type
						property.SetValue(instance, result.Value);
					}
					else
					{
						_logger.Value.Error($"Unknown property type: {property.PropertyType} <- {child.Name.LocalName}");
					}
				}
				else if (directArrayProperties.FirstOrDefault(x => x.Constructors.ContainsKey(child.Name.LocalName)) is var directArrayProperty && directArrayProperty != null)
				{
					// add DAP item into temporary buffer
					var innerInstance = directArrayProperty.Constructors[child.Name.LocalName]();
					child.PopulateTo(innerInstance);
					directArrayProperty.Buffer.Add(innerInstance);
				}
				else
				{
					_logger.Value.Error($"Unmapped property: {child.Name.LocalName}");
				}
			}
			foreach (var directArrayProperty in directArrayProperties)
			{
				// map DAP buffer to property
				if (directArrayProperty.Buffer.Any())
				{
					var array = directArrayProperty.Buffer.ToTypeArray(directArrayProperty.ElementType);

					directArrayProperty.Property.SetValue(instance, array);
				}
			}

			if (properties.FirstOrDefault(x => x.PropertyType == typeof(string) && x.GetCustomAttribute<XmlTextAttribute>() != null) is PropertyInfo rawContentProperty)
			{
				// map direct content
				rawContentProperty.SetValue(instance, element.Value);
			}

			return instance;
		}


		/// <summary>
		/// Try parse value into the target type
		/// </summary>
		/// <param name="type">target type</param>
		/// <param name="value">value to parse</param>
		/// <param name="throwException">True to throw exception on format exception, false to return exception as Value of result</param>
		/// <returns>True if successful, false if failed to parse, and null for unknown type</returns>
		private static (bool? Success, object Value) TryParseValue(Type type, string value, bool throwException = false)
		{
			if (type == typeof(string))
			{
				return (true, value);
			}
			else if (type == typeof(bool))
			{
				return bool.TryParse(value, out var result)
					? (true, result)
					: MaybeThrow();
			}
			else if (type == typeof(Uri))
			{
				return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var result)
					? (true, result)
					: MaybeThrow();
			}

			return default;

			(bool? Success, object Result) MaybeThrow(Exception e = null) => throwException
				? throw e ?? new FormatException($"Unable to parse {type} from: {value}")
				: (false, e ?? new FormatException($"Unable to parse {type} from: {value}"));
		}

		private static TAttribute[] GetCustomAttributes<TAttribute>(this PropertyInfo property) where TAttribute : Attribute
		{
			return property.GetCustomAttributes(typeof(TAttribute), false)
				.Cast<TAttribute>()
				.ToArray();
		}

		private static Array ToTypeArray(this IEnumerable<object> source, Type type)
		{
			var collection = source as ICollection ?? source.ToArray();
			var buffer = Array.CreateInstance(type, collection.Count);
			collection.CopyTo(buffer, 0);

			return buffer;
		}
	}
}
