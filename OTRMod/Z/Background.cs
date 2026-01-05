/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Background : Resource {
	public byte[] JpegData { get; set; }
	private const int Width = 320;
	private const int Height = 240;
	private const int RawDataSize = (Width * Height) * 2;

	private const ushort MARKER_EOI = 0xFFD9;

	public Background(byte[] jpegData) : base(ResourceType.Background) {
		JpegData = jpegData;
	}

	public static Background LoadFrom(Resource res) {
		if (res.Data == null || res.Data.Length < 4)
			throw new ArgumentException("Resource data cannot be null or too short.", nameof(res));

		// Background format: first 4 bytes = texDataSize (little-endian), then JPEG data.
		int texDataSize = res.Data.ToI32(0, false);
		int jpegOffset = 4;
		int jpegLength = Math.Min(texDataSize, res.Data.Length - jpegOffset);

		byte[] jpegData = new byte[jpegLength];
		Array.Copy(res.Data, jpegOffset, jpegData, 0, jpegLength);

		return new Background(jpegData) {
			Version = res.Version,
			IsModded = res.IsModded,
		};
	}

	public override byte[] Formatted() {
		// Ensure payload is padded to the expected raw data size for OTR background
		// containers (u16 matrix: width * height * 2).
		AddPaddingIfNeeded();

		byte[] jpegBytes = JpegData ?? new byte[0];

		// Background format: [4-byte texDataSize (little-endian)][jpeg data]
		byte[] resourceData = new byte[4 + jpegBytes.Length];
		ByteArray.FromI32(jpegBytes.Length, Big).CopyTo(resourceData, 0);
		jpegBytes.CopyTo(resourceData, 4);

		Data = resourceData;
		return base.Formatted();
	}

	/// <summary>
	/// Return the JPEG bytes trimmed at the EOI marker if present.
	/// Otherwise returns the raw payload.
	/// </summary>
	public byte[] GetJpegTrimmed() {
		if (JpegData == null || JpegData.Length < 2)
			return new byte[0];

		for (int i = 0; i < JpegData.Length - 1; i++) {
			if (JpegData.ToU16(i) == MARKER_EOI) {
				// Include marker bytes.
				int length = i + sizeof(ushort);
				byte[] output = new byte[length];
				Array.Copy(JpegData, 0, output, 0, length);
				return output;
			}
		}

		return JpegData;
	}

	private void AddPaddingIfNeeded() {
		if (JpegData == null) {
			JpegData = new byte[0];
			return;
		}

		if (JpegData.Length < RawDataSize) {
			byte[] padded = new byte[RawDataSize];
			Array.Copy(JpegData, 0, padded, 0, JpegData.Length);
			// Remaining bytes are 0x00 by default...
			JpegData = padded;
		}
		// Too large!? Leave as is. Trust me.
	}
}