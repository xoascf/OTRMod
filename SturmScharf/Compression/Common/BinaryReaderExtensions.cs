namespace SturmScharf.Compression.Common;

public static class EnumExtensions {
	/// <param name="allowNoFlags">
	/// If <see langword="true" />, an integral value of zero will be considered valid for
	/// <paramref name="enum" />, assuming <typeparamref name="TEnum" /> has <see cref="FlagsAttribute" />.
	/// </param>
	public static bool IsDefined<TEnum>(this TEnum @enum, bool allowNoFlags = true)
		where TEnum : struct, Enum {
		if (Enum.IsDefined(typeof(TEnum), @enum))
			return true;

		if (Attribute.GetCustomAttribute(typeof(TEnum), typeof(FlagsAttribute)) is null)
			return false;

		if (allowNoFlags && (int)(object)@enum == 0)
			return true;

		char firstChar = @enum.ToString()[0];
		return !char.IsDigit(firstChar) && firstChar != '-';
	}
}