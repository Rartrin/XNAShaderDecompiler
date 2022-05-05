using System.Collections.Generic;

namespace XNAShaderDecompiler
{
	public sealed class EffectTechnique
	{
		public string Name{get;init;}
		public IReadOnlyList<EffectAnnotation> Annotations{get;init;}
		public IReadOnlyList<EffectPass> Passes{get;init;}

		public static EffectTechnique[] ReadList(Effect effect, uint numTechniques, BinReader br, BinReader @base)
		{
			var effectTechniques = new EffectTechnique[numTechniques];
			
			for (int i = 0; i < numTechniques; i++)
			{
				effectTechniques[i] = Read(effect, br, @base);
			}

			return effectTechniques;
		}

		public static EffectTechnique Read(Effect effect, BinReader br, BinReader @base)
		{
			uint nameOffset = br.Read<uint>();
			uint numAnnotations = br.Read<uint>();
			uint numPasses = br.Read<uint>();

			return new EffectTechnique
			{
				Name = @base.ReadString(nameOffset),
				Annotations = EffectAnnotation.ReadList(effect, numAnnotations, br, @base),
				Passes = EffectPass.ReadList(effect, numPasses, br, @base)
			};
		}
	}
}
