﻿using NESSharp.Core;
using NESSharp.Common;
using System;

namespace NESSharp.Lib.Animation.Iterators {
	public class SequentialIterator : Module, IIterator {
		private VByte _length;
		private VByte _index;
		[Dependencies]
		public void Dependencies() {
			_length	= VByte.New(Zp, $"{nameof(SequentialIterator)}{nameof(_length)}");
			_index	= VByte.New(Zp, $"{nameof(SequentialIterator)}{nameof(_index)}");
		}
		public void Setup(IOperand val) {
			_length.Set(val);
		}
		public void Reset() {
			_index.Set(0);
		}
		public void Next() {
			_index.Inc();
		}
		public RegisterA Value() {
			return _index.Multiply(4);
		}
		public Func<Condition> Valid() {
			return () => _index.NotEquals(_length);
		}
		public Func<Condition> Invalid() {
			return () => _index.Equals(_length);
		}
	}
}
