global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using StringsDict = System.Collections.Generic.Dictionary<string, string>;

global using OTRMod.ID;
global using static OTRMod.Utility.IO;
global using ByteOrder = OTRMod.ID.ByteOrder.Type;
#if NETCOREAPP3_0_OR_GREATER
global using BitOperations = System.Numerics.BitOperations;
#else
global using BitOperations = SturmScharf.Compat.BitOperations;
#endif
