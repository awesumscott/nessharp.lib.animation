using NESSharp.Core;
using NESSharp.Common;
using System;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.Animation.Iterators {
	public class SkipIterator : Module, IIterator {
		private VByte _length;
		private VByte _index;
		private U8 _skipDist;

		public SkipIterator Initialize(U8 skipDist) {
			_skipDist = skipDist;
			return this;
		}

		[Dependencies]
		public void Dependencies() {
			_length	= VByte.New(Zp, $"{nameof(SequentialIterator)}{nameof(_length)}");
			_index	= VByte.New(Zp, $"{nameof(SequentialIterator)}{nameof(_index)}");
		}
		public void Setup(RegisterA val) {
			_length.Set(val);
		}
		public void Reset() {
			//_index.Set(0);
		}
		public void Next() {
			_index.Set(z => z.Add(_skipDist));
		}
		public RegisterA Value() {
			return _index.Multiply(4);
		}
		public Func<Condition> Valid() {
			//TODO: ensure 64 objs haven't yet been filled this frame
			return () => A.Set(0).Equals(0); //_index.NotEquals(_length);
		}
		public Func<Condition> Invalid() {
			//TODO: ensure 64 objs haven't yet been filled this frame
			return () => A.Set(0).NotEquals(0); //_index.NotEquals(_length);
		}
	}
}
