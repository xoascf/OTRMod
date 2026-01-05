/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Animation : Resource {
	public byte[] AnimationData { get; set; }
	public int Offset { get; set; }

	public Animation(byte[] aniData, int offset) : base(ResourceType.Animation) {
		AnimationData = aniData;
		Offset = offset;
	}

	public enum AnimationType {
		Normal = 0,
		Link = 1,
		Curve = 2,
		Legacy = 3,
	}

	private class AnimationHeader {
		public short FrameCount;
#pragma warning disable CS8618 // Should we use "required" members soon?
		public byte[] FrameData;
		public byte[] JointIndices;
#pragma warning restore CS8618
		public ushort StaticIndexMax;
	}

	private static void GetAnimationHeader
		(byte[] data, int offset, out AnimationHeader header) {
		int framePos = data.ToI16(offset + 6);
		int jointPos = data.ToI16(offset + 10);

		header = new AnimationHeader {
			FrameCount = data[offset + 1],
			FrameData = data.Get(framePos, jointPos - framePos),
			JointIndices = data.Get(jointPos, offset - jointPos - 2),
			StaticIndexMax = data[offset + 13]
		};
	}

	private static byte[] GetAnimationData(AnimationHeader header) {
		List<byte> bytes = new();
		bytes.AddRange(ByteArray.FromI32((int)AnimationType.Normal, false));
		bytes.AddRange(ByteArray.FromI16(header.FrameCount, false));
		bytes.AddRange(ByteArray.FromI32(header.FrameData.Length / 2, false));
		bytes.AddRange(Misc.SwapByteArray(header.FrameData));
		bytes.AddRange(ByteArray.FromI32(header.JointIndices.Length / 6, false));
		bytes.AddRange(Misc.SwapByteArray(header.JointIndices));
		bytes.AddRange(ByteArray.FromU16(header.StaticIndexMax, false));

		return bytes.ToArray();
	}

	public override byte[] Formatted() {
		GetAnimationHeader(AnimationData, Offset, out AnimationHeader animHeader);

		Data = GetAnimationData(animHeader);

		return base.Formatted();
	}
}