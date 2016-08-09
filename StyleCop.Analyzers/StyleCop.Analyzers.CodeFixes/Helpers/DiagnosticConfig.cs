// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.Helpers
{
    using System.Collections.Generic;

    /// <summary>
    /// [UGLY]: Use this class for configuring analyzer, until better solution be introduced
    /// </summary>
    public static class DiagnosticConfig
    {
        /// <summary>
        /// Gets abbreveatures to skip
        /// </summary>
        public static HashSet<string> AbbreviationsToSkip { get; }

        static DiagnosticConfig()
        {
            AbbreviationsToSkip = new HashSet<string>
                {
                    "AX", "DTO"
                };
        }
    }
}
