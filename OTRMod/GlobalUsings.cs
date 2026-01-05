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

global using Color = System.Drawing.Color;
global using Codec = OTRMod.ID.Texture.Codec;
#if NET5_0_OR_GREATER
global using Bitmap = IronSoftware.Drawing.AnyBitmap;
#else
global using Bitmap = System.Drawing.Bitmap;
#endif