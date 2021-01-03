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
			Console.WriteLine(animConfig);
		}
		public AnimUtil Initialize(IIterator iterator) {
			_iterator = iterator;
			return this;
		}
		public void StartFrame() {
			OAM.HideAll();
			_iterator.Reset();
		}
		public void EndFrame() {}

		[Subroutine]
		private void DrawFrame() {
			Y.Set(0);
			_numTiles.Set(A.Set(_ptr[Y]));
			_tileIndex.Set(0);
			Y++;
			//X.Set(0);
			Loop.While(() => _tileIndex.NotEquals(_numTiles), tileLoop => {
				If(_iterator.Invalid, () => GoTo(tileLoop.Break));

				X.Set(_iterator.Value());
				OAM.Object[X].Y.Set(A.Set(_ptr[Y]).Add(_animData.Y));
				Y++;
				OAM.Object[X].Tile.Set(A.Set(_ptr[Y]).Add(_tileOffsetLabel));
				Y++;
				//OAM.Object[X].Attr.Set(_ptr[Y]);
				OAM.Object[X].Attr.Set(A.Set(_ptr[Y]).Or(_animData.Attr));
				Y++;
				//TODO: handle palette change here
				//A.Set(_ptr[Y]); //compressed array of 4 palette indexes

				OAM.Object[X].Attr.Set(z => z.Or(_animData.Palette));
				Y++;
				//OAM.Object[X].X.Set(A.Set(_ptr[Y]).Add(_animData.X));										//original
				If(	Option(() => _animData.Attr.And(0b01000000).NotEquals(0), () => {
						//OAM.Object[X].X.Set(A.Set(_ptr[Y]).Subtract(_animData.X));
						OAM.Object[X].X.Set(Common.Math.Negate(A.Set(_ptr[Y])).Add(_animData.X).Subtract(8));	//attempt 1
					}),
					Default(() => {
						OAM.Object[X].X.Set(A.Set(_ptr[Y]).Add(_animData.X));
					})
				);
						
				//OAM.Object[X].X.Set(A.Set(255).Subtract(_ptr[Y]).And(0b01000000)).Add(_animData.X));
				Y++;
				_iterator.Next();
				_tileIndex.Increment();
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

		public void DrawSingleObject(U8 tile, VByte x, VByte y, Func<RegisterA> attr) {
			If(_iterator.Valid(), () => {
				X.Set(_iterator.Value());
				OAM.Object[X].Y.Set(y);
				OAM.Object[X].Tile.Set(tile);
				OAM.Object[X].Attr.Set(attr());
				OAM.Object[X].X.Set(x);
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
			Y++;
			_stateLoop.Set(_ptr[Y]);
			Y++;
			_stateNext.Set(_ptr[Y]);
			Y++; //now on last frame start

			Loop.Infinite(loop => {
				If(	Option(() => A.Set(_ptr[Y]).LessThanOrEqualTo(_animData.Counter), () => {
						Y++; //now on frame ID
						_ptr.PointTo(_frameLabelList[X.Set(A.Set(_ptr[Y]))]);
						GoSub(DrawFrame); //Y is no longer needed after this because of the break
						_animData.Counter++;
						GoTo(loop.Break);
					}),
					Default(() => {
						Y++; //now on frame ID
					})
				);
				Y++; //now on frame start
			});
			If(() => _animData.Counter.Equals(_stateLength), () => {	//is counter maxed out?
				_animData.Counter.Set(0);								//	reset it
				If(() => _stateLoop.Equals(0), () => {					//if this state doesn't loop,
					_animData.State.Set(_stateNext);					//	move to the specified next state
				});
			});
			//NES.PPU.Mask.Set(NES.PPU.LazyMask.Set(z => z.And(0b01111111)));
		}
		
		[DataSection]
		private void AnimData() {
			//General data
			_tileOffsetLabel = Labels.New();
			Use(_tileOffsetLabel);
			Raw(animConfig.Offset);

			//State data
			var stateLabels = new List<Label>();
			foreach (var state in animConfig.States) {
				var lbl = Labels.New();
				Comment($"Animation state definition: {state.Name}");
				stateLabels.Add(lbl);
				Use(lbl);
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
				var lbl = Labels.New();
				_frames.Add(frame.Name, lbl);
				Use(lbl);
				frame.Write();
			}
			Comment($"Animation frame labels");
			_frameLabelList = new LabelList(_frames.Values.ToArray());
			_frameLabelList.WriteList();
		}
	}
}
