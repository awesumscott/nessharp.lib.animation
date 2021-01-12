using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NESSharp.Lib.Animation {
	[VarSize(6)]
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
		public void ChangeState(U8 newState) => ChangeState((IOperand)newState);
		public void ChangeState(IOperand newState) {
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
		//public override Var Copy(Var v) {
		//	if (!(v is AnimationData))
		//		throw new Exception("Type must be AnimationData");
		//	var ad = (AnimationData)v;
		//	State	= ad.State;
		//	Counter	= ad.Counter;
		//	X		= ad.X;
		//	Y		= ad.Y;
		//	Palette	= ad.Palette;
		//	Attr	= ad.Attr;

		//	State.Index = Index;
		//	Counter.Index = Index;
		//	X.Index = Index;
		//	Y.Index = Index;
		//	Palette.Index = Index;
		//	Attr.Index = Index;
		//	return this;
		//}
		
		//public override Var Copy(IEnumerable<Var> v) {
		//	var vars = v.ToArray();
		//	if (vars.Count() != 6)
		//		throw new Exception("Type must be AnimationData");
		//	//var ad = (AnimationData)v;
		//	State	= (VByte)vars[0];
		//	Counter	= (VByte)vars[1];
		//	X		= (VByte)vars[2];
		//	Y		= (VByte)vars[3];
		//	Palette	= (VByte)vars[4];
		//	Attr	= (VByte)vars[5];

		//	State.Index = Index;
		//	Counter.Index = Index;
		//	X.Index = Index;
		//	Y.Index = Index;
		//	Palette.Index = Index;
		//	Attr.Index = Index;
		//	return this;
		//}
	}
}