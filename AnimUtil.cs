using NESSharp.Common;
using NESSharp.Core;
using NESSharp.Lib.Animation.Iterators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NESSharp.Lib.Animation;

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
		CPU.Y.Set(0);
		_numTiles.Set(CPU.A.Set(_ptr[CPU.Y]));
		_tileIndex.Set(0);
		CPU.Y.Inc();
		//CPU.X.Set(0);
		Loop.While_PreCondition_NoInc(() => _tileIndex.NotEquals(_numTiles), tileLoop => {
			If.True(_iterator.Invalid, tileLoop.Break);

			CPU.X.Set(_iterator.Value());
			NES.PPU.OAM.Object[CPU.X].Y.Set(CPU.A.Set(_ptr[CPU.Y]).Add(_animData.Y));
			CPU.Y.Inc();
			NES.PPU.OAM.Object[CPU.X].Tile.Set(CPU.A.Set(_ptr[CPU.Y]).Add(_tileOffsetLabel));
			CPU.Y.Inc();
			//OAM.Object[CPU.X].Attr.Set(_ptr[CPU.Y]);
			NES.PPU.OAM.Object[CPU.X].Attr.Set(CPU.A.Set(_ptr[CPU.Y]).Or(_animData.Attr));
			CPU.Y.Inc();
			//TODO: handle palette change here
			//CPU.A.Set(_ptr[CPU.Y]); //compressed array of 4 palette indexes

			NES.PPU.OAM.Object[CPU.X].Attr.Set(z => z.Or(_animData.Palette));
			CPU.Y.Inc();
			//OAM.Object[CPU.X].X.Set(CPU.A.Set(_ptr[CPU.Y]).Add(_animData.X));										//original
			If.Block(c => c
				.True(() => _animData.Attr.And(0b01000000).NotEquals(0), () => {
					//OAM.Object[CPU.X].X.Set(CPU.A.Set(_ptr[CPU.Y]).Subtract(_animData.X));
					NES.PPU.OAM.Object[CPU.X].X.Set(Common.Math.Negate(CPU.A.Set(_ptr[CPU.Y])).Add(_animData.X).Subtract(8));	//attempt 1
				})
				.Else(() => {
					NES.PPU.OAM.Object[CPU.X].X.Set(CPU.A.Set(_ptr[CPU.Y]).Add(_animData.X));
				})
			);
					
			//OAM.Object[CPU.X].X.Set(CPU.A.Set(255).Subtract(_ptr[CPU.Y]).And(0b01000000)).Add(_animData.X));
			CPU.Y.Inc();
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
		_ptr.PointTo(_frameLabelList[CPU.X.Set(CPU.A.Set(animData.State))]);
		AL.GoSub(DrawFrame);
	}
	public void DrawSingleFrame(Func<IOperand> x, Func<IOperand> y, Func<IOperand> palette, Func<IOperand> attr, Func<IOperand> state) {
		//_animData.State.Set(animData.State);
		//_animData.Counter.Set(animData.Counter);
		_animData.X.Set(x());
		_animData.Y.Set(y());
		_animData.Attr.Set(attr());
		_animData.Palette.Set(palette());
		_ptr.PointTo(_frameLabelList[CPU.X.Set(CPU.A.Set(state()))]);
		AL.GoSub(DrawFrame);
	}

	public void DrawSingleObject(IndexingRegister reg, U8 tile, IOperand x, IOperand y, Func<RegisterA> attr) {
		If.True(_iterator.Valid(), () => {
			reg.Set(_iterator.Value());
			NES.PPU.OAM.Object[reg].Y.Set(y);
			NES.PPU.OAM.Object[reg].Tile.Set(tile);
			NES.PPU.OAM.Object[reg].Attr.Set(attr());
			NES.PPU.OAM.Object[reg].X.Set(x);
			_iterator.Next();
		});
	}
	public void DrawSingleObject(IndexingRegister reg, U8 tile, Func<RegisterA> x, Func<RegisterA> y, Func<RegisterA> attr) {
		If.True(_iterator.Valid(), () => {
			reg.Set(_iterator.Value());
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
			AL.GoSub(Update);
		} else {
			Stack.Preserve(animData.State.Index, () => {
				AL.GoSub(Update);
			});
		}
		animData.State.Set(_animData.State);
		animData.Counter.Set(_animData.Counter);
	}
	[Subroutine]
	private void Update() {
		//NES.PPU.Mask.Set(NES.PPU.LazyMask.Set(z => z.Or(0b10000000)));
		_ptr.PointTo(_stateLabelList[CPU.X.Set(_animData.State)]);
		_stateLength.Set(_ptr[CPU.Y.Set(0)]);
		CPU.Y.Inc();
		_stateLoop.Set(_ptr[CPU.Y]);
		CPU.Y.Inc();
		_stateNext.Set(_ptr[CPU.Y]);
		CPU.Y.Inc(); //now on last frame start

		Loop.Infinite(loop => {
			If.Block(c => c
				.True(() => CPU.A.Set(_ptr[CPU.Y]).LessThanOrEqualTo(_animData.Counter), () => {
					CPU.Y.Inc(); //now on frame ID
					_ptr.PointTo(_frameLabelList[CPU.X.Set(CPU.A.Set(_ptr[CPU.Y]))]);
					AL.GoSub(DrawFrame); //CPU.Y is no longer needed after this because of the break
					_animData.Counter.Inc();
					loop.Break();
				})
				.Else(() => CPU.Y.Inc()) //now on frame ID
			);
			CPU.Y.Inc(); //now on frame start
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
		_tileOffsetLabel = AL.Labels.New().Write();
		AL.Raw(animConfig.Offset);

		//State data
		var stateLabels = new List<Label>();
		foreach (var state in animConfig.States) {
			AL.Comment($"Animation state definition: {state.Name}");
			var lbl = AL.Labels.New().Write();
			stateLabels.Add(lbl);
			AL.Raw((byte)state.Length);
			AL.Raw(state.Loop ? (byte)1 : (byte)0);
			AL.Raw((byte)(state.NextState ?? 0));
			foreach (var frame in state.Frames.OrderByDescending(x => (int)x.Start)) {
				AL.Raw(frame.Start, frame.Id);
			}
		}
		
		AL.Comment($"Animation state labels");
		_stateLabelList = new LabelList(stateLabels.ToArray());
		_stateLabelList.WriteList();

		foreach (var frame in animConfig.Frames) {
			AL.Comment($"Animation frame definition: {frame.Name}");
			var lbl = AL.Labels.New().Write();
			_frames.Add(frame.Name, lbl);
			frame.Write();
		}
		AL.Comment($"Animation frame labels");
		_frameLabelList = new LabelList(_frames.Values.ToArray());
		_frameLabelList.WriteList();
	}
}
