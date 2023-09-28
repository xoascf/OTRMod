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

	private class AnimationHeader {
		public short frameCount;
#pragma warning disable CS8618 // Should we use "required" members soon?
		public byte[] frameData;
		public byte[] jointIndices;
#pragma warning restore CS8618
		public ushort indexMax;
	}

	private static void GetAnimationHeader
		(byte[] data, int offset, out AnimationHeader header) {
		int framePos = data.ToI16(offset + 6);
		int jointPos = data.ToI16(offset + 10);

		header = new AnimationHeader {
			frameCount = data[offset + 1],
			frameData = data.Get(framePos, jointPos - framePos),
			jointIndices = data.Get(jointPos, offset - jointPos - 2),
			indexMax = data[offset + 13]
		};
	}

	private static byte[] GetAnimationData(AnimationHeader header) {
		List<byte> bytes = new();
		bytes.AddRange(GetHeader(ResourceType.Animation));
		bytes.AddRange(ByteArray.FromI32((int)AnimationType.Normal, false));
		bytes.AddRange(ByteArray.FromI16(header.frameCount, false));
		bytes.AddRange(ByteArray.FromI32(header.frameData.Length / 2, false));
		bytes.AddRange(Misc.SwapByteArray(header.frameData));
		bytes.AddRange(new byte[] { 0x2F, 0x00, 0x00, 0x00 }); // Separator??
		bytes.AddRange(Misc.SwapByteArray(header.jointIndices));
		bytes.AddRange(ByteArray.FromU16(header.indexMax, false));

		return bytes.ToArray();
	}

	public static byte[] Export(byte[] input, int offset) {
		GetAnimationHeader(input, offset, out AnimationHeader animHeader);
		return GetAnimationData(animHeader);
	}
}
