using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace XNAShaderDecompiler
{
	public struct SymbolStructMember
	{
		public string Name;
		public SymbolTypeInfo Info;
	}

	public struct SymbolTypeInfo
	{
		public SymbolClass ParameterClass;
		public SymbolType ParameterType;
		public uint Rows;
		public uint Columns;
		public uint Elements;
		public SymbolStructMember[] Members;
	}

	public struct EffectSamplerMap
	{
		public SymbolType Type;
		public string Name;
	}

	public sealed class EffectObject
	{
		public SymbolType Type{get;}

		public EffectShader Shader;
		public EffectSamplerMap Mapping;
		public string String;//EffectString
		//public EffectTexture Texture;

		public EffectObject(SymbolType type)
		{
			Type = type;
		}
	}

	public sealed class EffectSamplerState
	{
		public SamplerStateType Type{get;init;}
		public EffectValue Value{get;init;}
	}

	/* Defined later in the state change types... */
	public sealed class SamplerStateRegister
	{
		public string SamplerName{get;init;}
		public uint SamplerRegister{get;init;}
		public IReadOnlyList<EffectSamplerState> SamplerStates{get;init;}
	}

	public sealed class Preshader
	{
		public IReadOnlyList<double> Literals;
		public uint TempCount;
		public IReadOnlyList<Symbol> Symbols;
		//public IReadOnlyList<PreshaderInstruction> Instructions;
	}

	public struct EffectShader
	{
		public SymbolType Type;
		public int Technique;
		public int Pass;
		public bool IsPreshader;
		public IReadOnlyList<uint> PreshaderParams;
		public IReadOnlyList<uint> Params;
		public IReadOnlyList<SamplerStateRegister> Samplers;
			
		//union
		//{
		public object Shader; /* glShader, mtlShader, etc. */
		public Preshader Preshader;
		//};
	}

	public enum SymbolRegisterSet
	{
		Bool,
		Int4,
		Float4,
		Sampler,
	}
	public sealed class Symbol
	{
		public string Name;
		public SymbolRegisterSet RegisterSet;
		public uint RegisterIndex;
		public uint RegisterCount;
		public SymbolTypeInfo Info;
	}

	public static class EffectParser
	{
		public static Effect Parse(byte[] effectCode)
		{
			BinReader br = new BinReader(effectCode);

			var header = br.Read<uint>();
			if (header == 0xBCF00BCF)
			{
				var skip = br.Read<uint>() - 8;
				br.Skip(skip);
				header = br.Read<uint>();
			}

			if (header != 0xFEFF0901)
				throw new ContentLoadException("Invalid Effect!");

			uint offset = br.Read<uint>();
			//if (offset > length)
			//    throw new EndOfStreamException();

			var @base = br.Slice(0);
			br.Skip(offset);

			//if (length < 16)
			//    throw new EndOfStreamException();

			uint numParams = br.Read<uint>();
			uint numTechniques = br.Read<uint>();
			uint unknown = br.Read<uint>();
			uint numObjects = br.Read<uint>();

			var effect = new Effect
			{
				Objects = new EffectObject[numObjects]
			};
			effect.Params = EffectParam.ReadList(effect, numParams, br, @base);
			effect.Techniques = EffectTechnique.ReadList(effect, numTechniques, br, @base);
			

			//if (length < 8)
			//    throw new EndOfStreamException();

			uint numSmallObjects = br.Read<uint>();
			uint numLargeObjects = br.Read<uint>();

			ReadSmallObjects(effect, numSmallObjects, br);
			ReadLargeObjects(effect, numLargeObjects, br);

			return effect;
		}

		private static void ReadSmallObjects(Effect effect, uint numSmallObjects, BinReader br)
		{
			for(int i = 0; i < numSmallObjects; i++)
			{
				ReadSmallObject(effect, br);
			}
		}

		private static void ReadSmallObject(Effect effect, BinReader br)
		{
			var index = br.Read<uint>();
			var length = br.Read<uint>();

			ref var obj = ref effect.Objects[index];
			if(obj.Type == SymbolType.String)
			{
				if(length>0)
				{
					obj.String = br.ReadString(0, length);
				}
			}
			else if(obj.Type == SymbolType.Texture || obj.Type == SymbolType.Texture1D || obj.Type == SymbolType.Texture2D || obj.Type == SymbolType.Texture3D || obj.Type == SymbolType.TextureCube
				 || obj.Type == SymbolType.Sampler || obj.Type == SymbolType.Sampler1D || obj.Type == SymbolType.Sampler2D || obj.Type == SymbolType.Sampler3D || obj.Type == SymbolType.SamplerCube)
			{
				if(length>0)
				{
					obj.Mapping.Name = br.ReadString(0, length);
				}
			}
			else if(obj.Type == SymbolType.PixelShader || obj.Type == SymbolType.VertexShader)
			{
				var mainfn = $"ShaderFunction{index}";
				obj.Shader.Technique = -1;
				obj.Shader.Pass = -1;
				obj.Shader.Shader = CompileShader(mainfn, br, length);

				if(obj.Shader.Shader == null)
				{
					throw new Exception();
				}

				GetParseData(obj.Shader.Shader, out var symbols, out var preshader);

				int samplerCount = 0;
				for(int j=0; j<symbols.Length; j++)
				{
					if(symbols[j].RegisterSet == SymbolRegisterSet.Sampler)
					{
						samplerCount++;
					}
				}
				var shaderParams = new uint[samplerCount];
				var samplers = new SamplerStateRegister[samplerCount];
				uint curSampler = 0;
				for(int j=0; j<symbols.Length; j++)
				{
					uint par = FindParameter(effect, symbols[j].Name);
					shaderParams[j] = par;
					if(symbols[j].RegisterSet == SymbolRegisterSet.Sampler)
					{
						samplers[curSampler] = new SamplerStateRegister
						{
							SamplerName = effect.Params[(int)par].Value.Name,
							SamplerRegister = symbols[j].RegisterIndex,
							SamplerStates = effect.Params[(int)par].Value.ValuesSS
						};
						curSampler++;
					}
				}
				obj.Shader.Params = shaderParams;
				obj.Shader.Samplers = samplers;

				if(preshader != null)
				{
					var preshaderParams = new uint[preshader.Symbols.Count];
					for(int j=0; j<preshader.Symbols.Count; j++)
					{
						preshaderParams[j] = FindParameter(effect, preshader.Symbols[j].Name);
					}
					obj.Shader.PreshaderParams = preshaderParams;
				}
			}
			else
			{
				throw new Exception("Small object type unknown!");
			}

			//Object block is always a multiple of four
			uint blocklen = (length + 3) - ((length - 1) % 4);
			br.Skip(blocklen);
		}

		private static void ReadLargeObjects(Effect effect, uint numLargeObjects, BinReader br)
		{
			for(uint i=0; i<numLargeObjects; i++)
			{
				ReadLargeObject(effect, br);
			}
		}
		private static void ReadLargeObject(Effect effect, BinReader br)
		{
			int technique = br.Read<int>();
			int index = br.Read<int>();
			uint FIXME = br.Read<uint>();
			int state = br.Read<int>();
			uint type = br.Read<uint>();
			uint length = br.Read<uint>();

			int objectIndex;
			if(technique == -1)
			{
				objectIndex = effect.Params[index].Value.ValuesSS[state].Value.ValuesI[0];
			}
			else
			{
				objectIndex = effect.Techniques[technique].Passes[index].States[state].Value.ValuesI[0];
			}

			ref var obj = ref effect.Objects[objectIndex];
			if (obj.Type == SymbolType.PixelShader || obj.Type == SymbolType.VertexShader)
			{
				obj.Shader.Technique = technique;
				obj.Shader.Pass = index;

				if(type == 2)
				{
					// This is a standalone preshader!
					// It exists solely for effect passes that do not use a single
					// vertex/fragment shader.
					obj.Shader.IsPreshader = true;

					var array = br.ReadString(0);
					var start = (uint)array.Length+4;//Gets the total length of the string and length field
					obj.Shader.Params = new[]{FindParameter(effect, array)};

					obj.Shader.Preshader = ParsePreshader(br.Slice(start), length);

					// !!! FIXME: check for errors.
					var preshaderParams = new uint[obj.Shader.Preshader.Symbols.Count];
					for(int j=0; j<obj.Shader.Preshader.Symbols.Count; j++)
					{
						preshaderParams[j] = FindParameter(effect, obj.Shader.Preshader.Symbols[j].Name);
					}
					obj.Shader.PreshaderParams = preshaderParams;
				}
				else
				{

					string mainfn = $"ShaderFuntion{objectIndex}";
					obj.Shader.Shader = CompileShader(mainfn, br, length);
					File.WriteAllBytes($"Obj{objectIndex}_Shader.bin", (byte[])obj.Shader.Shader);

					//if(obj.Shader.Shader == null)
					//{
					//	throw new Exception();
					//}
					//GetParseData(obj.Shader.Shader, out var symbols, out var preshader);

					//int samplerCount = 0;
					//for(int j=0; j<symbols.Length; j++)
					//{
					//	if(symbols[j].RegisterSet == SymbolRegisterSet.Sampler)
					//	{
					//		samplerCount++;
					//	}
					//}
					//obj.Shader.Params = new uint[samplerCount];
					//obj.Shader.Samplers = new SamplerStateRegister[samplerCount];
					//uint curSampler = 0;
					//for(int j=0; j<symbols.Length; j++)
					//{
					//	uint par = FindParameter(symbols[j].Name);
					//	obj.Shader.Params[j] = par;
					//	if(symbols[j].RegisterSet == SymbolRegisterSet.Sampler)
					//	{
					//		obj.Shader.Samplers[curSampler] = new SamplerStateRegister
					//		{
					//			SamplerName = EffectParams[(int)par].Value.Name,
					//			SamplerRegister = symbols[j].RegisterIndex,
					//			SamplerStates = EffectParams[(int)par].Value.ValuesSS
					//		};
					//		curSampler++;
					//	}
					//}

					//if(preshader != null)
					//{
					//	obj.Shader.PreshaderParams = new uint[preshader.Symbols.Length];
					//	for(int j=0; j<preshader.Symbols.Length; j++)
					//	{
					//		obj.Shader.PreshaderParams[j] = FindParameter(preshader.Symbols[j].Name);
					//	}
					//}
				}
			}
			else if(obj.Type == SymbolType.Texture || obj.Type == SymbolType.Texture1D || obj.Type == SymbolType.Texture2D || obj.Type == SymbolType.Texture3D || obj.Type == SymbolType.TextureCube
				 || obj.Type == SymbolType.Sampler || obj.Type == SymbolType.Sampler1D || obj.Type == SymbolType.Sampler2D || obj.Type == SymbolType.Sampler3D || obj.Type == SymbolType.SamplerCube)
			{
				obj.Mapping.Name = br.ReadString(0, length);
			}
			else if (obj.Type != SymbolType.Void) // FIXME: Why? -flibit
			{
				throw new Exception("Large object type unknown!");
			}
				
			// Object block is always a multiple of four
			uint blocklen = (length + 3) - ((length - 1) % 4);
			br.Skip(blocklen);
		}

		private static uint FindParameter(Effect effect, string name)
		{
			for(int i=0; i<effect.Params.Length; i++)
			{
				if(name == effect.Params[i].Value.Name)
				{
					return (uint)i;
				}
			}
			throw new Exception("Parameter not found!");
		}

		private static void GetParseData(object shader, out Symbol[] symbols, out Preshader preshader)
		{
			throw new NotImplementedException();
		}

		private static Preshader ParsePreshader(BinReader br, uint length)
		{
			throw new NotImplementedException();
		}

		private static object CompileShader(string tokenBuf, BinReader br, uint length)
		{
			//throw new NotImplementedException();
			return br.ReadBytes(0, length);
		}

		//https://github.com/icculus/mojoshader/blob/main/mojoshader_effects.c
		//https://github.com/icculus/mojoshader/blob/main/mojoshader_effects.h

		//MOJOSHADER_parseData
		//https://github.com/icculus/mojoshader/blob/main/mojoshader.h
	}
}