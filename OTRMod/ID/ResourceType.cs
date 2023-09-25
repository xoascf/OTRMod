namespace OTRMod.ID;

/* Sources:
 * Retro - resource_type.dart
 * LUS - ResourceType.h
 */

public enum ResourceType {
	Unknown = -1,

	/* Common */
	Archive     = 0x4F415243, /* OARC */
	DisplayList = 0x4F444C54, /* ODLT */
	Vertex      = 0x4F565458, /* OVTX */
	Matrix      = 0x4F4D5458, /* OMTX */
	Array       = 0x4F415252, /* OARR */
	Blob        = 0x4F424C42, /* OBLB */
	Texture     = 0x4F544558, /* OTEX */

	/* Ship */
	Animation       = 0x4F414E4D, /* OANM */
	PlayerAnimation = 0x4F50414D, /* OPAM */
	Room            = 0x4F524F4D, /* OROM */
	CollisionHeader = 0x4F434F4C, /* OCOL */
	Skeleton        = 0x4F534B4C, /* OSKL */
	SkeletonLimb    = 0x4F534C42, /* OSLB */
	Path            = 0x4F505448, /* OPTH */
	Cutscene        = 0x4F435654, /* OCVT */
	Text            = 0x4F545854, /* OTXT */
	Audio           = 0x4F415544, /* OAUD */
	AudioSample     = 0x4F534D50, /* OSMP */
	AudioSoundFont  = 0x4F534654, /* OSFT */
	AudioSequence   = 0x4F534551, /* OSEQ */
	Background      = 0x4F424749, /* OBGI */
	SceneCommand    = 0x4F52434D, /* ORCM */
}