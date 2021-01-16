﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace TyeExplorer.Tye.Models
{
    public class V1ServiceDescription
    {
        public string Name { get; set; }
        public int Replicas { get; set; }
        public V1RunInfo RunInfo { get; set; }
        public List<V1ServiceBinding> Bindings { get; set; }
        public List<V1ConfigurationSource> Configuration { get; set; }
    }
}
