using NESSharp.Core;
using System.Linq;

namespace NESSharp.Lib.Animation.Models;

public class SObject : IWritable {
	public U8 Y { get; set; }
	public U8 Tile { get; set; }
	public U8 Attr { get; set; }
	public U8 X { get; set; }
	public U8[] Palettes { get; set; }
	
	public SObject() {}
	public SObject(U8 y, U8 tile, U8 attr, U8 x) {
		X = x; Y = y; Tile = tile; Attr = attr;
	}

	private U8 _getPalettesCompressed() {
		byte b = 0;
		var first = true;
		foreach (var p in Palettes.Reverse()) {
			if (!first)
				b = (byte)(b << 2);
			b |= (byte)p;
			first = false;
		}
		return b;
	}

	public void Write() => AL.Raw(Y, Tile, Attr, _getPalettesCompressed(), X);
}
