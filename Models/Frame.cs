using NESSharp.Core;
using System.Collections.Generic;

namespace NESSharp.Lib.Animation.Models;

public class Frame : IWritable {
	public string Name { get; set; }
	public List<SObject> Tiles { get; set; }
	public void Write() {
		AL.Raw((U8)Tiles.Count);
		foreach(var sobj in Tiles)
			sobj.Write();
	}
}
