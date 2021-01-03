using NESSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace NESSharp.Lib.Animation.Iterators {
	public interface IIterator {
		public void Reset();
		public void Next();
		//public T Value<T>() where T : IndexingRegisterBase;
		public RegisterA Value();
		public Func<Condition> Valid();
		public Func<Condition> Invalid();
	}
}
