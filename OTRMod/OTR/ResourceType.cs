namespace OTRMod.OTR;

// Resource.h
public enum ResourceType
{
	Archive         = 0x4F415243, // OARC (UNUSED)
	Model           = 0x4F4D444C, // OMDL (WIP)
	Texture         = 0x4F544558, // OTEX
	Material        = 0x4F4D4154, // OMAT (WIP)
	Animation       = 0x4F414E4D, // OANM
	PlayerAnimation = 0x4F50414D, // OPAM
	DisplayList     = 0x4F444C54, // ODLT
	Room            = 0x4F524F4D, // OROM
	CollisionHeader = 0x4F434F4C, // OCOL
	Skeleton        = 0x4F534B4C, // OSKL
	SkeletonLimb    = 0x4F534C42, // OSLB
	Matrix          = 0x4F4D5458, // OMTX
	Path            = 0x4F505448, // OPTH
	Vertex          = 0x4F565458, // OVTX
	Cutscene        = 0x4F435654, // OCUT
	Array           = 0x4F415252, // OARR
	Text            = 0x4F545854, // OTXT
	Blob            = 0x4F424C42, // OBLB
	Audio           = 0x4F415544, // OAUD
	AudioSample     = 0x4F534D50, // OSMP
	AudioSoundFont  = 0x4F534654, // OSFT
	AudioSequence   = 0x4F534551, // OSEQ
}