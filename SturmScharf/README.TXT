===========
SturmScharf
===========

SturmScharf is a .NET library for working with MPQ/OTR files based on War3Net.IO.Mpq.

Main changes:
- Renamed to "Sturm" (in honor of Storm.dll) + "Sharp" (after C#) in German.
- Dependencies on LINQ removed.
- Updated to work with .NET Framework 4 Client Profile up to .NET 6.0.
- Standardized line endings to CRLF in "(listfile)" for cross-platform consistency.
- Reworked code style and removed StyleCop rules.
- Tests are heretical so they were dropped.
- Game public keys are not required for OTR handling, so they have also been removed.
- "Unreadable" streams don't close early.
- Obsolete functions removed.
- Added MD5 hashing support.
- Fixed file time support.
- Writes and reads the internal file "(listfile)" as Latin-1.

License:
SturmScharf is released under the MIT License. See the LICENSE file for details.