using NESSharp.Core;
using System.Collections.Generic;

namespace NESSharp.Lib.Animation.Models {
	public class State : IWritable {
		public U8 Id { get; set; }
		public string Name { get; set; }
		public U8 Length { get; set; }
		public bool Loop { get; set; }
		public U8 NextState { get; set; }
		public List<FrameRef> Frames { get; set; }
		public void Write() {}
	}
}
