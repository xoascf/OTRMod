/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Animation : Resource {
	public enum AnimationType {
		Normal = 0,
		Link = 1,
		Curve = 2,
		Legacy = 3,
	}

	internal static class PlayerAnimation {
		public static byte[] Export(byte[] input) {
			int aniSize = input.Length;

			byte[] data = new byte[HeaderSize + 4 + aniSize];

			data.Set(0, GetHeader(ResourceType.PlayerAnimation));
			data.Set(HeaderSize, BitConverter.GetBytes(aniSize / 2));
			data.Set(HeaderSize + 4, input.CopyAs(ByteOrder.ByteSwapped, l: aniSize));

			return data;
		}
	}

	public static byte[] ParseAnimation(byte[] input, AnimationType type) {
		int aniSize = input.Length;

		byte[] data = new byte[HeaderSize + 12 + aniSize];

		data.Set(0, GetHeader(ResourceType.Animation));
		data.Set(HeaderSize, AnimationType.Normal); // FIXME: Autodetect!!
		data.Set(HeaderSize + 4, AnimationType.Normal); // FRAMECOUNT
		data.Set(HeaderSize + 10, input.CopyAs(ByteOrder.ByteSwapped, l: aniSize));

		return data;
	}


	public static byte[] Export(byte[] input) {
		int aniSize = input.Length;

		byte[] data = new byte[HeaderSize + 12 + aniSize];

		data.Set(0, GetHeader(ResourceType.Animation));
		data.Set(HeaderSize, AnimationType.Normal); // FIXME: Autodetect!!
		data.Set(HeaderSize + 4, AnimationType.Normal); // FRAMECOUNT
		data.Set(HeaderSize + 10, input.CopyAs(ByteOrder.ByteSwapped, l: aniSize));

		return data;
	}
}
