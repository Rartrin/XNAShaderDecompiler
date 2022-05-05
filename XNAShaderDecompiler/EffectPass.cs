using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XNAShaderDecompiler
{
	public sealed class EffectPass
	{
		public string Name{get;init;}
		public IReadOnlyList<EffectAnnotation> Annotations{get;init;}
		public IReadOnlyList<EffectState> States{get;init;}

		public static EffectPass[] ReadList(Effect effect, uint numPasses, BinReader br, BinReader @base)
		{
			var passes = new EffectPass[numPasses];

			for (int i = 0; i < numPasses; i++)
			{
				passes[i] = Read(effect, br, @base);
			}
			
			return passes;
		}

		public static EffectPass Read(Effect effect, BinReader br, BinReader @base)
		{
			uint passNameOffset = br.Read<uint>();
			uint numAnnotations = br.Read<uint>();
			uint numStates = br.Read<uint>();

			return new EffectPass
			{
				Name = @base.ReadString(passNameOffset),
				Annotations = EffectAnnotation.ReadList(effect, numAnnotations, br, @base),
				States = EffectState.ReadList(effect, numStates, br, @base)
			};
		}

	}
}
