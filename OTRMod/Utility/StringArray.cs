/* Licensed under the Open Software License version 3.0 */
// Thanks Internet.

namespace OTRMod.Utility;

internal static class StringArray
{
	// I just don't like to use LINQ, so here's this.
	// Courtesy of https://stackoverflow.com/a/4423303
	public static string[] Skip(this string[] array, int index)
	{
		string[] newArray = new string[array.Length - 1];

		int i = 0; int j = 0;
		while (i < array.Length)
		{
			if (i != index)
			{
				newArray[j] = array[i];
				j++;
			}

			i++;
		}

		return newArray;
	}
}