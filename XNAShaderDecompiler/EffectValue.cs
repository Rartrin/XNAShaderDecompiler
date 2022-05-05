using System;
using System.Collections;
using System.Collections.Generic;

namespace XNAShaderDecompiler
{
	public struct EffectValue
	{
		public string Name;
		public string Semantic;
		public SymbolTypeInfo Type;
			
		public IList Values{private get;set;}

		public IReadOnlyList<int> ValuesI => (IReadOnlyList<int>)Values;
		public IReadOnlyList<float> ValuesF => (IReadOnlyList<float>)Values;

		public IReadOnlyList<EffectSamplerState> ValuesSS => (IReadOnlyList<EffectSamplerState>)Values;

		public static EffectValue ReadValue(Effect effect, BinReader @base, uint typeOffset, uint valOffset)
		{
			EffectValue value = new EffectValue();

			var typePtr = @base.Slice(typeOffset);
			var valPtr = @base.Slice(valOffset);

			SymbolType type = typePtr.Read<SymbolType>();
			SymbolClass valClass = typePtr.Read<SymbolClass>();
			uint nameOffset = typePtr.Read<uint>();
			uint semanticOffset = typePtr.Read<uint>();
			uint numElements = typePtr.Read<uint>();

			value.Type.ParameterType = type;
			value.Type.ParameterClass = valClass;
			value.Name = @base.ReadString(nameOffset);
			value.Semantic = @base.ReadString(semanticOffset);
			value.Type.Elements = numElements;

			/* Class sanity check */
			if(valClass < SymbolClass.Scalar || valClass > SymbolClass.Struct)
			{
				throw new Exception();
			}
			
			if (valClass == SymbolClass.Scalar
			 || valClass == SymbolClass.Vector
			 || valClass == SymbolClass.MatrixRows
			 || valClass == SymbolClass.MatrixColumns)
			{
				/* These classes only ever contain scalar values */
				if(type < SymbolType.Bool || type > SymbolType.Float)
				{
					throw new Exception();
				}

				var columnCount = typePtr.Read<uint>();
				var rowCount = typePtr.Read<uint>();

				value.Type.Columns = columnCount;
				value.Type.Rows = rowCount;

				uint size = 4 * rowCount;
				if(numElements > 0)
				{
					size *= numElements;
				}
				var values = new float[size];

				for(uint i = 0; i<size; i+=4)
				{
					for(uint c = 0; c<columnCount; c++)
					{
						values[i+c] = valPtr.Read<float>(columnCount*i + c);
					}
				}
				value.Values = values;
			}
			else if(valClass == SymbolClass.Object)
			{
				/* This class contains either samplers or "objects" */
				if(type < SymbolType.String || type > SymbolType.VertexShader)
				{
					throw new Exception();
				}

				if (type == SymbolType.Sampler || type == SymbolType.Sampler1D || type == SymbolType.Sampler2D || type == SymbolType.Sampler3D || type == SymbolType.SamplerCube)
				{
					var numStates = valPtr.Read<uint>();

					var values = new EffectSamplerState[numStates];

					for(int i=0; i<numStates; i++)
					{
						var stype = (SamplerStateType)(valPtr.Read<uint>() & ~0xA0);
						valPtr.Skip<uint>();//FIXME
						var stateTypeOffset = valPtr.Read<uint>();
						var stateValOffset = valPtr.Read<uint>();

						var state = new EffectSamplerState
						{
							Type = stype,
							Value = ReadValue(effect, @base, stateTypeOffset, stateValOffset)
						};

						if(stype == SamplerStateType.Texture)
						{
							effect.Objects[state.Value.ValuesI[0]] = new EffectObject(type);
						}

						values[i] = state;
					}

					value.Values = values;
				}
				else
				{
					uint numObjects = 1;
					if(numElements > 0)
					{
						numObjects = numElements;
					}

					var values = new int[numObjects];

					for(int i=0; i<values.Length; i++)
					{
						var val = valPtr.Read<uint>();
						values[i] = (int)val;

						effect.Objects[val] = new EffectObject(type);
					}

					value.Values = values;
				}
			}
			else if(valClass == SymbolClass.Struct)
			{
				var memberCount = typePtr.Read<uint>();
				value.Type.Members = new SymbolStructMember[memberCount];

				uint structSize = 0;

				for(int i=0; i<value.Type.Members.Length; i++)
				{
					ref var mem = ref value.Type.Members[i];

					mem.Info.ParameterType = typePtr.Read<SymbolType>();
					mem.Info.ParameterClass = typePtr.Read<SymbolClass>();

					var memNameOffset = typePtr.Read<uint>();
					var memSemantic = typePtr.Read<uint>();//Unused
					mem.Name = @base.ReadString(memNameOffset);

					mem.Info.Elements = typePtr.Read<uint>();
					mem.Info.Columns = typePtr.Read<uint>();
					mem.Info.Rows = typePtr.Read<uint>();

					if(mem.Info.ParameterClass < SymbolClass.Scalar || mem.Info.ParameterClass > SymbolClass.MatrixColumns)
					{
						throw new Exception();
					}
					if(mem.Info.ParameterType < SymbolType.Bool || mem.Info.ParameterType > SymbolType.Float)
					{
						throw new Exception();
					}

					mem.Info.Members = null;

					uint memSize = 4 * mem.Info.Rows;
					if(mem.Info.Elements > 0)
					{
						memSize *= mem.Info.Elements;
					}
					structSize += memSize;
				}

				value.Type.Columns = structSize;
				value.Type.Rows = 1;
				var valueCount = structSize;
				if(numElements > 0)
				{
					valueCount *= numElements;
				}

				var values = new float[valueCount];

				uint dstOffset = 0;
				uint srcOffset = 0;

				if(numElements == 0)
				{
					numElements = 1;
				}
				
				for(int i2=0; i2<numElements; i2++)
				{
					for(int j=0; j<value.Type.Members.Length; j++)
					{
						var size = value.Type.Members[j].Info.Rows * value.Type.Members[j].Info.Elements;
						for(int k=0; k<size; k++)
						{
							for(int f=0; f<value.Type.Members[j].Info.Columns; f++)
							{
								values[dstOffset + f] = typePtr.Read<float>(srcOffset*sizeof(float));/* Yes, typeptr. -flibit */
							}
							dstOffset += 1;
							srcOffset += value.Type.Members[j].Info.Columns;
						}
					}
				}

				value.Values = values;
			}

			return value;
		}
	}
}
