﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using openCypherTranspiler.Common.GraphSchema;
using openCypherTranspiler.Common.Exceptions;
using openCypherTranspiler.openCypherParser.AST;

namespace openCypherTranspiler.LogicalPlanner
{
    /// <summary>
    /// Starting operator representing an entity data source of the graph
    /// </summary>
    public sealed class DataSourceOperator : StartLogicalOperator, IBindable
    {
        public DataSourceOperator(Entity entity)
        {
            Entity = entity;
        }

        public Entity Entity { get; set; }

        /// <summary>
        /// This function binds the data source to a given graph definitions
        /// </summary>
        /// <param name="graphDefinition"></param>
        public void Bind(IGraphSchemaProvider graphDefinition)
        {
            // During binding, we read graph definition of the entity
            // and populate the EntityField object in the output
            // with the list of fields that the node/edge definition can expose
            var properties = new List<ValueField>();
            string entityUniqueName;
            string sourceEntityName = null;
            string sinkEntityName = null;
            List<ValueField> nodeIdFields = new List<ValueField>(); //20230612-VM-Was = null;
            List<ValueField> edgeSrcFields = new List<ValueField>(); //20230611-VM-Was = null;
            List<ValueField> edgeSinkFields = new List<ValueField>(); //20230611-VM-Was = null;

            if (Entity is NodeEntity)
            {
                var nodeDef = graphDefinition.GetNodeDefinition(Entity.EntityName);

                if (nodeDef == null)
                {
                    throw new TranspilerBindingException($"Failed to bind entity with alias '{Entity.Alias}' of type '{Entity.EntityName}' to graph definition.");
                }

                entityUniqueName = nodeDef.Id;

                nodeIdFields = nodeDef.NodeIdProperties.Select(property => new ValueField(property.PropertyName, property.DataType)).ToList();

                //20240504-VM-Added (Need fields on the Source side of a ForeignKeyRelationship.
                edgeSrcFields = nodeDef.Properties
                                        .Where(property => property.PropertyType == EntityProperty.PropertyDefinitionType.SourceNodeJoinKey)
                                        .Select(property => new ValueField(property.PropertyName, property.DataType)).ToList();

                //20230612-VM-Was...
                //nodeIdField = new ValueField(nodeDef.NodeIdProperty.PropertyName, nodeDef.NodeIdProperty.DataType);

                properties.AddRange(nodeDef.Properties.Select(p => new ValueField(p.PropertyName, p.DataType)));
                properties.AddRange(nodeIdFields);
            }
            else
            {
                var edgeEnt = Entity as RelationshipEntity;
                EdgeSchema edgeDef = null;

                switch (edgeEnt.RelationshipDirection)
                {
                    case RelationshipEntity.Direction.Forward:
                        edgeDef = graphDefinition.GetEdgeDefinition(edgeEnt.EntityName, edgeEnt.LeftEntityName, edgeEnt.RightEntityName);
                        break;
                    case RelationshipEntity.Direction.Backward:
                        edgeDef = graphDefinition.GetEdgeDefinition(edgeEnt.EntityName, edgeEnt.RightEntityName, edgeEnt.LeftEntityName);
                        break;
                    default:
                        // either direction
                        // TODO: we don't handle 'both' direction yet
                        Debug.Assert(edgeEnt.RelationshipDirection == RelationshipEntity.Direction.Both);
                        edgeDef = graphDefinition.GetEdgeDefinition(edgeEnt.EntityName, edgeEnt.LeftEntityName, edgeEnt.RightEntityName);
                        if (edgeDef == null)
                        {
                            edgeDef = graphDefinition.GetEdgeDefinition(edgeEnt.EntityName, edgeEnt.RightEntityName, edgeEnt.LeftEntityName);
                        }
                        break;
                }

                if (edgeDef == null)
                {
                    throw new TranspilerBindingException($"Failed to bind entity with alias '{Entity.Alias}' of type '{Entity.EntityName}' to graph definition.");
                }

                entityUniqueName = edgeDef.Id;
                sourceEntityName = edgeDef.SourceNodeId;
                sinkEntityName = edgeDef.SinkNodeId;
                edgeSrcFields.AddRange(edgeDef.SourceProperties.Select(p => new ValueField(p.PropertyName, p.DataType)));
                edgeSinkFields.AddRange(edgeDef.SinkProperties.Select(p => new ValueField(p.PropertyName, p.DataType)));

                properties.AddRange(edgeDef.Properties.Select(p => new ValueField(p.PropertyName, p.DataType)));
                properties.AddRange(edgeSrcFields);
                properties.AddRange(edgeSinkFields);
            }

            Debug.Assert(OutputSchema.Count == 1
                && OutputSchema.First() is EntityField
                && (OutputSchema.First() as EntityField).EntityName == Entity.EntityName);

            var field = OutputSchema.First() as EntityField;
            field.BoundEntityName = entityUniqueName;
            field.BoundSourceEntityName = sourceEntityName;
            field.BoundSinkEntityName = sinkEntityName;
            field.EncapsulatedFields = properties;
            field.NodeJoinFields = nodeIdFields;
            field.RelSourceJoinFields = edgeSrcFields;
            field.RelSinkJoinFields = edgeSinkFields;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(base.ToString());
            sb.AppendLine($"DataEntitySource: {Entity};");
            return sb.ToString();
        }
    }
}
