namespace OTRMod.Web;

public enum GenerationStatus {
	NoSelected, /* None is selected */
	Selected,   /* At least one is selected */
	Loading,    /* Loading files */
	Generating, /* Processing (decompressing/creating OTR) */
	Finished,   /* Then we're done */
	Error,      /* F */
}