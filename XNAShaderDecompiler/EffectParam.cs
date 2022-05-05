using System.Collections.Generic;

namespace XNAShaderDecompiler
{
	public sealed class EffectParam
	{
		public IReadOnlyList<EffectAnnotation> Annotations{get;init;}
		public EffectValue Value{get;init;}

		public static EffectParam[] ReadList(Effect effect, uint numParams, BinReader br, BinReader @base)
		{
			var effectParams = new EffectParam[numParams];

			for (int i = 0; i < numParams; i++)
			{
				effectParams[i] = Read(effect, br, @base);
			}

			return effectParams;
		}

		public static EffectParam Read(Effect effect, BinReader br, BinReader @base)
		{
			uint typeOffset = br.Read<uint>();
			uint valOffset = br.Read<uint>();
			uint flags = br.Read<uint>();
			uint numAnnotations = br.Read<uint>();

			return new EffectParam
			{
				Annotations = EffectAnnotation.ReadList(effect, numAnnotations, br, @base),
				Value = EffectValue.ReadValue(effect, @base, typeOffset, valOffset)
			};
		}
	}
}
