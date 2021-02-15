using NESSharp.Common;
using NESSharp.Core;
using NESSharp.Lib.Animation.Iterators;
using System;
using System.Collections.Generic;
using System.Linq;
using static NESSharp.Core.AL;

namespace NESSharp.Lib.Animation {
	public class AnimUtil : Module {
		private Ptr _ptr;
		private AnimationData _animData;
		private VByte _numTiles;
		private VByte _tileIndex;
		private VByte _stateLength;
		private VByte _stateLoop;
		private VByte _stateNext;
		private Dictionary<string, Label> _frames;
		private IIterator _iterator;
		private Models.Animation animConfig;
		private Label _tileOffsetLabel;
		private LabelList _stateLabelList;
		private LabelList _frameLabelList;

		[Dependencies]
		private void Dependencies() {
			_ptr			= Ptr	.New(Zp,					$"{nameof(AnimUtil)}{nameof(_ptr)}");
			_animData		= Struct.New<AnimationData>(Zp,		$"{nameof(AnimUtil)}{nameof(_animData)}");
			_numTiles		= VByte	.New(Zp,					$"{nameof(AnimUtil)}{nameof(_numTiles)}");
			_tileIndex		= VByte	.New(Zp,					$"{nameof(AnimUtil)}{nameof(_tileIndex)}");
			_stateLength	= VByte	.New(Zp,					$"{nameof(AnimUtil)}{nameof(_stateLength)}");
			_stateLoop		= VByte	.New(Zp,					$"{nameof(AnimUtil)}{nameof(_stateLoop)}");
			_stateNext		= VByte	.New(Zp,					$"{nameof(AnimUtil)}{nameof(_stateNext)}");
			_frames = new Dictionary<string, Label>();
		}
		
		public void LoadConfig(string file) {
			if (!Json.TryLoadFile(file, out animConfig)) throw new Exception("Anim config file is bad");
			//Console.WriteLine(animConfig);
		}
		public AnimUtil Initialize(IIterator iterator) {
			_iterator = iterator;
			return this;
		}
		public void StartFrame() {
			NES.PPU.OAM.HideAll();
			_iterator.Reset();
		}
		public void EndFrame() {}

		[Subroutine]
		private void DrawFrame() {
			Y.Set(0);
			_numTiles.Set(A.Set(_ptr[Y]));
			_tileIndex.Set(0);
			Y.Increment();
			//X.Set(0);
			Loop.While_Pre(() => _tileIndex.NotEquals(_numTiles), tileLoop => {
				If.True(_iterator.Invalid, tileLoop.Break);

				X.Set(_iterator.Value());
				NES.PPU.OAM.Object[X].Y.Set(A.Set(_ptr[Y]).Add(_animData.Y));
				Y.Increment();
				NES.PPU.OAM.Object[X].Tile.Set(A.Set(_ptr[Y]).Add(_tileOffsetLabel));
				Y.Increment();
				//OAM.Object[X].Attr.Set(_ptr[Y]);
				NES.PPU.OAM.Object[X].Attr.Set(A.Set(_ptr[Y]).Or(_animData.Attr));
				Y.Increment();
				//TODO: handle palette change here
				//A.Set(_ptr[Y]); //compressed array of 4 palette indexes

				NES.PPU.OAM.Object[X].Attr.Set(z => z.Or(_animData.Palette));
				Y.Increment();
				//OAM.Object[X].X.Set(A.Set(_ptr[Y]).Add(_animData.X));										//original
				If.Block(c => c
					.True(() => _animData.Attr.And(0b01000000).NotEquals(0), () => {
						//OAM.Object[X].X.Set(A.Set(_ptr[Y]).Subtract(_animData.X));
						NES.PPU.OAM.Object[X].X.Set(Common.Math.Negate(A.Set(_ptr[Y])).Add(_animData.X).Subtract(8));	//attempt 1
					})
					.Else(() => {
						NES.PPU.OAM.Object[X].X.Set(A.Set(_ptr[Y]).Add(_animData.X));
					})
				);
						
				//OAM.Object[X].X.Set(A.Set(255).Subtract(_ptr[Y]).And(0b01000000)).Add(_animData.X));
				Y.Increment();
				_iterator.Next();
				_tileIndex.Inc();
			});
		}

		public void DrawSingleFrame(AnimationData animData) {
			//_animData.State.Set(animData.State);
			//_animData.Counter.Set(animData.Counter);
			_animData.X.Set(animData.X);
			_animData.Y.Set(animData.Y);
			_animData.Attr.Set(animData.Attr);
			_animData.Palette.Set(animData.Palette);
			_ptr.PointTo(_frameLabelList[X.Set(A.Set(animData.State))]);
			GoSub(DrawFrame);
		}
		public void DrawSingleFrame(Func<IOperand> x, Func<IOperand> y, Func<IOperand> palette, Func<IOperand> attr, Func<IOperand> state) {
			//_animData.State.Set(animData.State);
			//_animData.Counter.Set(animData.Counter);
			_animData.X.Set(x());
			_animData.Y.Set(y());
			_animData.Attr.Set(attr());
			_animData.Palette.Set(palette());
			_ptr.PointTo(_frameLabelList[X.Set(A.Set(state()))]);
			GoSub(DrawFrame);
		}

		public void DrawSingleObject(IndexingRegister reg, U8 tile, IOperand x, IOperand y, Func<RegisterA> attr) {
			If.True(_iterator.Valid(), () => {
				if (reg is RegisterX)	X.Set(_iterator.Value());
				else					Y.Set(_iterator.Value());
				NES.PPU.OAM.Object[reg].Y.Set(y);
				NES.PPU.OAM.Object[reg].Tile.Set(tile);
				NES.PPU.OAM.Object[reg].Attr.Set(attr());
				NES.PPU.OAM.Object[reg].X.Set(x);
				_iterator.Next();
			});
		}
		public void DrawSingleObject(IndexingRegister reg, U8 tile, Func<RegisterA> x, Func<RegisterA> y, Func<RegisterA> attr) {
			If.True(_iterator.Valid(), () => {
				if (reg is RegisterX)	X.Set(_iterator.Value());
				else					Y.Set(_iterator.Value());
				NES.PPU.OAM.Object[reg].Y.Set(y());
				NES.PPU.OAM.Object[reg].Tile.Set(tile);
				NES.PPU.OAM.Object[reg].Attr.Set(attr());
				NES.PPU.OAM.Object[reg].X.Set(x());
				_iterator.Next();
			});
		}

		public void Update(AnimationData animData) {
			_animData.State.Set(animData.State);
			_animData.Counter.Set(animData.Counter);
			_animData.X.Set(animData.X);
			_animData.Y.Set(animData.Y);
			_animData.Attr.Set(animData.Attr);
			_animData.Palette.Set(animData.Palette);
			if (animData.State.Index == null) {
				GoSub(Update);
			} else {
				Stack.Preserve(animData.State.Index, () => {
					GoSub(Update);
				});
			}
			animData.State.Set(_animData.State);
			animData.Counter.Set(_animData.Counter);
		}
		[Subroutine]
		private void Update() {
			//NES.PPU.Mask.Set(NES.PPU.LazyMask.Set(z => z.Or(0b10000000)));
			_ptr.PointTo(_stateLabelList[X.Set(_animData.State)]);
			_stateLength.Set(_ptr[Y.Set(0)]);
			Y.Increment();
			_stateLoop.Set(_ptr[Y]);
			Y.Increment();
			_stateNext.Set(_ptr[Y]);
			Y.Increment(); //now on last frame start

			Loop.Infinite(loop => {
				If.Block(c => c
					.True(() => A.Set(_ptr[Y]).LessThanOrEqualTo(_animData.Counter), () => {
						Y.Increment(); //now on frame ID
						_ptr.PointTo(_frameLabelList[X.Set(A.Set(_ptr[Y]))]);
						GoSub(DrawFrame); //Y is no longer needed after this because of the break
						_animData.Counter.Inc();
						loop.Break();
					})
					.Else(() => Y.Increment()) //now on frame ID
				);
				Y.Increment(); //now on frame start
			});
			If.True(() => _animData.Counter.Equals(_stateLength), () => {	//is counter maxed out?
				_animData.Counter.Set(0);								//	reset it
				If.True(() => _stateLoop.Equals(0), () => {					//if this state doesn't loop,
					_animData.State.Set(_stateNext);					//	move to the specified next state
				});
			});
			//NES.PPU.Mask.Set(NES.PPU.LazyMask.Set(z => z.And(0b01111111)));
		}
		
		[DataSection]
		private void AnimData() {
			//General data
			_tileOffsetLabel = Labels.New().Write();
			Raw(animConfig.Offset);

			//State data
			var stateLabels = new List<Label>();
			foreach (var state in animConfig.States) {
				Comment($"Animation state definition: {state.Name}");
				var lbl = Labels.New().Write();
				stateLabels.Add(lbl);
				Raw((byte)state.Length);
				Raw(state.Loop ? (byte)1 : (byte)0);
				Raw((byte)(state.NextState ?? 0));
				foreach (var frame in state.Frames.OrderByDescending(x => (int)x.Start)) {
					Raw(frame.Start, frame.Id);
				}
			}
			
			Comment($"Animation state labels");
			_stateLabelList = new LabelList(stateLabels.ToArray());
			_stateLabelList.WriteList();

			foreach (var frame in animConfig.Frames) {
				Comment($"Animation frame definition: {frame.Name}");
				var lbl = Labels.New().Write();
				_frames.Add(frame.Name, lbl);
				frame.Write();
			}
			Comment($"Animation frame labels");
			_frameLabelList = new LabelList(_frames.Values.ToArray());
			_frameLabelList.WriteList();
		}
	}
}
