using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Schema;

namespace RefactorThis.GraphDiff.Internal
{
    internal static class Extensions
    {
        internal static IEnumerable<PropertyInfo> GetPrimaryKeyFieldsFor(this IObjectContextAdapter context, Type entityType)
        {
			EntityType metadata = context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .SingleOrDefault(p => p.FullName == entityType.FullName);

            if (metadata == null)
            {
                throw new InvalidOperationException(String.Format("The type {0} is not known to the DbContext.", entityType.FullName));
            }

			return
				metadata.KeyMembers.Select(
					k => entityType.GetProperty(k.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)).ToList();
		}

		public static List<PropertyInfo> GetMappingFor<T>(this IObjectContextAdapter context, T entity)
		{
			Expression<Func<IUpdateConfiguration<T>, dynamic>> mapping = null;

			Type entityType = typeof (T);

			foreach (NavigationProperty property in context.GetNavigationPropertiesForType(entityType))
			{
				var navigationProperty = entity.GetType().GetProperties().First(pi => property.Name == pi.Name);

				dynamic value = navigationProperty.GetValue(entity, null);
				if (value == null) continue;

				if (navigationProperty.PropertyType.IsCollectionType(typeof (ICollection<>)))
				{
					if (value.Count > 0)
					{
						DetermineRelationShip<T>(property);
					}
				}
				else
				{
					DetermineRelationShip<T>(property);
				}
			}

			var navigationProperties = context.GetNavigationPropertiesForType(entityType);
			var requiredNavigationProperties = context.GetRequiredNavigationPropertiesForType(entityType);

			//var CSMapping = (EntityContainerMapping) context.ObjectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace).First();
			//foreach (AssociationSetMapping associationSetMapping in CSMapping.AssociationSetMappings)
			//{
			//	// RefactorThis.GraphDiff.Tests.Models.TestNode
			//	string elementName = associationSetMapping.AssociationSet.ElementType.FullName;
			//	int i = 0;
			//}
			//IEnumerable<bool> entityMap = CSMapping.AssociationSetMappings.Select(mapping => mapping.AssociationSet.ElementType.FullName == entityType.FullName);


			return null;
		}

		private static void DetermineRelationShip<T>(NavigationProperty property)
		{
			switch (property.FromEndMember.RelationshipMultiplicity)
			{
				case RelationshipMultiplicity.ZeroOrOne:
					switch (property.ToEndMember.RelationshipMultiplicity)
					{
						case RelationshipMultiplicity.ZeroOrOne:
							break;
						case RelationshipMultiplicity.One:
							break;
						case RelationshipMultiplicity.Many:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				case RelationshipMultiplicity.One:
					switch (property.ToEndMember.RelationshipMultiplicity)
					{
						case RelationshipMultiplicity.ZeroOrOne:
							break;
						case RelationshipMultiplicity.One:
							break;
						case RelationshipMultiplicity.Many:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				case RelationshipMultiplicity.Many:
					switch (property.ToEndMember.RelationshipMultiplicity)
					{
						case RelationshipMultiplicity.ZeroOrOne:
							break;
						case RelationshipMultiplicity.One:
							break;
						case RelationshipMultiplicity.Many:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
        }

        internal static IEnumerable<NavigationProperty> GetRequiredNavigationPropertiesForType(this IObjectContextAdapter context, Type entityType)
        {
            return context.GetNavigationPropertiesForType(ObjectContext.GetObjectType(entityType))
                    .Where(navigationProperty => navigationProperty.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.One);
        }

        internal static IEnumerable<NavigationProperty> GetNavigationPropertiesForType(this IObjectContextAdapter context, Type entityType)
        {
            return context.ObjectContext.MetadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(p => p.FullName == entityType.FullName)
                    .NavigationProperties;
        }

		public static dynamic ToType<T>(this object @object, T destinationType)
		{
			var type = (object) destinationType as Type;
			if (type == null)
				throw new Exception(destinationType + " was unable to cast to type");
			if (type.AssemblyQualifiedName == null)
				throw new Exception(destinationType + " has no assembly qualified name");


			//Create a new instance of the given type
			object instance = Activator.CreateInstance(Type.GetType(type.AssemblyQualifiedName, true));

			foreach (PropertyInfo sourceProperty in @object.GetType().GetProperties())
			{
				try
				{
					PropertyInfo destPropert = instance.GetType().GetProperty(sourceProperty.Name);

					//dest prop and source prop are the same type (such as stings ints ect)
					if (destPropert.PropertyType == sourceProperty.PropertyType)
						destPropert.SetValue(instance, sourceProperty.GetValue(@object, null), null);

						//check if dest type is a collection
					else if (destPropert.PropertyType.IsCollectionType(typeof (ICollection<>)))
					{
						//cast the source prop in order to process its with extension method.
						var list = (IEnumerable) sourceProperty.GetValue(@object, null);

						if (sourceProperty.PropertyType.GetGenericArguments().Any())
						{
							object listValue = list.ToNonAnonymousList(destPropert.GetGenericArgumentType());

							destPropert.SetValue(instance, listValue, null);
						}
						else
						{
							var listValue = list.ToNonAnonymousList(destPropert.GetGenericArgumentType());
							destPropert.SetValue(instance, listValue, null);
						}
					}
					else
					{
						dynamic entity = sourceProperty.GetValue(@object, null).ToType(destPropert.PropertyType);
						destPropert.SetValue(instance, entity, null);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("Issue converting Dto " + @object.GetType().Name + " to " + instance.GetType().Name, ex);
				}
			}
			return instance;
		}

		#region To Type Helpers

		public static bool IsCollectionType(this Type type, Type collection)
		{
			return type.IsGenericType && collection.IsAssignableFrom(type.GetGenericTypeDefinition()) ||
					 type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == collection);
		}

		private static Type GetGenericArgumentType(this PropertyInfo pi)
		{
			return (pi.PropertyType.GetGenericArguments()[0]);
		}

		private static object ToNonAnonymousList(this IEnumerable list, Type t)
		{
			object listInstance = Activator.CreateInstance(typeof (List<>).MakeGenericType(new[] {t}));
			MethodInfo method = listInstance.GetType().GetMethod("Add");
			foreach (object obj in list)
				method.Invoke(listInstance, new object[]
				{
					obj.ToType(t)
				});
			return listInstance;
		  }
 
        internal static string GetEntitySetName(this IObjectContextAdapter context, Type entityType)
        {
            Type type = entityType;
            EntitySetBase set = null;

            while (set == null && type != null)
            {
                set = context.ObjectContext.MetadataWorkspace
                        .GetEntityContainer(context.ObjectContext.DefaultContainerName, DataSpace.CSpace)
                        .EntitySets
                        .FirstOrDefault(item => item.ElementType.Name.Equals(type.Name));

                type = type.BaseType;
            }

            return set != null ? set.Name : null;
        }
	 
		 #endregion
    }
}
