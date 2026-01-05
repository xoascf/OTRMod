/* TODO: This is a dirty adaptation of the ZAPDTR's ZBackground.cpp, needs cleanup! */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Background : Resource {
	// Raw jpeg/obgi payload stored in the resource Data field.
	public byte[] JpegData { get; set; }
	// Default background screen size used for padding when none configured.
	private const int DefaultBgWidth = 320;
	private const int DefaultBgHeight = 240;

	private const uint JPEG_MARKER = 0xFFD8FFE0;
	private const ushort MARKER_DQT = 0xFFDB;
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
	/// Return the JPEG bytes trimmed at the EOI marker (0xFFD9) if present.
	/// Otherwise returns the raw payload.
	/// </summary>
	public byte[] GetJpegTrimmed() {
		if (JpegData == null || JpegData.Length < 2)
			return new byte[0];

		for (int i = 0; i < JpegData.Length - 1; i++) {
			if (JpegData[i] == 0xFF && JpegData[i + 1] == 0xD9) {
				// include marker bytes
				int len = i + 2;
				byte[] outb = new byte[len];
				Array.Copy(JpegData, 0, outb, 0, len);
				return outb;
			}
		}

		return JpegData;
	}

	private int GetRawDataSize() {
		return DefaultBgWidth * DefaultBgHeight * 2;
	}

	private void AddPaddingIfNeeded() {
		if (JpegData == null) {
			JpegData = new byte[0];
			return;
		}

		int rawSize = GetRawDataSize();
		if (JpegData.Length < rawSize) {
			byte[] padded = new byte[rawSize];
			Array.Copy(JpegData, 0, padded, 0, JpegData.Length);
			// remaining bytes are 0x00 by default
			JpegData = padded;
		}
		else if (JpegData.Length > rawSize) {
			// Leave larger images intact, but log a warning.
			Debug.WriteLine($"Warning: background jpeg ({JpegData.Length} bytes) is larger than screen buffer ({rawSize} bytes).");
		}

		// Basic validation (best-effort) on JPEG header
		try {
			CheckValidJpeg();
		}
		catch (Exception ex) {
			Debug.WriteLine($"Warning: CheckValidJpeg failed: {ex.Message}");
		}
	}

	private void CheckValidJpeg() {
		if (JpegData == null || JpegData.Length < 12)
			return;

		uint marker = JpegData.ToU32(0);
		if (marker != JPEG_MARKER) {
			Debug.WriteLine("Warning: missing JPEG marker at beginning of background data.");
		}

		// Check for 'JFIF\0' at offset 6
		if (!(JpegData[6] == (byte)'J' && JpegData[7] == (byte)'F' && JpegData[8] == (byte)'I' && JpegData[9] == (byte)'F' && JpegData[10] == 0x00)) {
			// Not fatal, but warn
			Debug.WriteLine("Warning: missing 'JFIF' identifier in background jpeg.");
		}

		byte majorVersion = JpegData[11];
		byte minorVersion = JpegData[12];
		if (majorVersion != 0x01 || minorVersion != 0x01) {
			Debug.WriteLine($"Warning: unexpected JFIF version {majorVersion}.{minorVersion:00} in background jpeg.");
		}

		if (JpegData.Length >= 22) {
			if (JpegData.ToU16(20) != MARKER_DQT) {
				Debug.WriteLine("Warning: extra data before image data (DQT marker not found at expected offset).");
			}
		}
	}
}