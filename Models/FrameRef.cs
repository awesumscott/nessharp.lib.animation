using NESSharp.Core;

namespace NESSharp.Lib.Animation.Models {
	public class FrameRef : IWritable {
		public U8 Id { get; set; }
		public U8 Start { get; set; }
		public void Write() {}
	}
}
