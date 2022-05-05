namespace XNAShaderDecompiler
{
	public sealed class EffectState
	{
		public RenderStateType Type{get;init;}
		public EffectValue Value{get;init;}

		public static EffectState[] ReadList(Effect effect, uint numStates, BinReader br, BinReader @base)
		{
			var states = new EffectState[numStates];

			for (int i = 0; i < numStates; i++)
			{
				states[i] = Read(effect, br, @base);
			}

			return states;
		}

		public static EffectState Read(Effect effect, BinReader br, BinReader @base)
		{
			var type = br.Read<RenderStateType>();
			var unknown = br.Read<uint>();
			var typeOffset = br.Read<uint>();
			var valOffset = br.Read<uint>();

			return new EffectState
			{
				Type = type,
				Value = EffectValue.ReadValue(effect, @base, typeOffset, valOffset)
			};
		}
	}
}
