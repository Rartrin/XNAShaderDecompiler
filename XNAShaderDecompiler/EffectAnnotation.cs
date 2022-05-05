namespace XNAShaderDecompiler
{
	public sealed class EffectAnnotation
	{
		public EffectValue Value{get;init;}

		public static EffectAnnotation[] ReadList(Effect effect, uint numAnnotations, BinReader br, BinReader @base)
		{
			var annotations = new EffectAnnotation[numAnnotations];
			
			for (int i = 0; i < numAnnotations; i++)
			{
				annotations[i] = Read(effect, br, @base);
			}

			return annotations;
		}

		public static EffectAnnotation Read(Effect effect, BinReader br, BinReader @base)
		{
			uint typeOffset = br.Read<uint>();
			uint valOffset = br.Read<uint>();

			return new EffectAnnotation
			{
				Value = EffectValue.ReadValue(effect, @base, typeOffset, valOffset)
			};
		}
	}
}
