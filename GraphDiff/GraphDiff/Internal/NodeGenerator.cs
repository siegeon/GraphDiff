using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Reflection;
using RefactorThis.GraphDiff.Internal.Graph;

namespace RefactorThis.GraphDiff.Internal
{
	/// <summary>
	///    Walks an entity graph and produces an GraphNode graph.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class NodeGenerator<T>
	{
		private readonly List<int> _visitedEntities = new List<int>();
		private GraphNode _currentMember;
		private string _relationShip = "";

		public GraphNode GetNodes(DbContext context, T entity)
		{
			var initialNode = new GraphNode();
			_currentMember = initialNode;
			_visitedEntities.Add(entity.GetHashCode());
			WalkTree(context, entity);


			return initialNode;
		}

		private void WalkTree<TEntity>(DbContext context, TEntity entity)
		{
			var navprops = context.GetNavigationPropertiesForType(typeof (TEntity));
			foreach (NavigationProperty navigationProperty in context.GetNavigationPropertiesForType(typeof (TEntity)))
			{
				PropertyInfo entityProperty = entity.GetType().GetProperties().First(pi => navigationProperty.Name == pi.Name);
				dynamic value = entityProperty.GetValue(entity, null);
				if (value == null) continue;

				if(_visitedEntities.Contains(entityProperty.GetHashCode())) continue;
				_visitedEntities.Add(entityProperty.GetHashCode());


				DetermineRelationShip(navigationProperty);
				GraphNode newMember = CreateNewMember(entityProperty);
				if (newMember == null) continue;
			
				_currentMember.Members.Push(newMember);
				_currentMember = newMember;


				if (entityProperty.PropertyType.IsCollectionType(typeof(IEnumerable<>)))
				{
					foreach (var collectionEntity in value)
					{
						WalkTree(context, collectionEntity);
					}
				}
				else
				{
					WalkTree(context, value);
				}

				_currentMember = _currentMember.Parent;
			}
		}

    	private GraphNode CreateNewMember(PropertyInfo accessor)
		{
			GraphNode newMember;
			switch (_relationShip)
			{
				case "OwnedEntity":
					newMember = new OwnedEntityGraphNode(_currentMember, accessor);
					break;
				case "AssociatedEntity":
					newMember = new AssociatedEntityGraphNode(_currentMember, accessor);
					break;
				case "OwnedCollection":
					newMember = new CollectionGraphNode(_currentMember, accessor, true);
					break;
				case "AssociatedCollection":
					newMember = new CollectionGraphNode(_currentMember, accessor, false);
					break;
				default:
					newMember = null;
					break;
			}
			return newMember;
		}

		private void DetermineRelationShip(NavigationProperty property)
		{
			switch (property.FromEndMember.RelationshipMultiplicity)
			{
				case RelationshipMultiplicity.ZeroOrOne:
					switch (property.ToEndMember.RelationshipMultiplicity)
					{
						case RelationshipMultiplicity.ZeroOrOne:
							_relationShip = "AssociatedEntity";
							break;
						case RelationshipMultiplicity.One:
							_relationShip = "Skip";
							break;
						case RelationshipMultiplicity.Many:
							_relationShip = "AssociatedCollection";
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				case RelationshipMultiplicity.One:
					switch (property.ToEndMember.RelationshipMultiplicity)
					{
						case RelationshipMultiplicity.ZeroOrOne:
							_relationShip = "OwnedEntity";
							break;
						case RelationshipMultiplicity.One:
							_relationShip = "Skip";
							break;
						case RelationshipMultiplicity.Many:
							_relationShip = "OwnedCollection";
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				case RelationshipMultiplicity.Many:
					switch (property.ToEndMember.RelationshipMultiplicity)
					{
						case RelationshipMultiplicity.ZeroOrOne:
							_relationShip = "Skip";
							break;
						case RelationshipMultiplicity.One:
							_relationShip = "Skip";
							break;
						case RelationshipMultiplicity.Many:
							_relationShip = "Skip";
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}