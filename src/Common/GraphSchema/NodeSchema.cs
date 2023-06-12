﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Collections.Generic;

namespace openCypherTranspiler.Common.GraphSchema
{
    public class NodeSchema : EntitySchema
    {
        public override string Id
        {
            get
            {
                return Name;
            }
        }

        public List<EntityProperty> NodeIdProperties { get; set; }
        //20230612-VM-Was
        //public EntityProperty NodeIdProperty { get; set; }
    }
}

