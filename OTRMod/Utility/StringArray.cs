namespace OTRMod.Utility;

internal static class StringArray {
	/* We don't use LINQ, so here's this.  /
	/ https://stackoverflow.com/a/4423303 */
	public static string[] Skip(this string[] array, int index) {
		string[] newArray = new string[array.Length - 1];

		int i = 0; int j = 0;
		while (i < array.Length) {
			if (i != index) {
				newArray[j] = array[i];
				j++;
			}

			i++;
		}

		return newArray;
	}

	public static string Join(this string[] array, string separator, int index) {
		string value = array[index];
		for (int i = index + 1; i < array.Length; i++)
			value += separator + array[i];

		return value;
	}
}