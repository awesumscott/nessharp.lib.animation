using NESSharp.Core;
using System;

namespace NESSharp.Lib.Animation {
	public class AnimationData : Struct {
		private static readonly U8 HORIZ_FLIP = 0b01000000;
		private static readonly U8 HORIZ_FLIP_MASK = 0b10111111;
		private VByte _state;
		public VByte State { get; set; }
		public VByte Counter { get; set; }
		public VByte X { get; set; }
		public VByte Y { get; set; }
		public VByte Palette { get; set; } //index into palette data
		/// <summary>
		/// Attributes that apply to every object of every sprite frame. These may not map directly to objects, but they
		///	are laid out in the same format to maybe provide some crossover to OAM manipulation.
		/// </summary>
		public VByte Attr { get; set; }
		public void ChangeState(U8 newState) {
			State.Set(newState);
			Counter.Set(0);
		}
		public void SetHorizontalFlip(bool x) {
			if (x)
				Attr.Set(z => z.Or(HORIZ_FLIP));
			else
				Attr.Set(z => z.And(HORIZ_FLIP_MASK));
		}
		public void ToggleHorizontalFlip() {
			Attr.Set(z => z.Xor(HORIZ_FLIP));
		}
	}
}