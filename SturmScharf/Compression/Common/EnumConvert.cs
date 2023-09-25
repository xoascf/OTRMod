using System.Globalization;

namespace SturmScharf.Compression.Common;

public static class EnumConvert<TEnum>
	where TEnum : struct, Enum {
	public static TEnum FromByte(byte value, bool allowNoFlags = true) {
		TEnum result = (TEnum)(object)value;
		if (!result.IsDefined(allowNoFlags)) {
			string displayValue = Attribute.GetCustomAttribute(typeof(TEnum), typeof(FlagsAttribute)) is null
				? value.ToString(CultureInfo.InvariantCulture)
				: $"0b{Convert.ToString(value, 2).PadLeft(8, '0')}";

			throw new ArgumentException(
				$"Value '{displayValue}' is not defined for enum of type {typeof(TEnum).Name}.");
		}

		return result;
	}

	public static TEnum FromInt32(int value, bool allowNoFlags = true) {
		TEnum result = (TEnum)(object)value;
		if (!result.IsDefined(allowNoFlags)) {
			string displayValue = Attribute.GetCustomAttribute(typeof(TEnum), typeof(FlagsAttribute)) is null
				? value.ToString(CultureInfo.InvariantCulture)
				: $"0b{Convert.ToString(value, 2).PadLeft(32, '0')}";

			throw new ArgumentException(
				$"Value '{displayValue}' is not defined for enum of type {typeof(TEnum).Name}.");
		}

		return result;
	}

	public static TEnum FromChar(char value) {
		TEnum result = (TEnum)(object)value;
		if (!result.IsDefined())
			throw new ArgumentException($"Value '{value}' is not defined for enum of type {typeof(TEnum).Name}.");

		return result;
	}
}