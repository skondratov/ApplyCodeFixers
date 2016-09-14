﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Settings.ObjectModel
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    internal class AbbreviationSettings
    {
        /// <summary>
        /// This is the backing field for the <see cref="AbbreviationsToSkip"/> property.
        /// </summary>
        [JsonProperty("abbreviationsToSkip", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private HashSet<string> abbreviationsToSkip;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbbreviationSettings"/> class during JSON deserialization.
        /// </summary>
        [JsonConstructor]
        protected internal AbbreviationSettings()
        {
            this.abbreviationsToSkip = new HashSet<string>();
        }

        public HashSet<string> AbbreviationsToSkip => this.abbreviationsToSkip;
    }
}
