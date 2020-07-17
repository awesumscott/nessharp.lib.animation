using NESSharp.Core;
using System.Collections.Generic;

namespace NESSharp.Lib.Animation.Models {
	public class Animation : IWritable {
		public U8 Offset { get; set; }
		public List<Frame> Frames { get; set; }
		public List<State> States { get; set; }
		public void Write() {}
	}
}
