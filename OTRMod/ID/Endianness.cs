namespace OTRMod.ID;

public enum Endianness {
	Unknown = -1,

	/* ROM and OTR data byte order */
	BigEndian,    /* Z64 */
	LittleEndian, /* N64 */
	ByteSwapped,  /* V64 */
	WordSwapped,  /* U64 */
}