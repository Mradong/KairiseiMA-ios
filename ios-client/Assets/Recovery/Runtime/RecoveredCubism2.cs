// Reconstructed Cubism 2.1 managed core for this recovery project.
// Original shipped managed reference: legacy-managed/Live2DUnity.dll, SHA-256 14660a7ae8189909e7c44f0d22e065d6519873d50bf05d726f47dd893910bc33.
// Original SDK copyright: Live2D, 2013. Existing game-owned input; no original DLL is shipped.
// Invalid identifiers normalized by ILSpy; assembly attributes removed; namespace isolated.
// Shader lookup is explicit through the recovery resource bundle and fails for missing shaders.
// This source has not yet been validated by Unity 6000/IL2CPP, iOS or a device.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Kairisei.Recovery.Cubism2
{
	public abstract class ALive2DModel
	{
		public enum ModelDrawMethodVersion
		{
			NOT_SPECIFIED,
			DRAW_2_0,
			DRAW_2_1
		}

		public const int FILE_LOAD_EOF_ERROR = 1;

		public const int FILE_LOAD_VERSION_ERROR = 2;

		protected static int CubismField_3;

		protected ModelImpl CubismField_4;

		protected ModelContext CubismField_5;

		protected int CubismField_6;

		protected bool CubismField_7;

		internal ModelDrawMethodVersion CubismField_8;

		public ALive2DModel()
		{
			CubismField_3++;
			CubismField_5 = new ModelContext(this);
		}

		public void setModelImpl(ModelImpl argument0)
		{
			CubismField_4 = argument0;
		}

		public ModelImpl getModelImpl()
		{
			return CubismField_4;
		}

		public float getCanvasWidth()
		{
			if (CubismField_4 == null)
			{
				return 0f;
			}
			return CubismField_4.getCanvasWidth();
		}

		public float getCanvasHeight()
		{
			if (CubismField_4 == null)
			{
				return 0f;
			}
			return CubismField_4.getCanvasHeight();
		}

		public float getParamFloat(string argument0)
		{
			return CubismField_5.getParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)));
		}

		public void setParamFloat(string argument0, float argument1)
		{
			setParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)), argument1);
		}

		public void setParamFloat(string argument0, float argument1, float argument2)
		{
			setParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)), argument1, argument2);
		}

		public void addToParamFloat(string argument0, float argument1)
		{
			addToParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)), argument1);
		}

		public void addToParamFloat(string argument0, float argument1, float argument2)
		{
			addToParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)), argument1, argument2);
		}

		public void multParamFloat(string argument0, float argument1)
		{
			multParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)), argument1);
		}

		public void multParamFloat(string argument0, float argument1, float argument2)
		{
			multParamFloat(CubismField_5.getParamIndex(ParamID.getID(argument0)), argument1, argument2);
		}

		public void loadParam()
		{
			CubismField_5.loadParam();
		}

		public void saveParam()
		{
			CubismField_5.saveParam();
		}

		public void init()
		{
			CubismField_5.init();
		}

		public virtual void update()
		{
			if (!CubismField_7)
			{
				CubismField_5._3000_3000_0020();
			}
		}

		public int generateModelTextureNo()
		{
			return getDrawParam().generateModelTextureNo();
		}

		public void releaseModelTextureNo(int argument0)
		{
			getDrawParam().releaseModelTextureNo(argument0);
		}

		public abstract void deleteTextures();

		public abstract void draw();

		public static void loadModel_exe(ALive2DModel argument0, string argument1)
		{
			byte[] argument2 = UtFile.loadFile(argument1);
			loadModel_exe(argument0, UtFile.convertStream(argument2));
		}

		public static void loadModel_exe(ALive2DModel argument0, Stream argument1)
		{
			BReader bReader = new BReader(argument1);
			byte b = bReader.readByte();
			byte b2 = bReader.readByte();
			byte b3 = bReader.readByte();
			if (b == 109 && b2 == 111 && b3 == 99)
			{
				int num = bReader.readByte();
				bReader.setFormatVersion(num);
				if (num > 11)
				{
					argument0.CubismField_6 |= 2;
					UtDebug.print("Illegal data version ( available : {0} < loaded . {1} )@ALive2DModel#loadModel()\n", 11, num);
					return;
				}
				ModelImpl modelImpl = (ModelImpl)bReader.readObject();
				if (num >= 8 && bReader.readInt() != -2004318072)
				{
					argument0.CubismField_6 |= 1;
					UtDebug.print("Assert @ALive2DModel#loadModel() / EOF value check\n");
				}
				else
				{
					argument0.setModelImpl(modelImpl);
					argument0.getModelContext().init();
					argument0.CubismField_7 = false;
				}
				return;
			}
			throw new Exception("Invalid version");
		}

		public int getParamIndex(string argument0)
		{
			return CubismField_5.getParamIndex(ParamID.getID(argument0));
		}

		public float getParamFloat(int argument0)
		{
			return CubismField_5.getParamFloat(argument0);
		}

		public void setParamFloat(int argument0, float argument1)
		{
			CubismField_5.setParamFloat(argument0, argument1);
		}

		public void setParamFloat(int argument0, float argument1, float argument2)
		{
			CubismField_5.setParamFloat(argument0, CubismField_5.getParamFloat(argument0) * (1f - argument2) + argument1 * argument2);
		}

		public void addToParamFloat(int argument0, float argument1)
		{
			CubismField_5.setParamFloat(argument0, CubismField_5.getParamFloat(argument0) + argument1);
		}

		public void addToParamFloat(int argument0, float argument1, float argument2)
		{
			CubismField_5.setParamFloat(argument0, CubismField_5.getParamFloat(argument0) + argument1 * argument2);
		}

		public void multParamFloat(int argument0, float argument1)
		{
			CubismField_5.setParamFloat(argument0, CubismField_5.getParamFloat(argument0) * (1f + (argument1 - 1f)));
		}

		public void multParamFloat(int argument0, float argument1, float argument2)
		{
			CubismField_5.setParamFloat(argument0, CubismField_5.getParamFloat(argument0) * (1f + (argument1 - 1f) * argument2));
		}

		public ModelContext getModelContext()
		{
			return CubismField_5;
		}

		public int getErrorFlags()
		{
			return CubismField_6;
		}

		public void setPartsOpacity(string argument0, float argument1)
		{
			int partsDataIndex = CubismField_5.getPartsDataIndex(PartsDataID.getID(argument0));
			setPartsOpacity(partsDataIndex, argument1);
		}

		public void setPartsOpacity(int argument0, float argument1)
		{
			if (argument0 >= 0)
			{
				CubismField_5.setPartsOpacity(argument0, argument1);
			}
		}

		public float getPartsOpacity(string argument0)
		{
			int partsDataIndex = CubismField_5.getPartsDataIndex(PartsDataID.getID(argument0));
			return getPartsOpacity(partsDataIndex);
		}

		public int getPartsDataIndex(string argument0)
		{
			return CubismField_5.getPartsDataIndex(PartsDataID.getID(argument0));
		}

		public int getPartsDataIndex(PartsDataID argument0)
		{
			return CubismField_5.getPartsDataIndex(argument0);
		}

		public float getPartsOpacity(int argument0)
		{
			if (argument0 < 0)
			{
				return 0f;
			}
			return CubismField_5.getPartsOpacity(argument0);
		}

		public abstract DrawParam getDrawParam();

		public int getDrawDataIndex(string argument0)
		{
			return CubismField_5.getDrawDataIndex(DrawDataID.getID(argument0));
		}

		public IDrawData getDrawData(int argument0)
		{
			return CubismField_5.getDrawData(argument0);
		}

		public float[] getTransformedPoints(int argument0)
		{
			if (argument0 < 0)
			{
				return null;
			}
			IDrawContext drawContext = CubismField_5.getDrawContext(argument0);
			if (drawContext is CubismType_15.CubismType_16)
			{
				return ((CubismType_15.CubismType_16)drawContext)._0020();
			}
			return null;
		}

		public ushort[] getIndexArray(int argument0)
		{
			if (argument0 < 0)
			{
				return null;
			}
			IDrawContext drawContext = CubismField_5.getDrawContext(argument0);
			IDrawData drawData = CubismField_5.getDrawData(argument0);
			if (drawContext is CubismType_15.CubismType_16)
			{
				return ((CubismType_15)drawData)._3000();
			}
			return null;
		}

		public void setAvatarParts(string argument0, AvatarPartsItem argument1)
		{
			argument1.init(argument0, this);
		}

		public void releaseAvatarParts(string argument0)
		{
			int partsDataIndex = CubismField_5.getPartsDataIndex(PartsDataID.getID(argument0));
			PartsData partsData = CubismField_5.getPartsData(partsDataIndex);
			foreach (IDrawData drawDatum in partsData.getDrawData())
			{
				if (drawDatum is CubismType_15 cubismType_)
				{
					releaseModelTextureNo(cubismType_._3000_0020_3000());
					getDrawParam()._0020(cubismType_.getDrawDataID().ToString());
				}
			}
			partsData._3000();
		}

		public void setAnisotropy(int argument0)
		{
			if (argument0 < 1)
			{
				argument0 = 1;
			}
			if (9 < argument0)
			{
				argument0 = 9;
			}
			getDrawParam().setAnisotropy(argument0);
		}

		public int getAnisotropy()
		{
			return getDrawParam().getAnisotropy();
		}

		public string toStringAllDrawable()
		{
			return CubismField_4.toStringAllDrawable();
		}

		internal bool _0020(string argument0)
		{
			if (CubismField_4._0020(argument0))
			{
				return false;
			}
			PartsDataID._0020(argument0);
			PartsData partsData = new PartsData();
			partsData.setPartsDataID(PartsDataID.getID(argument0));
			CubismField_4.addPartsData(partsData);
			init();
			return true;
		}

		public void setTextureMap(int argument0, int argument1, float argument2, float argument3, float argument4, float argument5, bool argument6 = false)
		{
			CubismField_5.setTextureMap(argument0, argument1, argument2, argument3, argument4, argument5, argument6);
		}

		public void resetTextureMap()
		{
			foreach (PartsData partsData in CubismField_4.getPartsDataList())
			{
				foreach (IDrawData drawDatum in partsData.getDrawData())
				{
					if (drawDatum.getType() == 2 && drawDatum is CubismType_15 cubismType_)
					{
						cubismType_._3000_0020_0020_3000();
					}
				}
			}
		}

		public void setPremultipliedAlpha(bool argument0)
		{
			getDrawParam().setPremultipliedAlpha(argument0);
		}

		public bool isPremultipliedAlpha()
		{
			return getDrawParam().isPremultipliedAlpha();
		}

		public void setModelDrawMethodVersion(ModelDrawMethodVersion argument0)
		{
			CubismField_8 = argument0;
		}

		public ModelDrawMethodVersion getModelDrawMethodVersion()
		{
			return CubismField_8;
		}

		public void setColorConvertHSL(int argument0, float[] argument1)
		{
			getDrawParam()._3000_0020_0020(argument0, argument1);
		}

		public void setColorConvertMainLine(int argument0, float argument1, float argument2)
		{
			getDrawParam()._3000_3000(argument0, argument1, argument2);
		}
	}
	public class ModelImpl : ISerializableV2
	{
		private static int CubismField_13;

		private ParamDefSet CubismField_14;

		private List<PartsData> CubismField_15;

		private int CubismField_16 = 400;

		private int CubismField_17 = 400;

		internal ModelImpl()
		{
			CubismField_13++;
		}

		internal void _3000()
		{
			if (CubismField_14 == null)
			{
				CubismField_14 = new ParamDefSet();
			}
			if (CubismField_15 == null)
			{
				CubismField_15 = new List<PartsData>();
			}
		}

		public float getCanvasWidth()
		{
			return CubismField_16;
		}

		public float getCanvasHeight()
		{
			return CubismField_17;
		}

		public void readV2(BReader argument0)
		{
			CubismField_14 = (ParamDefSet)argument0.readObject();
			List<object> list = argument0.readObject() as List<object>;
			CubismField_15 = new List<PartsData>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_15.Add((PartsData)list[i]);
			}
			CubismField_16 = argument0.readInt();
			CubismField_17 = argument0.readInt();
		}

		public void addPartsData(PartsData argument0)
		{
			CubismField_15.Add(argument0);
		}

		public List<PartsData> getPartsDataList()
		{
			return CubismField_15;
		}

		public ParamDefSet getParamDefSet()
		{
			return CubismField_14;
		}

		public string toStringAllDrawable()
		{
			string text = "";
			for (int i = 0; i < CubismField_15.Count; i++)
			{
				text = text + "\n" + CubismField_15[i].toStringAllDrawable();
			}
			return text;
		}

		internal bool _0020(string argument0)
		{
			foreach (PartsData item in CubismField_15)
			{
				if (item._0020(argument0))
				{
					return true;
				}
			}
			return false;
		}
	}
	public class ModelContext
	{
		private static bool CubismField_18 = true;

		private const short CubismField_19 = -1;

		private const short CubismField_20 = -1;

		private const bool CubismField_21 = false;

		private const bool CubismField_22 = true;

		private const float CubismField_23 = -1000000f;

		private const float CubismField_24 = 1000000f;

		private bool CubismField_25 = true;

		private ALive2DModel CubismField_26;

		private int CubismField_27 = -1;

		private const int CubismField_28 = 32;

		private int CubismField_29;

		private ParamID[] CubismField_30 = new ParamID[32];

		private float[] CubismField_31 = new float[32];

		private float[] CubismField_32 = new float[32];

		private float[] CubismField_33 = new float[32];

		private float[] CubismField_34 = new float[32];

		private float[] CubismField_35 = new float[32];

		private bool[] CubismField_36 = new bool[32];

		private List<IBaseData> CubismField_37 = new List<IBaseData>();

		private List<IDrawData> CubismField_38 = new List<IDrawData>();

		private Dictionary<DrawDataID, IDrawData> CubismField_39;

		private List<PartsData> CubismField_40 = new List<PartsData>();

		private List<IBaseContext> CubismField_41 = new List<IBaseContext>();

		private List<IDrawContext> CubismField_42 = new List<IDrawContext>();

		private List<PartsData.PartsDataContext> CubismField_43 = new List<PartsData.PartsDataContext>();

		private short[] CubismField_44;

		private short[] CubismField_45;

		private short[] CubismField_46;

		internal CubismType_43 CubismField_47;

		private short[] CubismField_48 = new short[65];

		private float[] CubismField_49 = new float[10];

		private const bool CubismField_50 = false;

		internal ModelContext(ALive2DModel argument0)
		{
			CubismField_26 = argument0;
		}

		internal void _3000_0020_3000()
		{
			if (CubismField_47 != null)
			{
				CubismField_47._3000_3000_0020();
			}
		}

		public int getDrawDataIndex(DrawDataID argument0)
		{
			for (int num = CubismField_38.Count - 1; num >= 0; num--)
			{
				if (CubismField_38[num] != null && CubismField_38[num].getDrawDataID() == argument0)
				{
					return num;
				}
			}
			return -1;
		}

		public IDrawData getDrawData(DrawDataID argument0)
		{
			if (CubismField_39 == null)
			{
				CubismField_39 = new Dictionary<DrawDataID, IDrawData>();
				int count = CubismField_38.Count;
				for (int i = 0; i < count; i++)
				{
					IDrawData drawData = CubismField_38[i];
					DrawDataID drawDataID = drawData.getDrawDataID();
					if (drawDataID != null)
					{
						CubismField_39.Add(drawDataID, drawData);
					}
				}
			}
			return CubismField_39[argument0];
		}

		public IDrawData getDrawData(int argument0)
		{
			if (argument0 < CubismField_38.Count)
			{
				return CubismField_38[argument0];
			}
			return null;
		}

		private void _3000_0020_0020()
		{
			CubismField_37.Clear();
			CubismField_38.Clear();
			CubismField_40.Clear();
			if (CubismField_39 != null)
			{
				CubismField_39.Clear();
			}
			CubismField_41.Clear();
			CubismField_42.Clear();
			CubismField_43.Clear();
			for (int i = 0; i < CubismField_29; i++)
			{
				CubismField_30[i] = null;
			}
			CubismField_29 = 0;
		}

		public void init()
		{
			CubismField_27++;
			if (CubismField_40.Count > 0)
			{
				_3000_0020_0020();
			}
			ModelImpl modelImpl = CubismField_26.getModelImpl();
			List<PartsData> partsDataList = modelImpl.getPartsDataList();
			int count = partsDataList.Count;
			List<IBaseData> list = new List<IBaseData>();
			List<IBaseContext> list2 = new List<IBaseContext>();
			for (int i = 0; i < count; i++)
			{
				PartsData partsData = partsDataList[i];
				CubismField_40.Add(partsData);
				CubismField_43.Add(partsData.init(this));
				List<IBaseData> baseData = partsData.getBaseData();
				int count2 = baseData.Count;
				for (int j = 0; j < count2; j++)
				{
					list.Add(baseData[j]);
				}
				for (int k = 0; k < count2; k++)
				{
					IBaseContext baseContext = baseData[k].init(this);
					baseContext.setPartsIndex(i);
					list2.Add(baseContext);
				}
				List<IDrawData> drawData = partsData.getDrawData();
				int count3 = drawData.Count;
				for (int l = 0; l < count3; l++)
				{
					IDrawData drawData2 = drawData[l];
					IDrawContext drawContext = drawData2.init(this);
					drawContext.partsIndex = i;
					CubismField_38.Add(drawData2);
					CubismField_42.Add(drawContext);
				}
			}
			int count4 = list.Count;
			BaseDataID baseDataID = BaseDataID.DST_BASE_ID();
			bool flag;
			do
			{
				flag = false;
				for (int m = 0; m < count4; m++)
				{
					IBaseData baseData2 = list[m];
					if (baseData2 != null)
					{
						BaseDataID targetBaseDataID = baseData2.getTargetBaseDataID();
						if (targetBaseDataID == null || targetBaseDataID == baseDataID || getBaseDataIndex(targetBaseDataID) >= 0)
						{
							CubismField_37.Add(baseData2);
							CubismField_41.Add(list2[m]);
							list[m] = null;
							flag = true;
						}
					}
				}
			}
			while (flag);
			Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
			for (int n = 0; n < CubismField_38.Count; n++)
			{
				IDrawData drawData3 = CubismField_38[n];
				string text = drawData3.getDrawDataID().ToString();
				if (dictionary.ContainsKey(text))
				{
					string text2 = text;
					do
					{
						text2 = "_" + text2;
					}
					while (dictionary.ContainsKey(text2));
					UtDebug.error("Duplicated Drawable Object ID : " + text);
					drawData3.setDrawDataID(DrawDataID.getID(text2));
					text = text2;
				}
				dictionary.Add(text, value: true);
			}
			dictionary.Clear();
			for (int num = 0; num < CubismField_37.Count; num++)
			{
				string text3 = CubismField_37[num].getBaseDataID().ToString();
				if (dictionary.ContainsKey(text3))
				{
					UtDebug.error("Duplicated Deformer ID : " + text3);
				}
				dictionary.Add(text3, value: true);
			}
			ParamDefSet paramDefSet = modelImpl.getParamDefSet();
			if (paramDefSet != null)
			{
				List<ParamDefFloat> paramDefFloatList = paramDefSet.getParamDefFloatList();
				if (paramDefFloatList != null)
				{
					int count5 = paramDefFloatList.Count;
					for (int num2 = 0; num2 < count5; num2++)
					{
						ParamDefFloat paramDefFloat = paramDefFloatList[num2];
						if (paramDefFloat != null)
						{
							addFloatParam(paramDefFloat.getParamID(), paramDefFloat.getDefaultValue(), paramDefFloat.getMinValue(), paramDefFloat.getMaxValue());
						}
					}
				}
			}
			if (DrawParam.CubismField_54 == DrawParam.ClippingMethod.BufferPre)
			{
				CubismField_47 = new CubismType_43();
				CubismField_47._3000_0020_0020(this, CubismField_38, CubismField_42);
			}
			CubismField_25 = true;
		}

		internal bool _3000_3000_0020()
		{
			int num = CubismField_31.Length;
			for (int i = 0; i < num; i++)
			{
				if (CubismField_31[i] != CubismField_32[i])
				{
					CubismField_36[i] = true;
					CubismField_32[i] = CubismField_31[i];
				}
			}
			bool result = false;
			int count = CubismField_37.Count;
			int count2 = CubismField_38.Count;
			int num2 = CubismType_19._3000_3000();
			int num3 = CubismType_19._3000_0020() - num2 + 1;
			if (CubismField_44 == null || CubismField_44.Length < num3)
			{
				CubismField_44 = new short[num3];
				CubismField_45 = new short[num3];
			}
			for (int j = 0; j < num3; j++)
			{
				CubismField_44[j] = -1;
				CubismField_45[j] = -1;
			}
			if (CubismField_46 == null || CubismField_46.Length < count2)
			{
				CubismField_46 = new short[count2];
			}
			for (int k = 0; k < count2; k++)
			{
				CubismField_46[k] = -1;
			}
			Exception ex = null;
			for (int l = 0; l < count; l++)
			{
				IBaseData baseData = CubismField_37[l];
				IBaseContext argument = CubismField_41[l];
				try
				{
					baseData.setupInterpolate(this, argument);
					baseData.setupTransform(this, argument);
				}
				catch (Exception ex2)
				{
					if (ex == null)
					{
						ex = ex2;
					}
				}
			}
			if (ex != null && CubismField_18)
			{
				UtDebug.print("{0} / firstExceptionBase\t\t\t\t\t@@ModelContext\n", ex.ToString());
			}
			Exception ex3 = null;
			for (int m = 0; m < count2; m++)
			{
				IDrawData drawData = CubismField_38[m];
				IDrawContext drawContext = CubismField_42[m];
				try
				{
					drawData.setupInterpolate(this, drawContext);
					if (!drawContext.isParamOutside())
					{
						drawData.setupTransform(this, drawContext);
						int num4 = drawData.getDrawOrder(this, drawContext) - num2;
						int num5;
						try
						{
							num5 = CubismField_45[num4];
						}
						catch (Exception ex4)
						{
							UtDebug.print("error :: {0} / {1} \t\t\t\t@@ModelContext\n", ex4.Message, drawData.getDrawDataID().ToString());
							num4 = drawData.getDrawOrder(this, drawContext) - num2;
							goto end_IL_01a3;
						}
						if (num5 == -1)
						{
							CubismField_44[num4] = (short)m;
						}
						else
						{
							CubismField_46[num5] = (short)m;
						}
						CubismField_45[num4] = (short)m;
					}
					end_IL_01a3:;
				}
				catch (Exception ex5)
				{
					if (ex3 == null)
					{
						ex3 = ex5;
						Live2D._0020(4000);
					}
				}
			}
			if (ex3 != null && CubismField_18)
			{
				UtDebug.print("{0} / firstExceptionDraw\t\t\t\t\t@@ModelContext\n", ex3.Message);
			}
			for (int num6 = CubismField_36.Length - 1; num6 >= 0; num6--)
			{
				CubismField_36[num6] = false;
			}
			CubismField_25 = false;
			return result;
		}

		public void preDraw(DrawParam argument0)
		{
			if (CubismField_44 == null)
			{
				UtDebug.error("model.preDraw() 前に model.update() が呼ばれていない可能性があります");
			}
			else if (DrawParam.CubismField_54 == DrawParam.ClippingMethod.BufferPre && CubismField_47 != null)
			{
				argument0.setupDraw();
				CubismField_47._3000_0020_0020_0020(this, (CubismType_42)argument0);
			}
		}

		public void draw(DrawParam argument0)
		{
			if (CubismField_44 == null)
			{
				UtDebug.error("model.draw() 前に model.update() が呼ばれていない可能性があります");
				return;
			}
			int num = CubismField_44.Length;
			argument0.setupDraw();
			for (int i = 0; i < num; i++)
			{
				int num2 = CubismField_44[i];
				if (num2 == -1)
				{
					continue;
				}
				while (true)
				{
					IDrawData drawData = CubismField_38[num2];
					IDrawContext drawContext = CubismField_42[num2];
					if (drawContext.isAvailable())
					{
						int partsIndex = drawContext.partsIndex;
						PartsData.PartsDataContext partsDataContext = CubismField_43[partsIndex];
						drawContext.partsOpacity = partsDataContext.getPartsOpacity();
						drawData.preDraw(argument0, this, drawContext);
						drawData.draw(argument0, this, drawContext);
					}
					int num3 = CubismField_46[num2];
					if (num3 <= num2 || num3 == -1)
					{
						break;
					}
					num2 = num3;
				}
			}
			argument0.drawLast();
		}

		public int getParamIndex(ParamID argument0)
		{
			for (int i = 0; i < CubismField_29; i++)
			{
				if (CubismField_30[i] == argument0)
				{
					return i;
				}
			}
			return addFloatParam(argument0, 0f, -1000000f, 1000000f);
		}

		public int getBaseIndex(BaseDataID argument0)
		{
			return getBaseDataIndex(argument0);
		}

		public int getBaseDataIndex(BaseDataID argument0)
		{
			for (int num = CubismField_37.Count - 1; num >= 0; num--)
			{
				if (CubismField_37[num] != null && CubismField_37[num].getBaseDataID() == argument0)
				{
					return num;
				}
			}
			return -1;
		}

		internal float[] _0020(float[] argument0, int argument1)
		{
			float[] array = new float[argument1];
			Array.Copy(argument0, 0, array, 0, argument0.Length);
			return array;
		}

		public int addFloatParam(ParamID argument0, float argument1, float argument2, float argument3)
		{
			if (CubismField_29 >= CubismField_30.Length)
			{
				int num = CubismField_30.Length;
				ParamID[] array = new ParamID[num * 2];
				Array.Copy(CubismField_30, 0, array, 0, num);
				CubismField_30 = array;
				CubismField_31 = _0020(CubismField_31, num * 2);
				CubismField_32 = _0020(CubismField_32, num * 2);
				CubismField_33 = _0020(CubismField_33, num * 2);
				CubismField_34 = _0020(CubismField_34, num * 2);
				bool[] array2 = new bool[num * 2];
				Array.Copy(CubismField_36, 0, array2, 0, num);
				CubismField_36 = array2;
			}
			CubismField_30[CubismField_29] = argument0;
			CubismField_31[CubismField_29] = argument1;
			CubismField_32[CubismField_29] = argument1;
			CubismField_33[CubismField_29] = argument2;
			CubismField_34[CubismField_29] = argument3;
			CubismField_36[CubismField_29] = true;
			return CubismField_29++;
		}

		public void setBaseData(int argument0, IBaseData argument1)
		{
			CubismField_37[argument0] = argument1;
		}

		public void setParamFloat(int argument0, float argument1)
		{
			if (float.IsNaN(argument1))
			{
				argument1 = 0f;
			}
			if (argument1 < CubismField_33[argument0])
			{
				argument1 = CubismField_33[argument0];
			}
			if (argument1 > CubismField_34[argument0])
			{
				argument1 = CubismField_34[argument0];
			}
			CubismField_31[argument0] = argument1;
		}

		public void loadParam()
		{
			int num = CubismField_31.Length;
			if (num > CubismField_35.Length)
			{
				num = CubismField_35.Length;
			}
			Array.Copy(CubismField_35, 0, CubismField_31, 0, num);
		}

		public void saveParam()
		{
			int num = CubismField_31.Length;
			if (num > CubismField_35.Length)
			{
				CubismField_35 = new float[num];
			}
			Array.Copy(CubismField_31, 0, CubismField_35, 0, num);
		}

		public int getInitVersion()
		{
			return CubismField_27;
		}

		public bool requireSetup()
		{
			return CubismField_25;
		}

		internal bool _3000_3000(int argument0)
		{
			return CubismField_36[argument0];
		}

		internal short[] _3000()
		{
			return CubismField_48;
		}

		internal float[] _3000_0020()
		{
			return CubismField_49;
		}

		public IBaseData getBaseData(int argument0)
		{
			return CubismField_37[argument0];
		}

		public float getParamFloat(int argument0)
		{
			return CubismField_31[argument0];
		}

		public float getParamMax(int argument0)
		{
			return CubismField_34[argument0];
		}

		public float getParamMin(int argument0)
		{
			return CubismField_33[argument0];
		}

		public void setPartsOpacity(int argument0, float argument1)
		{
			CubismField_43[argument0].setPartsOpacity(argument1);
		}

		public float getPartsOpacity(int argument0)
		{
			return CubismField_43[argument0].getPartsOpacity();
		}

		public int getPartsDataIndex(PartsDataID argument0)
		{
			for (int num = CubismField_40.Count - 1; num >= 0; num--)
			{
				if (CubismField_40[num] != null && CubismField_40[num].getPartsDataID() == argument0)
				{
					return num;
				}
			}
			return -1;
		}

		public IBaseContext getBaseContext(int argument0)
		{
			return CubismField_41[argument0];
		}

		public IDrawContext getDrawContext(int argument0)
		{
			return CubismField_42[argument0];
		}

		public PartsData.PartsDataContext getPartsContext(int argument0)
		{
			return CubismField_43[argument0];
		}

		internal void _3000_3000_3000(float argument0, float argument1)
		{
			int num = CubismField_44.Length;
			float num2 = argument0;
			for (int i = 0; i < num; i++)
			{
				int num3 = CubismField_44[i];
				if (num3 == -1)
				{
					continue;
				}
				while (true)
				{
					IDrawContext drawContext = CubismField_42[num3];
					if (drawContext.isAvailable())
					{
						drawContext.getSrcPtr().setZ_TestImpl(this, drawContext, num2);
						num2 += argument1;
					}
					int num4 = CubismField_46[num3];
					if (num4 <= num3 || num4 == -1)
					{
						break;
					}
					num3 = num4;
				}
			}
		}

		public PartsData getPartsData(int argument0)
		{
			if (argument0 < 0)
			{
				return null;
			}
			return CubismField_40[argument0];
		}

		public void setTextureMap(int argument0, int argument1, float argument2, float argument3, float argument4, float argument5, bool argument6)
		{
			int count = CubismField_42.Count;
			for (int i = 0; i < count; i++)
			{
				if (CubismField_38[i].getType() == 2 && CubismField_38[i] is CubismType_15 cubismType_ && cubismType_._3000_0020_3000() == argument0)
				{
					IDrawContext argument7 = CubismField_42[i];
					cubismType_._3000_0020_0020_0020(argument7, argument1, argument2, argument3, argument4, argument5, argument6);
					cubismType_._0020(argument7);
				}
			}
		}

		public bool isDrawMethodVersion_2_0()
		{
			switch (Live2D.getDrawMethodVersion())
			{
			case Live2D.DrawMethodVersion.DEFAULT_2_1:
				return CubismField_26.getModelDrawMethodVersion() == ALive2DModel.ModelDrawMethodVersion.DRAW_2_0;
			case Live2D.DrawMethodVersion.DEFAULT_2_0:
				return CubismField_26.getModelDrawMethodVersion() != ALive2DModel.ModelDrawMethodVersion.DRAW_2_1;
			case Live2D.DrawMethodVersion.FORCE_2_0:
				return true;
			default:
				_ = 1;
				return false;
			}
		}

		public ALive2DModel getModel()
		{
			return CubismField_26;
		}
	}
	public class ParamID : ID
	{
		private static Dictionary<string, ParamID> CubismField_51 = new Dictionary<string, ParamID>();

		private static readonly object CubismField_52 = new object();

		private ParamID()
		{
		}

		private ParamID(string argument0)
		{
			CubismField_150 = argument0;
		}

		public ParamID createIDForSerialize()
		{
			return new ParamID();
		}

		internal static void _0020()
		{
			CubismField_51.Clear();
		}

		public static ParamID getID(string argument0)
		{
			if (CubismField_51.ContainsKey(argument0))
			{
				return CubismField_51[argument0];
			}
			ParamID paramID = new ParamID(argument0);
			CubismField_51.Add(argument0, paramID);
			return paramID;
		}

		public static ParamID getIDAsync(string argument0)
		{
			Monitor.Enter(CubismField_52);
			if (CubismField_51.ContainsKey(argument0))
			{
				Monitor.Exit(CubismField_52);
				return CubismField_51[argument0];
			}
			ParamID paramID = new ParamID(argument0);
			CubismField_51.Add(argument0, paramID);
			Monitor.Exit(CubismField_52);
			return paramID;
		}
	}
	public abstract class DrawParam
	{
		public enum ClippingMethod
		{
			Ignore,
			Alpha,
			BufferSeq,
			BufferPre,
			Mesh
		}

		public class TextureInfo
		{
			public float r = 1f;

			public float g = 1f;

			public float b = 1f;

			public float scale = 1f;
		}

		public const int DEFAULT_FIXED_TEXTURE_COUNT = 32;

		internal static ClippingMethod CubismField_54 = ClippingMethod.BufferPre;

		public const int CLIPPING_PROCESS_NONE = 0;

		public const int CLIPPING_PROCESS_OVERWRITE_ALPHA = 1;

		public const int CLIPPING_PROCESS_MULTIPLY_ALPHA = 2;

		public const int CLIPPING_PROCESS_DRAW = 3;

		public const int CLIPPING_PROCESS_CLEAR_ALPHA = 4;

		public int clippingProcess;

		protected int CubismField_61 = 32;

		protected float CubismField_62 = 1f;

		protected float CubismField_63 = 1f;

		protected float CubismField_64 = 1f;

		protected float CubismField_65 = 1f;

		protected bool CubismField_66;

		protected bool CubismField_67;

		protected bool CubismField_68;

		protected float[] CubismField_69 = new float[16];

		protected int CubismField_70 = -1;

		protected List<TextureInfo> CubismField_71 = new List<TextureInfo>();

		protected Dictionary<int, ColorConverter> CubismField_72 = new Dictionary<int, ColorConverter>();

		protected int CubismField_73 = -1;

		protected int CubismField_74;

		internal int CubismField_75 = -1;

		protected bool CubismField_76;

		public DrawParam()
		{
		}

		public virtual void releaseDrawParam_testImpl()
		{
		}

		public virtual void setupDraw()
		{
		}

		public abstract void drawTexture(int argument0, int argument1, ushort[] argument2, float[] argument3, float[] argument4, float argument5, int argument6);

		public abstract void drawLast();

		public virtual int generateModelTextureNo()
		{
			return -1;
		}

		public virtual void releaseModelTextureNo(int argument0)
		{
		}

		public void setBaseColor(float argument0, float argument1, float argument2, float argument3)
		{
			if (argument0 < 0f)
			{
				argument0 = 0f;
			}
			else if (argument0 > 1f)
			{
				argument0 = 1f;
			}
			if (argument1 < 0f)
			{
				argument1 = 0f;
			}
			else if (argument1 > 1f)
			{
				argument1 = 1f;
			}
			if (argument2 < 0f)
			{
				argument2 = 0f;
			}
			else if (argument2 > 1f)
			{
				argument2 = 1f;
			}
			if (argument3 < 0f)
			{
				argument3 = 0f;
			}
			else if (argument3 > 1f)
			{
				argument3 = 1f;
			}
			CubismField_62 = argument0;
			CubismField_63 = argument1;
			CubismField_64 = argument2;
			CubismField_65 = argument3;
		}

		public void setPremultipliedAlpha(bool argument0)
		{
			CubismField_76 = argument0;
		}

		public bool isPremultipliedAlpha()
		{
			return CubismField_76;
		}

		public virtual void setCurrentDDID(DrawDataID argument0)
		{
		}

		public void setLockCulling(bool argument0)
		{
			CubismField_68 = argument0;
		}

		public void setCulling(bool argument0)
		{
			if (!CubismField_68)
			{
				CubismField_67 = argument0;
			}
		}

		public bool getCulling()
		{
			return CubismField_67;
		}

		public void setMatrix(float[] argument0)
		{
			if (argument0.Length == 16)
			{
				for (int i = 0; i < 16; i++)
				{
					CubismField_69[i] = argument0[i];
				}
			}
		}

		public float[] getMatrix()
		{
			return CubismField_69;
		}

		public void setAnisotropy(int argument0)
		{
			CubismField_70 = argument0;
		}

		public int getAnisotropy()
		{
			if (CubismField_70 < 0)
			{
				return 1;
			}
			return CubismField_70;
		}

		public void setTextureColor(int argument0, float argument1, float argument2, float argument3, float argument4)
		{
			if (argument0 < CubismField_71.Count)
			{
				TextureInfo textureInfo = CubismField_71[argument0];
				textureInfo.r = argument1;
				textureInfo.g = argument2;
				textureInfo.b = argument3;
				textureInfo.scale = argument4;
				CubismField_66 = true;
			}
		}

		protected void createTextureInfo(int argument0)
		{
			while (CubismField_71.Count < argument0 + 1)
			{
				CubismField_71.Add(new TextureInfo());
			}
		}

		internal void _3000_3000_0020(int argument0)
		{
			CubismField_73 = argument0;
		}

		internal virtual void _0020(string argument0)
		{
		}

		internal void _3000_0020_3000(int argument0, ColorConvertMethod argument1)
		{
			_3000(argument0).data.methodType = argument1;
		}

		internal ColorConvertMethod _3000_0020(int argument0)
		{
			return _3000(argument0).data.methodType;
		}

		internal virtual void _3000_0020_0020(int argument0, float[] argument1)
		{
			ColorConverter colorConverter = _3000(argument0);
			colorConverter.data.methodType = ColorConvertMethod.HSL;
			colorConverter.data.setMatrix(argument1);
		}

		internal virtual void _3000_3000(int argument0, float argument1, float argument2)
		{
			ColorConverter colorConverter = _3000(argument0);
			colorConverter.data.methodType = ColorConvertMethod.HSL;
			colorConverter.data.mainLineHigh = argument2;
			colorConverter.data.mainLineLow = argument1;
		}

		internal virtual ColorConverter _3000(int argument0)
		{
			if (!CubismField_72.ContainsKey(argument0))
			{
				CubismField_72[argument0] = new ColorConverter();
			}
			return CubismField_72[argument0];
		}

		internal string _3000_3000_3000()
		{
			string text = "";
			foreach (KeyValuePair<int, ColorConverter> item in CubismField_72)
			{
				text = text + "\n" + item.ToString();
			}
			return text;
		}
	}
	public class UtFile
	{
		public static byte[] loadFile(string argument0)
		{
			try
			{
				BinaryReader binaryReader = new BinaryReader(File.OpenRead(argument0));
				byte[] result = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
				binaryReader.Close();
				return result;
			}
			catch (Exception)
			{
				return ((TextAsset)Resources.Load(argument0, typeof(TextAsset))).bytes;
			}
		}

		public static Stream convertStream(byte[] argument0)
		{
			return new MemoryStream(argument0);
		}

		public static byte[] load(Stream argument0)
		{
			int num = (int)argument0.Length;
			byte[] array = new byte[num];
			argument0.Read(array, 0, num);
			return array;
		}
	}
	public class BReader
	{
		private BinaryReader CubismField_77;

		private byte[] CubismField_78 = new byte[8];

		private byte[] CubismField_79 = new byte[8];

		private byte[] CubismField_80 = new byte[4];

		private byte[] CubismField_81 = new byte[2];

		private byte[] CubismField_82 = new byte[1000];

		private int CubismField_83;

		private int CubismField_84;

		private int CubismField_85;

		private List<object> CubismField_86 = new List<object>();

		public BReader(Stream argument0)
		{
			BinaryReader binaryReader = (CubismField_77 = new BinaryReader(argument0));
		}

		public BReader(Stream argument0, int argument1)
		{
			CubismField_85 = argument1;
			CubismField_77 = new BinaryReader(argument0);
		}

		public int getFormatVersion()
		{
			return CubismField_85;
		}

		public void setFormatVersion(int argument0)
		{
			CubismField_85 = argument0;
		}

		public int readNum()
		{
			return bytesToNum(CubismField_77);
		}

		public static int bytesToNum(BinaryReader argument0)
		{
			int num = argument0.ReadByte();
			if ((num & 0x80) == 0)
			{
				return num & 0xFF;
			}
			int num2;
			if (((num2 = argument0.ReadByte()) & 0x80) == 0)
			{
				return ((num & 0x7F) << 7) | (num2 & 0x7F);
			}
			int num3;
			if (((num3 = argument0.ReadByte()) & 0x80) == 0)
			{
				return ((num & 0x7F) << 14) | ((num2 & 0x7F) << 7) | (num3 & 0xFF);
			}
			int num4;
			if (((num4 = argument0.ReadByte()) & 0x80) == 0)
			{
				return ((num & 0x7F) << 21) | ((num2 & 0x7F) << 14) | ((num3 & 0x7F) << 7) | (num4 & 0xFF);
			}
			throw new Exception("未対応 _");
		}

		public double readDouble()
		{
			_0020();
			CubismField_77.Read(CubismField_79, 0, 8);
			if (BitConverter.IsLittleEndian)
			{
				CubismField_78[0] = CubismField_79[7];
				CubismField_78[1] = CubismField_79[6];
				CubismField_78[2] = CubismField_79[5];
				CubismField_78[3] = CubismField_79[4];
				CubismField_78[4] = CubismField_79[3];
				CubismField_78[5] = CubismField_79[2];
				CubismField_78[6] = CubismField_79[1];
				CubismField_78[7] = CubismField_79[0];
				return BitConverter.ToDouble(CubismField_78, 0);
			}
			return BitConverter.ToDouble(CubismField_79, 0);
		}

		public float readFloat()
		{
			_0020();
			CubismField_77.Read(CubismField_80, 0, 4);
			if (BitConverter.IsLittleEndian)
			{
				CubismField_78[0] = CubismField_80[3];
				CubismField_78[1] = CubismField_80[2];
				CubismField_78[2] = CubismField_80[1];
				CubismField_78[3] = CubismField_80[0];
				return BitConverter.ToSingle(CubismField_78, 0);
			}
			return BitConverter.ToSingle(CubismField_80, 0);
		}

		public int readInt()
		{
			_0020();
			CubismField_77.Read(CubismField_80, 0, 4);
			if (BitConverter.IsLittleEndian)
			{
				CubismField_78[0] = CubismField_80[3];
				CubismField_78[1] = CubismField_80[2];
				CubismField_78[2] = CubismField_80[1];
				CubismField_78[3] = CubismField_80[0];
				return BitConverter.ToInt32(CubismField_78, 0);
			}
			return BitConverter.ToInt32(CubismField_80, 0);
		}

		public byte readByte()
		{
			_0020();
			return (byte)CubismField_77.Read();
		}

		public short readShort()
		{
			_0020();
			CubismField_77.Read(CubismField_81, 0, 2);
			if (BitConverter.IsLittleEndian)
			{
				CubismField_78[0] = CubismField_79[1];
				CubismField_78[1] = CubismField_79[0];
				return BitConverter.ToInt16(CubismField_78, 0);
			}
			return BitConverter.ToInt16(CubismField_81, 0);
		}

		public long readLong()
		{
			_0020();
			CubismField_77.Read(CubismField_79, 0, 8);
			if (BitConverter.IsLittleEndian)
			{
				CubismField_78[0] = CubismField_79[7];
				CubismField_78[1] = CubismField_79[6];
				CubismField_78[2] = CubismField_79[5];
				CubismField_78[3] = CubismField_79[4];
				CubismField_78[4] = CubismField_79[3];
				CubismField_78[5] = CubismField_79[2];
				CubismField_78[6] = CubismField_79[1];
				CubismField_78[7] = CubismField_79[0];
				return BitConverter.ToInt64(CubismField_78, 0);
			}
			return BitConverter.ToInt64(CubismField_79, 0);
		}

		public bool readBoolean()
		{
			_0020();
			return CubismField_77.Read() != 0;
		}

		public string readUTF8()
		{
			_0020();
			int num = readNum();
			if (CubismField_82.Length < num)
			{
				CubismField_82 = new byte[num];
			}
			CubismField_77.Read(CubismField_82, 0, num);
			return Encoding.UTF8.GetString(CubismField_82, 0, num);
		}

		public int[] readArrayInt()
		{
			_0020();
			int num = readNum();
			int[] array = new int[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = readInt();
			}
			return array;
		}

		public float[] readArrayFloat()
		{
			_0020();
			int num = readNum();
			float[] array = new float[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = readFloat();
			}
			return array;
		}

		public double[] readArrayDouble()
		{
			_0020();
			int num = readNum();
			double[] array = new double[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = readDouble();
			}
			return array;
		}

		public object readObject()
		{
			return readObject_exe1(-1);
		}

		public object readObject_exe1(int argument0)
		{
			_0020();
			if (argument0 < 0)
			{
				argument0 = readNum();
			}
			if (argument0 == 33)
			{
				int num = readInt();
				if (0 <= num && num < CubismField_86.Count)
				{
					return CubismField_86[num];
				}
				throw new Exception("illegal refNo @Breader");
			}
			object obj = readObject_exe2(argument0);
			CubismField_86.Add(obj);
			return obj;
		}

		public object readObject_exe2(int argument0)
		{
			if (argument0 == 0)
			{
				return null;
			}
			if (argument0 == 50)
			{
				return DrawDataID.getID(readUTF8());
			}
			if (argument0 == 51)
			{
				return BaseDataID.getID(readUTF8());
			}
			if (argument0 == 134)
			{
				return PartsDataID.getID(readUTF8());
			}
			if (argument0 == 60)
			{
				return ParamID.getID(readUTF8());
			}
			if (argument0 >= 48)
			{
				ISerializableV2 serializableV = (ISerializableV2)CubismType_62._0020(argument0);
				if (serializableV != null)
				{
					serializableV.readV2(this);
					return serializableV;
				}
				return null;
			}
			switch (argument0)
			{
			case 1:
				return readUTF8();
			case 10:
				return new LDColor(readInt(), argument1: true);
			case 11:
				return new LDRectF((float)readDouble(), (float)readDouble(), (float)readDouble(), (float)readDouble());
			case 12:
				return new LDRectF(readFloat(), readFloat(), readFloat(), readFloat());
			case 13:
				return new LDPointF((float)readDouble(), (float)readDouble());
			case 14:
				return new LDPointF(readFloat(), readFloat());
			case 15:
			{
				int num = readNum();
				List<object> list = new List<object>(num);
				for (int i = 0; i < num; i++)
				{
					list.Add(readObject());
				}
				return list;
			}
			case 17:
				return new CubismType_34((float)readDouble(), (float)readDouble(), (float)readDouble(), (float)readDouble(), (float)readDouble(), (float)readDouble());
			case 21:
				return new LDRect(readInt(), readInt(), readInt(), readInt());
			case 22:
				return new LDPoint(readInt(), readInt());
			case 23:
				throw new Exception("未実装 _");
			case 16:
			case 25:
				return readArrayInt();
			case 26:
				return readArrayDouble();
			case 27:
				return readArrayFloat();
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 18:
			case 19:
			case 20:
			case 24:
			case 28:
				throw new Exception("not impl : readObject() of 2-9 ,18,19,20,24,28");
			default:
				throw new Exception("not impl : readObject() NO DEF");
			}
		}

		public bool readBit()
		{
			if (CubismField_83 == 0)
			{
				CubismField_84 = CubismField_77.ReadByte();
			}
			else if (CubismField_83 == 8)
			{
				CubismField_84 = CubismField_77.ReadByte();
				CubismField_83 = 0;
			}
			return ((CubismField_84 >> 7 - CubismField_83++) & 1) == 1;
		}

		private void _0020()
		{
			if (CubismField_83 != 0)
			{
				CubismField_83 = 0;
			}
		}
	}
	public class UtDebug
	{
		internal class CubismType_93
		{
			internal string CubismField_571;

			internal double CubismField_572;
		}

		private static Dictionary<string, CubismType_93> CubismField_87 = new Dictionary<string, CubismType_93>();

		public static void start(string argument0)
		{
			if (!CubismField_87.ContainsKey(argument0))
			{
				CubismField_87.Add(argument0, new CubismType_93());
			}
			CubismType_93 cubismType_ = CubismField_87[argument0];
			if (cubismType_ == null)
			{
				cubismType_ = new CubismType_93();
				cubismType_.CubismField_571 = argument0;
				CubismField_87.Add(argument0, cubismType_);
			}
			cubismType_.CubismField_572 = UtSystem.getSystemTimeMSec();
		}

		public static void dump(string argument0)
		{
			CubismType_93 cubismType_ = CubismField_87[argument0];
			if (cubismType_ != null)
			{
				double num = (double)UtSystem.getSystemTimeMSec() - cubismType_.CubismField_572;
				print("{0} :: {1}ms\n", argument0, num);
			}
		}

		public static void error(string argument0, params object[] argument1)
		{
			print(argument0 + "\n", argument1);
		}

		public static void print(string argument0, params object[] argument1)
		{
			MonoBehaviour.print(string.Format(argument0, argument1));
		}

		public static void println(string argument0, params object[] argument1)
		{
			MonoBehaviour.print(string.Format(argument0, argument1));
		}

		public static void dumpByte(byte[] argument0, int argument1)
		{
			for (int i = 0; i < argument1; i++)
			{
				if (i % 16 == 0 && i > 0)
				{
					print("\n");
				}
				else if (i % 8 == 0 && i > 0)
				{
					print("  ");
				}
				print("%02X ", argument0[i] & 0xFF);
			}
			print("\n");
		}

		public static void printVectorUShort(string argument0, short[] argument1, string argument2)
		{
			print("%s＼n", argument0);
			int num = argument1.Length;
			for (int i = 0; i < num; i++)
			{
				print("%5d", argument1[i]);
				print("%s＼n", argument2);
				print(",");
			}
			print("\n");
		}
	}
	public class PartsDataID : ID
	{
		private static Dictionary<string, PartsDataID> CubismField_88 = new Dictionary<string, PartsDataID>();

		private static readonly object CubismField_89 = new object();

		private PartsDataID()
		{
		}

		private PartsDataID(string argument0)
		{
			CubismField_150 = argument0;
		}

		public PartsDataID createIDForSerialize()
		{
			return new PartsDataID();
		}

		internal static void _3000_0020()
		{
			CubismField_88.Clear();
		}

		public static PartsDataID getID(string argument0)
		{
			if (CubismField_88.ContainsKey(argument0))
			{
				return CubismField_88[argument0];
			}
			return _0020(argument0);
		}

		public static PartsDataID getIDAsync(string argument0)
		{
			Monitor.Enter(CubismField_89);
			PartsDataID result = ((!_3000(argument0)) ? _0020(argument0) : CubismField_88[argument0]);
			Monitor.Exit(CubismField_89);
			return result;
		}

		internal static bool _3000(string argument0)
		{
			return CubismField_88.ContainsKey(argument0);
		}

		internal static PartsDataID _0020(string argument0)
		{
			if (_3000(argument0))
			{
				UtDebug.println("Parts ID already exist : {0}", argument0);
				return null;
			}
			PartsDataID partsDataID = new PartsDataID(argument0);
			CubismField_88.Add(argument0, partsDataID);
			return partsDataID;
		}
	}
	public class DrawDataID : ID
	{
		private static Dictionary<string, DrawDataID> CubismField_90 = new Dictionary<string, DrawDataID>();

		private static readonly object CubismField_91 = new object();

		private DrawDataID()
		{
		}

		private DrawDataID(string argument0)
		{
			CubismField_150 = argument0;
		}

		public DrawDataID createIDForSerialize()
		{
			return new DrawDataID();
		}

		internal static void _0020()
		{
			CubismField_90.Clear();
		}

		public static DrawDataID getID(string argument0)
		{
			if (CubismField_90.ContainsKey(argument0))
			{
				return CubismField_90[argument0];
			}
			DrawDataID drawDataID = new DrawDataID(argument0);
			CubismField_90.Add(argument0, drawDataID);
			return drawDataID;
		}

		public static DrawDataID getIDAsync(string argument0)
		{
			Monitor.Enter(CubismField_91);
			if (CubismField_90.ContainsKey(argument0))
			{
				Monitor.Exit(CubismField_91);
				return CubismField_90[argument0];
			}
			DrawDataID drawDataID = new DrawDataID(argument0);
			CubismField_90.Add(argument0, drawDataID);
			Monitor.Exit(CubismField_91);
			return drawDataID;
		}
	}
	public interface IDrawData : ISerializableV2
	{
		IDrawContext init(ModelContext argument0);

		void setupInterpolate(ModelContext argument0, IDrawContext argument1);

		void setupTransform(ModelContext argument0, IDrawContext argument1);

		DrawDataID getDrawDataID();

		void setDrawDataID(DrawDataID argument0);

		float getOpacity(ModelContext argument0, IDrawContext argument1);

		int getDrawOrder(ModelContext argument0, IDrawContext argument1);

		void setDrawOrder(int[] argument0);

		BaseDataID getTargetBaseDataID();

		void setTargetBaseDataID(BaseDataID argument0);

		bool needTransform();

		void draw(DrawParam argument0, ModelContext argument1, IDrawContext argument2);

		void preDraw(DrawParam argument0, ModelContext argument1, IDrawContext argument2);

		int getType();

		void setZ_TestImpl(ModelContext argument0, IDrawContext argument1, float argument2);
	}
	public class IDrawContext
	{
		public IDrawData srcPtr;

		public int partsIndex;

		public int interpolatedDrawOrder;

		public float interpolatedOpacity;

		public bool[] paramOutside = new bool[1];

		public float partsOpacity;

		public bool available = true;

		public float baseOpacity = 1f;

		internal CubismType_43.CubismType_44 CubismField_100;

		protected IDrawContext(IDrawData argument0)
		{
			srcPtr = argument0;
		}

		public bool isParamOutside()
		{
			return paramOutside[0];
		}

		public bool isAvailable()
		{
			if (available)
			{
				return !paramOutside[0];
			}
			return false;
		}

		public IDrawData getSrcPtr()
		{
			return srcPtr;
		}
	}
	internal class CubismType_15 : CubismType_19
	{
		public class CubismType_16 : IDrawContext
		{
			internal int CubismField_119 = -2;

			internal float[] CubismField_120;

			internal float[] CubismField_121;

			internal float[] CubismField_122;

			internal CubismType_16(IDrawData argument0)
				: base(argument0)
			{
			}

			internal float[] _0020()
			{
				if (CubismField_121 == null)
				{
					return CubismField_120;
				}
				return CubismField_121;
			}
		}

		internal class CubismType_39
		{
			public bool enabled;

			public int convertedTextureNo = -1;

			public float offsetX;

			public float offsetY;

			public float scaleX = 1f;

			public float scaleY = 1f;

			public bool transposition;
		}

		private static int CubismField_101 = 0;

		internal const int CubismField_102 = 30;

		internal const int CubismField_103 = 0;

		internal const int CubismField_104 = 1;

		internal const int CubismField_105 = 2;

		private int CubismField_106 = -1;

		private int CubismField_107;

		private int CubismField_108;

		private int CubismField_109;

		private Dictionary<string, object> CubismField_110;

		private ushort[] CubismField_111;

		private List<float[]> CubismField_112;

		private float[] CubismField_113;

		private CubismType_39 CubismField_114 = new CubismType_39();

		private int CubismField_115;

		private bool CubismField_116 = true;

		private static bool[] CubismField_117 = new bool[1];

		private int CubismField_118 = -1;

		internal CubismType_15()
		{
			CubismField_101++;
		}

		public void _3000_0020_3000_0020(int argument0)
		{
			CubismField_106 = argument0;
		}

		public int _3000_0020_3000()
		{
			return CubismField_106;
		}

		public float[] _3000_3000_0020()
		{
			return CubismField_113;
		}

		public int _3000_0020_0020()
		{
			return CubismField_109;
		}

		public new int _3000_0020()
		{
			return CubismField_107;
		}

		public override int getType()
		{
			return 2;
		}

		public override void setZ_TestImpl(ModelContext argument0, IDrawContext argument1, float argument2)
		{
			CubismType_16 cubismType_ = (CubismType_16)argument1;
			float[] array = ((cubismType_.CubismField_121 != null) ? cubismType_.CubismField_121 : cubismType_.CubismField_120);
			int num = 1;
			if (num == 1 || num != 2)
			{
				throw new Exception("未実装 _");
			}
			for (int num2 = CubismField_107 - 1; num2 >= 0; num2--)
			{
				int num3 = num2 * 2;
				array[num3 + 4] = argument2;
			}
		}

		internal void _3000_3000_3000()
		{
			CubismField_145 = new PivotManager();
			CubismField_145._3000_0020_0020();
		}

		public override void readV2(BReader argument0)
		{
			base.readV2(argument0);
			CubismField_106 = argument0.readInt();
			CubismField_107 = argument0.readInt();
			CubismField_108 = argument0.readInt();
			int[] array = (int[])argument0.readObject();
			CubismField_111 = new ushort[CubismField_108 * 3];
			for (int num = CubismField_108 * 3 - 1; num >= 0; num--)
			{
				CubismField_111[num] = (ushort)array[num];
			}
			List<object> list = argument0.readObject() as List<object>;
			CubismField_112 = new List<float[]>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_112.Add((float[])list[i]);
			}
			CubismField_113 = (float[])argument0.readObject();
			if (argument0.getFormatVersion() >= 8)
			{
				CubismField_109 = argument0.readInt();
				if (CubismField_109 == 0)
				{
					return;
				}
				if (((uint)CubismField_109 & (true ? 1u : 0u)) != 0)
				{
					int num2 = argument0.readInt();
					if (CubismField_110 == null)
					{
						CubismField_110 = new Dictionary<string, object>();
					}
					CubismField_118 = num2;
					CubismField_110.Add("BK_OPTION_COLOR", num2);
				}
				if (((uint)CubismField_109 & 0x1Eu) != 0)
				{
					CubismField_115 = (CubismField_109 & 0x1E) >> 1;
				}
				else
				{
					CubismField_115 = 0;
				}
				if (((uint)CubismField_109 & 0x20u) != 0)
				{
					CubismField_116 = false;
				}
			}
			else
			{
				CubismField_109 = 0;
			}
		}

		public override IDrawContext init(ModelContext argument0)
		{
			CubismType_16 cubismType_ = new CubismType_16(this);
			int num = CubismField_107 * 2;
			bool flag = needTransform();
			if (cubismType_.CubismField_120 != null)
			{
				cubismType_.CubismField_120 = null;
			}
			cubismType_.CubismField_120 = new float[num];
			if (cubismType_.CubismField_121 != null)
			{
				cubismType_.CubismField_121 = null;
			}
			cubismType_.CubismField_121 = (flag ? new float[num] : null);
			_3000_0020_3000_3000(cubismType_);
			_0020(cubismType_);
			return cubismType_;
		}

		public void _3000_0020_3000_3000(CubismType_16 argument0)
		{
			bool flag = needTransform();
			int num = 1;
			if (num == 1 || num != 2)
			{
				return;
			}
			for (int num2 = CubismField_107 - 1; num2 >= 0; num2--)
			{
				int num3 = num2 << 1;
				int num4 = num2 * 2;
				float num5 = CubismField_113[num3];
				float num6 = CubismField_113[num3 + 1];
				argument0.CubismField_120[num4] = num5;
				argument0.CubismField_120[num4 + 1] = num6;
				argument0.CubismField_120[num4 + 4] = 0f;
				if (flag)
				{
					argument0.CubismField_121[num4] = num5;
					argument0.CubismField_121[num4 + 1] = num6;
					argument0.CubismField_121[num4 + 4] = 0f;
				}
			}
		}

		public override void setupInterpolate(ModelContext argument0, IDrawContext argument1)
		{
			CubismType_16 cubismType_ = (CubismType_16)argument1;
			if (CubismField_145._3000_3000(argument0))
			{
				base.setupInterpolate(argument0, cubismType_);
				if (!cubismType_.paramOutside[0])
				{
					bool[] cubismField_ = CubismField_117;
					cubismField_[0] = false;
					CubismType_33._3000_0020(argument0, CubismField_145, cubismField_, CubismField_107, CubismField_112, cubismType_.CubismField_120, 0, 2);
				}
			}
		}

		public override void setupTransform(ModelContext argument0, IDrawContext argument1)
		{
			try
			{
				bool flag = false;
				if (argument1.paramOutside[0])
				{
					flag = true;
				}
				CubismType_16 cubismType_ = (CubismType_16)argument1;
				if (flag)
				{
					return;
				}
				base.setupTransform(argument0);
				if (!needTransform())
				{
					return;
				}
				BaseDataID targetBaseDataID = getTargetBaseDataID();
				if (cubismType_.CubismField_119 == -2)
				{
					cubismType_.CubismField_119 = argument0.getBaseDataIndex(targetBaseDataID);
				}
				if (cubismType_.CubismField_119 < 0)
				{
					UtDebug.error("Invalid deformer:: {0}", targetBaseDataID.ToString());
					return;
				}
				IBaseData baseData = argument0.getBaseData(cubismType_.CubismField_119);
				IBaseContext baseContext = argument0.getBaseContext(cubismType_.CubismField_119);
				if (baseData != null && !baseContext.isOutsideParam())
				{
					baseData.transformPoints(argument0, baseContext, cubismType_.CubismField_120, cubismType_.CubismField_121, CubismField_107, 0, 2);
					cubismType_.available = true;
				}
				else
				{
					cubismType_.available = false;
				}
				cubismType_.baseOpacity = baseContext.getTotalOpacity();
			}
			catch (CubismType_40 cubismType_2)
			{
				throw cubismType_2;
			}
			catch (Exception argument2)
			{
				throw new CubismType_40(argument2, getDrawDataID().ToString(), "DDTexture/catch@setupTransform");
			}
		}

		public override void draw(DrawParam argument0, ModelContext argument1, IDrawContext argument2)
		{
			if (!argument2.paramOutside[0])
			{
				CubismType_16 cubismType_ = argument2 as CubismType_16;
				int num = CubismField_106;
				if (CubismField_114.enabled && CubismField_114.convertedTextureNo >= 0)
				{
					num = CubismField_114.convertedTextureNo;
				}
				if (num < 0)
				{
					num = 0;
				}
				float argument3 = getOpacity(argument1, cubismType_) * argument2.partsOpacity * argument2.baseOpacity;
				float[] argument4 = ((cubismType_.CubismField_121 != null) ? cubismType_.CubismField_121 : cubismType_.CubismField_120);
				float[] argument5 = ((cubismType_.CubismField_122 != null) ? cubismType_.CubismField_122 : CubismField_113);
				if (DrawParam.CubismField_54 == DrawParam.ClippingMethod.BufferPre)
				{
					((CubismType_42)argument0)._3000_0020_3000_0020(argument2.CubismField_100);
				}
				argument0.setCurrentDDID(CubismField_143);
				argument0.setCulling(CubismField_116);
				argument0._3000_3000_0020(CubismField_118);
				argument0.drawTexture(num, 3 * CubismField_108, CubismField_111, argument4, argument5, argument3, CubismField_115);
			}
		}

		public object _3000_3000(string argument0)
		{
			if (CubismField_110 == null)
			{
				return null;
			}
			return CubismField_110[argument0];
		}

		public new ushort[] _3000()
		{
			return CubismField_111;
		}

		public override string ToString()
		{
			string text = $"Mesh ID:{CubismField_143.ToString()} Deformer:{CubismField_144.ToString()} Texture:{CubismField_106} Polygon:{CubismField_108}";
			if (CubismField_115 != 0)
			{
				if (CubismField_115 == 2)
				{
					text += " Color:Multiply";
				}
				else if (CubismField_115 == 1)
				{
					text += " Color:Screen";
				}
			}
			if (CubismField_110 != null && CubismField_110.ContainsKey("MASK_CULLING"))
			{
				text = text + " Culling:" + CubismField_110["MASK_CULLING"];
			}
			if (CubismField_118 != -1)
			{
				text += $"Color group: {CubismField_118}";
			}
			if (CubismField_149 != null)
			{
				text = text + " Clipping:" + CubismField_149.ToString();
			}
			return text;
		}

		public override void preDraw(DrawParam argument0, ModelContext argument1, IDrawContext argument2)
		{
			if (DrawParam.CubismField_54 != DrawParam.ClippingMethod.Alpha)
			{
				return;
			}
			if (CubismField_149 == null)
			{
				argument0.clippingProcess = 0;
				return;
			}
			argument0.clippingProcess = 4;
			draw(argument0, argument1, argument2);
			argument0.clippingProcess = 1;
			for (int i = 0; i < CubismField_149.Length; i++)
			{
				int drawDataIndex = argument1.getDrawDataIndex(CubismField_149[i]);
				if (drawDataIndex < 0)
				{
					argument0.clippingProcess = 0;
					return;
				}
				IDrawData drawData = argument1.getDrawData(drawDataIndex);
				IDrawContext drawContext = argument1.getDrawContext(drawDataIndex);
				drawData.draw(argument0, argument1, drawContext);
			}
			argument0.clippingProcess = 2;
			draw(argument0, argument1, argument2);
			argument0.clippingProcess = 3;
		}

		internal void _3000_0020_0020_0020(IDrawContext argument0, int argument1, float argument2, float argument3, float argument4, float argument5, bool argument6)
		{
			if (argument0 is CubismType_16)
			{
				CubismField_114.enabled = true;
				CubismField_114.convertedTextureNo = argument1;
				CubismField_114.offsetX = argument4;
				CubismField_114.offsetY = argument5;
				CubismField_114.scaleX = argument2;
				CubismField_114.scaleY = argument3;
				CubismField_114.transposition = argument6;
			}
		}

		internal void _3000_0020_0020_3000(IDrawContext argument0)
		{
			if (argument0 is CubismType_16 cubismType_)
			{
				CubismField_114.enabled = false;
				cubismType_.CubismField_122 = null;
			}
		}

		internal void _3000_0020_0020_3000()
		{
			CubismField_114.enabled = false;
		}

		internal void _0020(IDrawContext argument0)
		{
			if (CubismField_114 == null || !(argument0 is CubismType_16 cubismType_))
			{
				return;
			}
			if (cubismType_.CubismField_122 == null)
			{
				cubismType_.CubismField_122 = new float[CubismField_113.Length];
			}
			float[] cubismField_ = cubismType_.CubismField_122;
			int num = CubismField_113.Length / 2;
			int num2 = 0;
			float offsetX = CubismField_114.offsetX;
			float offsetY = CubismField_114.offsetY;
			float scaleX = CubismField_114.scaleX;
			float scaleY = CubismField_114.scaleY;
			bool transposition = CubismField_114.transposition;
			for (int i = 0; i < num; i++)
			{
				num2 = i << 1;
				float num3 = offsetX + CubismField_113[num2] * scaleX;
				float num4 = offsetY + CubismField_113[num2 + 1] * scaleY;
				if (transposition)
				{
					cubismField_[num2] = num4;
					cubismField_[num2 + 1] = num3;
				}
				else
				{
					cubismField_[num2] = num3;
					cubismField_[num2 + 1] = num4;
				}
			}
		}
	}
	public class AvatarPartsItem : ISerializableV2
	{
		public class AvatarTextureInfo
		{
			public int textureIndex = -1;

			public float scaleW = 1f;

			public float scaleH = 1f;

			public int colorGroupNo = -1;
		}

		private PartsDataID CubismField_123;

		private int CubismField_124 = -1;

		private List<IBaseData> CubismField_125;

		private List<IDrawData> CubismField_126;

		private string CubismField_127;

		private AvatarTextureInfo[] CubismField_128;

		private static int CubismField_129;

		public AvatarPartsItem()
		{
			CubismField_129++;
		}

		public List<IBaseData> getBaseDataList()
		{
			return CubismField_125;
		}

		public List<IDrawData> getDrawDataList()
		{
			return CubismField_126;
		}

		public void readV2(BReader argument0)
		{
			CubismField_123 = (PartsDataID)argument0.readObject();
			List<object> list = argument0.readObject() as List<object>;
			CubismField_126 = new List<IDrawData>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_126.Add((IDrawData)list[i]);
			}
			list = argument0.readObject() as List<object>;
			CubismField_125 = new List<IBaseData>(list.Count);
			for (int j = 0; j < list.Count; j++)
			{
				CubismField_125.Add((IBaseData)list[j]);
			}
		}

		private void _0020(PartsData argument0)
		{
			argument0.setBaseData(CubismField_125);
			argument0.setDrawData(CubismField_126);
		}

		public void init(string argument0, ALive2DModel argument1)
		{
			ModelContext modelContext = argument1.getModelContext();
			int partsDataIndex = modelContext.getPartsDataIndex(PartsDataID.getID(argument0));
			PartsData partsData = modelContext.getPartsData(partsDataIndex);
			if (partsData == null)
			{
				UtDebug.error("avatar parts not found!");
				return;
			}
			partsData.setVisible(argument0: true);
			int textureCount = getTextureCount();
			for (int i = 0; i < textureCount; i++)
			{
				if (CubismField_128[i].textureIndex < 0)
				{
					CubismField_128[i].textureIndex = argument1.generateModelTextureNo();
				}
			}
			int count = CubismField_126.Count;
			for (int j = 0; j < count; j++)
			{
				if (CubismField_126[j].getType() != 2 || !(CubismField_126[j] is CubismType_15 cubismType_))
				{
					continue;
				}
				int num = cubismType_._3000_0020_3000();
				if (textureCount <= num)
				{
					continue;
				}
				AvatarTextureInfo avatarTextureInfo = CubismField_128[num];
				int textureIndex = avatarTextureInfo.textureIndex;
				if (textureIndex == -1)
				{
					continue;
				}
				cubismType_._3000_0020_3000_0020(textureIndex);
				float[] array = cubismType_._3000_3000_0020();
				int num2 = cubismType_._3000_0020();
				int num3 = 0;
				float num4 = 1f / avatarTextureInfo.scaleW;
				float num5 = 1f / avatarTextureInfo.scaleH;
				if (num4 != 1f || num5 != 1f)
				{
					for (int k = 0; k < num2; k++)
					{
						num3 = k << 1;
						array[num3] *= num4;
						array[num3 + 1] *= num5;
					}
				}
			}
			_0020(partsData);
			CubismField_127 = argument0;
		}

		public static AvatarPartsItem loadAvatarParts(string argument0)
		{
			return loadAvatarParts(UtFile.convertStream(UtFile.loadFile(argument0)));
		}

		public static AvatarPartsItem loadAvatarParts(byte[] argument0)
		{
			return loadAvatarParts(new MemoryStream(argument0));
		}

		public static AvatarPartsItem loadAvatarParts(Stream argument0)
		{
			if (argument0 == null)
			{
				return null;
			}
			argument0.Position = 0L;
			BReader bReader = new BReader(argument0);
			byte b = bReader.readByte();
			byte b2 = bReader.readByte();
			byte b3 = bReader.readByte();
			byte formatVersion = bReader.readByte();
			bReader.setFormatVersion(formatVersion);
			if (b != 98 || b2 != 107 || b3 != 112)
			{
				UtDebug.error("loaded file is not  parts\n");
				return null;
			}
			bReader.readInt();
			AvatarPartsItem avatarPartsItem = bReader.readObject() as AvatarPartsItem;
			int num = bReader.readInt();
			avatarPartsItem.CubismField_128 = new AvatarTextureInfo[num];
			for (int i = 0; i < num; i++)
			{
				avatarPartsItem.CubismField_128[i] = new AvatarTextureInfo();
			}
			if (bReader.readInt() != -2004318072)
			{
				UtDebug.error("EOF check error\n");
				return null;
			}
			int count = avatarPartsItem.CubismField_126.Count;
			for (int j = 0; j < count; j++)
			{
				if (avatarPartsItem.CubismField_126[j].getType() != 2 || !(avatarPartsItem.CubismField_126[j] is CubismType_15 cubismType_))
				{
					continue;
				}
				int num2 = cubismType_._3000_0020_3000();
				if (num > num2)
				{
					object obj = cubismType_._3000_3000("BK_OPTION_COLOR");
					if (obj != null)
					{
						avatarPartsItem.CubismField_128[num2].colorGroupNo = (int)obj;
					}
				}
			}
			return avatarPartsItem;
		}

		public int getTextureCount()
		{
			return CubismField_128.Length;
		}

		public void setTextureInfo(int argument0, float argument1 = 1f, float argument2 = 1f)
		{
			if (argument0 >= 0 && CubismField_128.Length > argument0)
			{
				CubismField_128[argument0].scaleW = argument1;
				CubismField_128[argument0].scaleH = argument2;
			}
		}

		public void setTextureIndex(int argument0, int argument1)
		{
			if (argument0 >= 0 && CubismField_128.Length > argument0)
			{
				CubismField_128[argument0].textureIndex = argument1;
			}
		}

		public int getTextureIndex(int argument0)
		{
			if (argument0 < 0 || CubismField_128.Length <= argument0)
			{
				return -1;
			}
			return CubismField_128[argument0].textureIndex;
		}

		public int getColorGroupNo(int argument0)
		{
			if (argument0 < 0 || getTextureCount() <= argument0)
			{
				return -1;
			}
			return CubismField_128[argument0].colorGroupNo;
		}

		public void setPartsNo(int argument0)
		{
			CubismField_124 = argument0;
		}

		public int getPartsNo()
		{
			return CubismField_124;
		}
	}
	public class PartsData : ISerializableV2
	{
		public class PartsDataContext : IContextData
		{
			private float CubismField_464;

			private PartsData CubismField_465;

			public PartsDataContext(PartsData argument0)
			{
				CubismField_465 = argument0;
			}

			public float getPartsOpacity()
			{
				return CubismField_464;
			}

			public void setPartsOpacity(float argument0)
			{
				CubismField_464 = argument0;
			}
		}

		private static int CubismField_130;

		private bool CubismField_131 = true;

		private bool CubismField_132;

		private PartsDataID CubismField_133;

		private List<IBaseData> CubismField_134;

		private List<IDrawData> CubismField_135;

		public PartsData()
		{
			CubismField_130++;
		}

		internal void _3000()
		{
			CubismField_134 = new List<IBaseData>();
			CubismField_135 = new List<IDrawData>();
		}

		public void readV2(BReader argument0)
		{
			CubismField_132 = argument0.readBit();
			CubismField_131 = argument0.readBit();
			CubismField_133 = (PartsDataID)argument0.readObject();
			List<object> list = argument0.readObject() as List<object>;
			CubismField_134 = new List<IBaseData>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_134.Add((IBaseData)list[i]);
			}
			list = argument0.readObject() as List<object>;
			CubismField_135 = new List<IDrawData>(list.Count);
			for (int j = 0; j < list.Count; j++)
			{
				CubismField_135.Add((IDrawData)list[j]);
			}
		}

		public PartsDataContext init(ModelContext argument0)
		{
			PartsDataContext partsDataContext = new PartsDataContext(this);
			partsDataContext.setPartsOpacity(isVisible() ? 1f : 0f);
			return partsDataContext;
		}

		public void addBaseData(IBaseData argument0)
		{
			if (CubismField_134 == null)
			{
				throw new Exception("baseDataList not initialized@addBaseData");
			}
			CubismField_134.Add(argument0);
		}

		public void addDrawData(IDrawData argument0)
		{
			if (CubismField_135 == null)
			{
				throw new Exception("drawDataList not initialized@addDrawData");
			}
			CubismField_135.Add(argument0);
		}

		public void setBaseData(List<IBaseData> argument0)
		{
			CubismField_134 = argument0;
		}

		public void setDrawData(List<IDrawData> argument0)
		{
			CubismField_135 = argument0;
		}

		public bool isVisible()
		{
			return CubismField_131;
		}

		public bool isLocked()
		{
			return CubismField_132;
		}

		public void setVisible(bool argument0)
		{
			CubismField_131 = argument0;
		}

		public void setLocked(bool argument0)
		{
			CubismField_132 = argument0;
		}

		public List<IBaseData> getBaseData()
		{
			return CubismField_134;
		}

		public List<IDrawData> getDrawData()
		{
			return CubismField_135;
		}

		public PartsDataID getPartsDataID()
		{
			return CubismField_133;
		}

		public void setPartsDataID(PartsDataID argument0)
		{
			CubismField_133 = argument0;
		}

		public string toStringAllDrawable()
		{
			string text = "";
			for (int i = 0; i < CubismField_135.Count; i++)
			{
				if (CubismField_135[i] is CubismType_19 cubismType_)
				{
					text = text + "\n" + cubismType_.ToString();
				}
			}
			return text;
		}

		internal bool _0020(string argument0)
		{
			return CubismField_133.ToString() == argument0;
		}
	}
	internal abstract class CubismType_19 : IDrawData, ISerializableV2
	{
		internal const int CubismField_136 = -2;

		internal const int CubismField_137 = 500;

		internal const int CubismField_138 = 2;

		internal const int CubismField_139 = 3;

		internal const int CubismField_140 = 4;

		private static int CubismField_141 = 500;

		private static int CubismField_142 = 500;

		protected DrawDataID CubismField_143;

		protected BaseDataID CubismField_144;

		protected PivotManager CubismField_145;

		protected int CubismField_146;

		protected int[] CubismField_147;

		protected float[] CubismField_148;

		protected DrawDataID[] CubismField_149;

		internal CubismType_19()
		{
		}

		public virtual void readV2(BReader argument0)
		{
			CubismField_143 = (DrawDataID)argument0.readObject();
			CubismField_144 = (BaseDataID)argument0.readObject();
			CubismField_145 = (PivotManager)argument0.readObject();
			CubismField_146 = argument0.readInt();
			CubismField_147 = argument0.readArrayInt();
			CubismField_148 = argument0.readArrayFloat();
			if (argument0.getFormatVersion() >= 11)
			{
				DrawDataID argument = argument0.readObject() as DrawDataID;
				CubismField_149 = _0020(argument);
			}
			else
			{
				CubismField_149 = null;
			}
			_3000_0020_0020(CubismField_147);
		}

		private static DrawDataID[] _0020(DrawDataID argument0)
		{
			if (argument0 == null || argument0.ToString().Length == 0)
			{
				return null;
			}
			if (argument0.ToString().IndexOf(",") < 0)
			{
				return new DrawDataID[1] { argument0 };
			}
			string[] array = argument0.ToString().Split(',');
			DrawDataID[] array2 = new DrawDataID[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = DrawDataID.getID(array[i]);
			}
			return array2;
		}

		public virtual void setupInterpolate(ModelContext argument0, IDrawContext argument1)
		{
			argument1.paramOutside[0] = false;
			argument1.interpolatedDrawOrder = CubismType_33._3000(argument0, CubismField_145, argument1.paramOutside, CubismField_147);
			if (Live2D.CubismField_257 || !argument1.paramOutside[0])
			{
				argument1.interpolatedOpacity = CubismType_33._0020(argument0, CubismField_145, argument1.paramOutside, CubismField_148);
			}
		}

		public DrawDataID getDrawDataID()
		{
			return CubismField_143;
		}

		public void setDrawDataID(DrawDataID argument0)
		{
			CubismField_143 = argument0;
		}

		public float getOpacity(ModelContext argument0, IDrawContext argument1)
		{
			return argument1.interpolatedOpacity;
		}

		public int getDrawOrder(ModelContext argument0, IDrawContext argument1)
		{
			return argument1.interpolatedDrawOrder;
		}

		public DrawDataID[] _3000()
		{
			return CubismField_149;
		}

		public static void _3000_0020_0020(int[] argument0)
		{
			for (int num = argument0.Length - 1; num >= 0; num--)
			{
				int num2 = argument0[num];
				if (num2 < CubismField_141)
				{
					CubismField_141 = num2;
				}
				else if (num2 > CubismField_142)
				{
					CubismField_142 = num2;
				}
			}
		}

		public BaseDataID getTargetBaseDataID()
		{
			return CubismField_144;
		}

		public void setTargetBaseDataID(BaseDataID argument0)
		{
			CubismField_144 = argument0;
		}

		public bool needTransform()
		{
			if (CubismField_144 != null)
			{
				return CubismField_144 != BaseDataID.DST_BASE_ID();
			}
			return false;
		}

		public static int _3000_3000()
		{
			return CubismField_141;
		}

		public static int _3000_0020()
		{
			return CubismField_142;
		}

		public abstract IDrawContext init(ModelContext argument0);

		public void setDrawOrder(int[] argument0)
		{
			for (int num = argument0.Length - 1; num >= 0; num--)
			{
				int num2 = argument0[num];
				if (num2 < CubismField_141)
				{
					CubismField_141 = num2;
				}
				else if (num2 > CubismField_142)
				{
					CubismField_142 = num2;
				}
			}
		}

		public virtual void setupTransform(ModelContext argument0)
		{
		}

		public virtual void setupTransform(ModelContext argument0, IDrawContext argument1)
		{
		}

		public abstract void draw(DrawParam argument0, ModelContext argument1, IDrawContext argument2);

		public abstract int getType();

		public abstract void setZ_TestImpl(ModelContext argument0, IDrawContext argument1, float argument2);

		public override string ToString()
		{
			string text = $"Drawable id:{CubismField_143.ToString()}";
			if (CubismField_144 != null)
			{
				text = text + " parentDeformer:" + CubismField_144.ToString();
			}
			if (CubismField_149 != null)
			{
				text = text + " clip:" + CubismField_149.ToString();
			}
			return text;
		}

		public abstract void preDraw(DrawParam argument0, ModelContext argument1, IDrawContext argument2);
	}
	public class ID
	{
		protected string CubismField_150;

		public new string ToString()
		{
			return CubismField_150;
		}

		public static void releaseStored_notForClientCall()
		{
			ParamID._0020();
			BaseDataID._0020();
			DrawDataID._0020();
			PartsDataID._3000_0020();
		}
	}
	public interface ISerializableV2
	{
		void readV2(BReader argument0);
	}
	public abstract class IBaseData : ISerializableV2
	{
		public const int BASE_INDEX_NOT_INIT = -2;

		public const int TYPE_BD_AFFINE = 1;

		public const int TYPE_BD_BOX_GRID = 2;

		protected BaseDataID CubismField_158;

		protected BaseDataID CubismField_159;

		protected bool CubismField_160 = true;

		private float[] CubismField_161;

		public IBaseData()
		{
		}

		public virtual void readV2(BReader argument0)
		{
			CubismField_158 = (BaseDataID)argument0.readObject();
			CubismField_159 = (BaseDataID)argument0.readObject();
		}

		protected void readV2_opacity(BReader argument0)
		{
			if (argument0.getFormatVersion() >= 10)
			{
				CubismField_161 = argument0.readArrayFloat();
			}
		}

		public abstract IBaseContext init(ModelContext argument0);

		public abstract void setupInterpolate(ModelContext argument0, IBaseContext argument1);

		protected void interpolateOpacity(ModelContext argument0, PivotManager argument1, IBaseContext argument2, bool[] argument3)
		{
			if (CubismField_161 == null)
			{
				argument2.setInterpolatedOpacity(1f);
			}
			else
			{
				argument2.setInterpolatedOpacity(CubismType_33._0020(argument0, argument1, argument3, CubismField_161));
			}
		}

		public abstract void setupTransform(ModelContext argument0, IBaseContext argument1);

		public abstract void transformPoints(ModelContext argument0, IBaseContext argument1, float[] argument2, float[] argument3, int argument4, int argument5, int argument6);

		public abstract int getType();

		public void setTargetBaseDataID(BaseDataID argument0)
		{
			CubismField_159 = argument0;
		}

		public void setBaseDataID(BaseDataID argument0)
		{
			CubismField_158 = argument0;
		}

		public BaseDataID getTargetBaseDataID()
		{
			return CubismField_159;
		}

		public BaseDataID getBaseDataID()
		{
			return CubismField_158;
		}

		public bool needTransform()
		{
			if (CubismField_159 != null)
			{
				return CubismField_159 != BaseDataID.DST_BASE_ID();
			}
			return false;
		}
	}
	internal class CubismType_24 : ISerializableV2
	{
		internal float CubismField_162;

		internal float CubismField_163;

		internal float CubismField_164;

		internal float CubismField_165;

		internal float CubismField_166;

		internal bool CubismField_167;

		internal bool CubismField_168;

		internal CubismType_24()
		{
			CubismField_162 = 0f;
			CubismField_163 = 0f;
			CubismField_164 = 1f;
			CubismField_165 = 1f;
			CubismField_166 = 0f;
			CubismField_167 = false;
			CubismField_168 = false;
		}

		internal void _0020(CubismType_24 argument0)
		{
			CubismField_162 = argument0.CubismField_162;
			CubismField_163 = argument0.CubismField_163;
			CubismField_164 = argument0.CubismField_164;
			CubismField_165 = argument0.CubismField_165;
			CubismField_166 = argument0.CubismField_166;
			CubismField_167 = argument0.CubismField_167;
			CubismField_168 = argument0.CubismField_168;
		}

		public void readV2(BReader argument0)
		{
			CubismField_162 = argument0.readFloat();
			CubismField_163 = argument0.readFloat();
			CubismField_164 = argument0.readFloat();
			CubismField_165 = argument0.readFloat();
			CubismField_166 = argument0.readFloat();
			if (argument0.getFormatVersion() >= 10)
			{
				CubismField_167 = argument0.readBoolean();
				CubismField_168 = argument0.readBoolean();
			}
		}
	}
	internal class CubismType_25 : IBaseData
	{
		private PivotManager CubismField_169;

		private List<CubismType_24> CubismField_170;

		private static float[] CubismField_171 = new float[2];

		private static float[] CubismField_172 = new float[2];

		private static float[] CubismField_173 = new float[2];

		private static float[] CubismField_174 = new float[2];

		private static float[] CubismField_175 = new float[2];

		private static float[] CubismField_176 = new float[2];

		private static bool[] CubismField_177 = new bool[1];

		internal CubismType_25()
		{
		}

		internal void _3000()
		{
			CubismField_169 = new PivotManager();
			CubismField_169._3000_0020_0020();
			CubismField_170 = new List<CubismType_24>();
		}

		public override int getType()
		{
			return 1;
		}

		public override void readV2(BReader argument0)
		{
			base.readV2(argument0);
			CubismField_169 = (PivotManager)argument0.readObject();
			List<object> list = argument0.readObject() as List<object>;
			CubismField_170 = new List<CubismType_24>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_170.Add((CubismType_24)list[i]);
			}
			readV2_opacity(argument0);
		}

		public override IBaseContext init(ModelContext argument0)
		{
			CubismType_28 cubismType_ = new CubismType_28(this);
			cubismType_.CubismField_187 = new CubismType_24();
			if (needTransform())
			{
				cubismType_.CubismField_188 = new CubismType_24();
			}
			return cubismType_;
		}

		public override void setupInterpolate(ModelContext argument0, IBaseContext argument1)
		{
			CubismType_28 cubismType_ = (CubismType_28)argument1;
			if (!CubismField_169._3000_3000(argument0))
			{
				return;
			}
			bool[] cubismField_ = CubismField_177;
			cubismField_[0] = false;
			int num = CubismField_169._3000_0020(argument0, cubismField_);
			argument1.setOutsideParam(cubismField_[0]);
			interpolateOpacity(argument0, CubismField_169, argument1, cubismField_);
			short[] array = argument0._3000();
			float[] array2 = argument0._3000_0020();
			CubismField_169._3000(array, array2, num);
			if (num <= 0)
			{
				CubismType_24 argument2 = CubismField_170[array[0]];
				cubismType_.CubismField_187._0020(argument2);
			}
			else
			{
				switch (num)
				{
				case 1:
				{
					CubismType_24 cubismType_6 = CubismField_170[array[0]];
					CubismType_24 cubismType_7 = CubismField_170[array[1]];
					float num14 = array2[0];
					cubismType_.CubismField_187.CubismField_162 = cubismType_6.CubismField_162 + (cubismType_7.CubismField_162 - cubismType_6.CubismField_162) * num14;
					cubismType_.CubismField_187.CubismField_163 = cubismType_6.CubismField_163 + (cubismType_7.CubismField_163 - cubismType_6.CubismField_163) * num14;
					cubismType_.CubismField_187.CubismField_164 = cubismType_6.CubismField_164 + (cubismType_7.CubismField_164 - cubismType_6.CubismField_164) * num14;
					cubismType_.CubismField_187.CubismField_165 = cubismType_6.CubismField_165 + (cubismType_7.CubismField_165 - cubismType_6.CubismField_165) * num14;
					cubismType_.CubismField_187.CubismField_166 = cubismType_6.CubismField_166 + (cubismType_7.CubismField_166 - cubismType_6.CubismField_166) * num14;
					break;
				}
				case 2:
				{
					CubismType_24 cubismType_2 = CubismField_170[array[0]];
					CubismType_24 cubismType_3 = CubismField_170[array[1]];
					CubismType_24 cubismType_4 = CubismField_170[array[2]];
					CubismType_24 cubismType_5 = CubismField_170[array[3]];
					float num10 = array2[0];
					float num11 = array2[1];
					float num12 = cubismType_2.CubismField_162 + (cubismType_3.CubismField_162 - cubismType_2.CubismField_162) * num10;
					float num13 = cubismType_4.CubismField_162 + (cubismType_5.CubismField_162 - cubismType_4.CubismField_162) * num10;
					cubismType_.CubismField_187.CubismField_162 = num12 + (num13 - num12) * num11;
					num12 = cubismType_2.CubismField_163 + (cubismType_3.CubismField_163 - cubismType_2.CubismField_163) * num10;
					num13 = cubismType_4.CubismField_163 + (cubismType_5.CubismField_163 - cubismType_4.CubismField_163) * num10;
					cubismType_.CubismField_187.CubismField_163 = num12 + (num13 - num12) * num11;
					num12 = cubismType_2.CubismField_164 + (cubismType_3.CubismField_164 - cubismType_2.CubismField_164) * num10;
					num13 = cubismType_4.CubismField_164 + (cubismType_5.CubismField_164 - cubismType_4.CubismField_164) * num10;
					cubismType_.CubismField_187.CubismField_164 = num12 + (num13 - num12) * num11;
					num12 = cubismType_2.CubismField_165 + (cubismType_3.CubismField_165 - cubismType_2.CubismField_165) * num10;
					num13 = cubismType_4.CubismField_165 + (cubismType_5.CubismField_165 - cubismType_4.CubismField_165) * num10;
					cubismType_.CubismField_187.CubismField_165 = num12 + (num13 - num12) * num11;
					num12 = cubismType_2.CubismField_166 + (cubismType_3.CubismField_166 - cubismType_2.CubismField_166) * num10;
					num13 = cubismType_4.CubismField_166 + (cubismType_5.CubismField_166 - cubismType_4.CubismField_166) * num10;
					cubismType_.CubismField_187.CubismField_166 = num12 + (num13 - num12) * num11;
					break;
				}
				case 3:
				{
					CubismType_24 cubismType_24 = CubismField_170[array[0]];
					CubismType_24 cubismType_25 = CubismField_170[array[1]];
					CubismType_24 cubismType_26 = CubismField_170[array[2]];
					CubismType_24 cubismType_27 = CubismField_170[array[3]];
					CubismType_24 cubismType_28 = CubismField_170[array[4]];
					CubismType_24 cubismType_29 = CubismField_170[array[5]];
					CubismType_24 cubismType_30 = CubismField_170[array[6]];
					CubismType_24 cubismType_31 = CubismField_170[array[7]];
					float num27 = array2[0];
					float num28 = array2[1];
					float num29 = array2[2];
					float num30 = cubismType_24.CubismField_162 + (cubismType_25.CubismField_162 - cubismType_24.CubismField_162) * num27;
					float num31 = cubismType_26.CubismField_162 + (cubismType_27.CubismField_162 - cubismType_26.CubismField_162) * num27;
					float num32 = cubismType_28.CubismField_162 + (cubismType_29.CubismField_162 - cubismType_28.CubismField_162) * num27;
					float num33 = cubismType_30.CubismField_162 + (cubismType_31.CubismField_162 - cubismType_30.CubismField_162) * num27;
					cubismType_.CubismField_187.CubismField_162 = (1f - num29) * (num30 + (num31 - num30) * num28) + num29 * (num32 + (num33 - num32) * num28);
					num30 = cubismType_24.CubismField_163 + (cubismType_25.CubismField_163 - cubismType_24.CubismField_163) * num27;
					num31 = cubismType_26.CubismField_163 + (cubismType_27.CubismField_163 - cubismType_26.CubismField_163) * num27;
					num32 = cubismType_28.CubismField_163 + (cubismType_29.CubismField_163 - cubismType_28.CubismField_163) * num27;
					num33 = cubismType_30.CubismField_163 + (cubismType_31.CubismField_163 - cubismType_30.CubismField_163) * num27;
					cubismType_.CubismField_187.CubismField_163 = (1f - num29) * (num30 + (num31 - num30) * num28) + num29 * (num32 + (num33 - num32) * num28);
					num30 = cubismType_24.CubismField_164 + (cubismType_25.CubismField_164 - cubismType_24.CubismField_164) * num27;
					num31 = cubismType_26.CubismField_164 + (cubismType_27.CubismField_164 - cubismType_26.CubismField_164) * num27;
					num32 = cubismType_28.CubismField_164 + (cubismType_29.CubismField_164 - cubismType_28.CubismField_164) * num27;
					num33 = cubismType_30.CubismField_164 + (cubismType_31.CubismField_164 - cubismType_30.CubismField_164) * num27;
					cubismType_.CubismField_187.CubismField_164 = (1f - num29) * (num30 + (num31 - num30) * num28) + num29 * (num32 + (num33 - num32) * num28);
					num30 = cubismType_24.CubismField_165 + (cubismType_25.CubismField_165 - cubismType_24.CubismField_165) * num27;
					num31 = cubismType_26.CubismField_165 + (cubismType_27.CubismField_165 - cubismType_26.CubismField_165) * num27;
					num32 = cubismType_28.CubismField_165 + (cubismType_29.CubismField_165 - cubismType_28.CubismField_165) * num27;
					num33 = cubismType_30.CubismField_165 + (cubismType_31.CubismField_165 - cubismType_30.CubismField_165) * num27;
					cubismType_.CubismField_187.CubismField_165 = (1f - num29) * (num30 + (num31 - num30) * num28) + num29 * (num32 + (num33 - num32) * num28);
					num30 = cubismType_24.CubismField_166 + (cubismType_25.CubismField_166 - cubismType_24.CubismField_166) * num27;
					num31 = cubismType_26.CubismField_166 + (cubismType_27.CubismField_166 - cubismType_26.CubismField_166) * num27;
					num32 = cubismType_28.CubismField_166 + (cubismType_29.CubismField_166 - cubismType_28.CubismField_166) * num27;
					num33 = cubismType_30.CubismField_166 + (cubismType_31.CubismField_166 - cubismType_30.CubismField_166) * num27;
					cubismType_.CubismField_187.CubismField_166 = (1f - num29) * (num30 + (num31 - num30) * num28) + num29 * (num32 + (num33 - num32) * num28);
					break;
				}
				case 4:
				{
					CubismType_24 cubismType_8 = CubismField_170[array[0]];
					CubismType_24 cubismType_9 = CubismField_170[array[1]];
					CubismType_24 cubismType_10 = CubismField_170[array[2]];
					CubismType_24 cubismType_11 = CubismField_170[array[3]];
					CubismType_24 cubismType_12 = CubismField_170[array[4]];
					CubismType_24 cubismType_13 = CubismField_170[array[5]];
					CubismType_24 cubismType_14 = CubismField_170[array[6]];
					CubismType_24 cubismType_15 = CubismField_170[array[7]];
					CubismType_24 cubismType_16 = CubismField_170[array[8]];
					CubismType_24 cubismType_17 = CubismField_170[array[9]];
					CubismType_24 cubismType_18 = CubismField_170[array[10]];
					CubismType_24 cubismType_19 = CubismField_170[array[11]];
					CubismType_24 cubismType_20 = CubismField_170[array[12]];
					CubismType_24 cubismType_21 = CubismField_170[array[13]];
					CubismType_24 cubismType_22 = CubismField_170[array[14]];
					CubismType_24 cubismType_23 = CubismField_170[array[15]];
					float num15 = array2[0];
					float num16 = array2[1];
					float num17 = array2[2];
					float num18 = array2[3];
					float num19 = cubismType_8.CubismField_162 + (cubismType_9.CubismField_162 - cubismType_8.CubismField_162) * num15;
					float num20 = cubismType_10.CubismField_162 + (cubismType_11.CubismField_162 - cubismType_10.CubismField_162) * num15;
					float num21 = cubismType_12.CubismField_162 + (cubismType_13.CubismField_162 - cubismType_12.CubismField_162) * num15;
					float num22 = cubismType_14.CubismField_162 + (cubismType_15.CubismField_162 - cubismType_14.CubismField_162) * num15;
					float num23 = cubismType_16.CubismField_162 + (cubismType_17.CubismField_162 - cubismType_16.CubismField_162) * num15;
					float num24 = cubismType_18.CubismField_162 + (cubismType_19.CubismField_162 - cubismType_18.CubismField_162) * num15;
					float num25 = cubismType_20.CubismField_162 + (cubismType_21.CubismField_162 - cubismType_20.CubismField_162) * num15;
					float num26 = cubismType_22.CubismField_162 + (cubismType_23.CubismField_162 - cubismType_22.CubismField_162) * num15;
					cubismType_.CubismField_187.CubismField_162 = (1f - num18) * ((1f - num17) * (num19 + (num20 - num19) * num16) + num17 * (num21 + (num22 - num21) * num16)) + num18 * ((1f - num17) * (num23 + (num24 - num23) * num16) + num17 * (num25 + (num26 - num25) * num16));
					num19 = cubismType_8.CubismField_163 + (cubismType_9.CubismField_163 - cubismType_8.CubismField_163) * num15;
					num20 = cubismType_10.CubismField_163 + (cubismType_11.CubismField_163 - cubismType_10.CubismField_163) * num15;
					num21 = cubismType_12.CubismField_163 + (cubismType_13.CubismField_163 - cubismType_12.CubismField_163) * num15;
					num22 = cubismType_14.CubismField_163 + (cubismType_15.CubismField_163 - cubismType_14.CubismField_163) * num15;
					num23 = cubismType_16.CubismField_163 + (cubismType_17.CubismField_163 - cubismType_16.CubismField_163) * num15;
					num24 = cubismType_18.CubismField_163 + (cubismType_19.CubismField_163 - cubismType_18.CubismField_163) * num15;
					num25 = cubismType_20.CubismField_163 + (cubismType_21.CubismField_163 - cubismType_20.CubismField_163) * num15;
					num26 = cubismType_22.CubismField_163 + (cubismType_23.CubismField_163 - cubismType_22.CubismField_163) * num15;
					cubismType_.CubismField_187.CubismField_163 = (1f - num18) * ((1f - num17) * (num19 + (num20 - num19) * num16) + num17 * (num21 + (num22 - num21) * num16)) + num18 * ((1f - num17) * (num23 + (num24 - num23) * num16) + num17 * (num25 + (num26 - num25) * num16));
					num19 = cubismType_8.CubismField_164 + (cubismType_9.CubismField_164 - cubismType_8.CubismField_164) * num15;
					num20 = cubismType_10.CubismField_164 + (cubismType_11.CubismField_164 - cubismType_10.CubismField_164) * num15;
					num21 = cubismType_12.CubismField_164 + (cubismType_13.CubismField_164 - cubismType_12.CubismField_164) * num15;
					num22 = cubismType_14.CubismField_164 + (cubismType_15.CubismField_164 - cubismType_14.CubismField_164) * num15;
					num23 = cubismType_16.CubismField_164 + (cubismType_17.CubismField_164 - cubismType_16.CubismField_164) * num15;
					num24 = cubismType_18.CubismField_164 + (cubismType_19.CubismField_164 - cubismType_18.CubismField_164) * num15;
					num25 = cubismType_20.CubismField_164 + (cubismType_21.CubismField_164 - cubismType_20.CubismField_164) * num15;
					num26 = cubismType_22.CubismField_164 + (cubismType_23.CubismField_164 - cubismType_22.CubismField_164) * num15;
					cubismType_.CubismField_187.CubismField_164 = (1f - num18) * ((1f - num17) * (num19 + (num20 - num19) * num16) + num17 * (num21 + (num22 - num21) * num16)) + num18 * ((1f - num17) * (num23 + (num24 - num23) * num16) + num17 * (num25 + (num26 - num25) * num16));
					num19 = cubismType_8.CubismField_165 + (cubismType_9.CubismField_165 - cubismType_8.CubismField_165) * num15;
					num20 = cubismType_10.CubismField_165 + (cubismType_11.CubismField_165 - cubismType_10.CubismField_165) * num15;
					num21 = cubismType_12.CubismField_165 + (cubismType_13.CubismField_165 - cubismType_12.CubismField_165) * num15;
					num22 = cubismType_14.CubismField_165 + (cubismType_15.CubismField_165 - cubismType_14.CubismField_165) * num15;
					num23 = cubismType_16.CubismField_165 + (cubismType_17.CubismField_165 - cubismType_16.CubismField_165) * num15;
					num24 = cubismType_18.CubismField_165 + (cubismType_19.CubismField_165 - cubismType_18.CubismField_165) * num15;
					num25 = cubismType_20.CubismField_165 + (cubismType_21.CubismField_165 - cubismType_20.CubismField_165) * num15;
					num26 = cubismType_22.CubismField_165 + (cubismType_23.CubismField_165 - cubismType_22.CubismField_165) * num15;
					cubismType_.CubismField_187.CubismField_165 = (1f - num18) * ((1f - num17) * (num19 + (num20 - num19) * num16) + num17 * (num21 + (num22 - num21) * num16)) + num18 * ((1f - num17) * (num23 + (num24 - num23) * num16) + num17 * (num25 + (num26 - num25) * num16));
					num19 = cubismType_8.CubismField_166 + (cubismType_9.CubismField_166 - cubismType_8.CubismField_166) * num15;
					num20 = cubismType_10.CubismField_166 + (cubismType_11.CubismField_166 - cubismType_10.CubismField_166) * num15;
					num21 = cubismType_12.CubismField_166 + (cubismType_13.CubismField_166 - cubismType_12.CubismField_166) * num15;
					num22 = cubismType_14.CubismField_166 + (cubismType_15.CubismField_166 - cubismType_14.CubismField_166) * num15;
					num23 = cubismType_16.CubismField_166 + (cubismType_17.CubismField_166 - cubismType_16.CubismField_166) * num15;
					num24 = cubismType_18.CubismField_166 + (cubismType_19.CubismField_166 - cubismType_18.CubismField_166) * num15;
					num25 = cubismType_20.CubismField_166 + (cubismType_21.CubismField_166 - cubismType_20.CubismField_166) * num15;
					num26 = cubismType_22.CubismField_166 + (cubismType_23.CubismField_166 - cubismType_22.CubismField_166) * num15;
					cubismType_.CubismField_187.CubismField_166 = (1f - num18) * ((1f - num17) * (num19 + (num20 - num19) * num16) + num17 * (num21 + (num22 - num21) * num16)) + num18 * ((1f - num17) * (num23 + (num24 - num23) * num16) + num17 * (num25 + (num26 - num25) * num16));
					break;
				}
				default:
				{
					int num2 = (int)Math.Pow(2.0, num);
					float[] array3 = new float[num2];
					for (int i = 0; i < num2; i++)
					{
						int num3 = i;
						float num4 = 1f;
						for (int j = 0; j < num; j++)
						{
							num4 *= ((num3 % 2 == 0) ? (1f - array2[j]) : array2[j]);
							num3 /= 2;
						}
						array3[i] = num4;
					}
					CubismType_24[] array4 = new CubismType_24[num2];
					for (int k = 0; k < num2; k++)
					{
						array4[k] = CubismField_170[array[k]];
					}
					float num5 = 0f;
					float num6 = 0f;
					float num7 = 0f;
					float num8 = 0f;
					float num9 = 0f;
					for (int l = 0; l < num2; l++)
					{
						num5 += array3[l] * array4[l].CubismField_162;
						num6 += array3[l] * array4[l].CubismField_163;
						num7 += array3[l] * array4[l].CubismField_164;
						num8 += array3[l] * array4[l].CubismField_165;
						num9 += array3[l] * array4[l].CubismField_166;
					}
					cubismType_.CubismField_187.CubismField_162 = num5;
					cubismType_.CubismField_187.CubismField_163 = num6;
					cubismType_.CubismField_187.CubismField_164 = num7;
					cubismType_.CubismField_187.CubismField_165 = num8;
					cubismType_.CubismField_187.CubismField_166 = num9;
					break;
				}
				}
			}
			CubismType_24 cubismType_32 = CubismField_170[array[0]];
			cubismType_.CubismField_187.CubismField_167 = cubismType_32.CubismField_167;
			cubismType_.CubismField_187.CubismField_168 = cubismType_32.CubismField_168;
		}

		public override void setupTransform(ModelContext argument0, IBaseContext argument1)
		{
			CubismType_28 cubismType_ = (CubismType_28)argument1;
			cubismType_.setAvailable(argument0: true);
			if (!needTransform())
			{
				cubismType_.setTotalScale_notForClient(cubismType_.CubismField_187.CubismField_164);
				cubismType_.setTotalOpacity(cubismType_.getInterpolatedOpacity());
				return;
			}
			BaseDataID targetBaseDataID = getTargetBaseDataID();
			if (cubismType_.CubismField_186 == -2)
			{
				cubismType_.CubismField_186 = argument0.getBaseDataIndex(targetBaseDataID);
			}
			if (cubismType_.CubismField_186 < 0)
			{
				cubismType_.setAvailable(argument0: false);
				return;
			}
			IBaseData baseData = argument0.getBaseData(cubismType_.CubismField_186);
			if (baseData != null)
			{
				IBaseContext baseContext = argument0.getBaseContext(cubismType_.CubismField_186);
				float[] cubismField_ = CubismField_171;
				cubismField_[0] = cubismType_.CubismField_187.CubismField_162;
				cubismField_[1] = cubismType_.CubismField_187.CubismField_163;
				float[] cubismField_2 = CubismField_172;
				cubismField_2[0] = 0f;
				cubismField_2[1] = -0.1f;
				int type = baseContext.getSrcPtr().getType();
				if (type == 1 || (type == 2 && argument0.isDrawMethodVersion_2_0()))
				{
					cubismField_2[1] = -10f;
				}
				else
				{
					cubismField_2[1] = -0.1f;
				}
				float[] cubismField_3 = CubismField_173;
				_0020(argument0, baseData, baseContext, cubismField_, cubismField_2, cubismField_3);
				float num = (float)UtMath.getAngleNotAbs(cubismField_2, cubismField_3);
				baseData.transformPoints(argument0, baseContext, cubismField_, cubismField_, 1, 0, 2);
				cubismType_.CubismField_188.CubismField_162 = cubismField_[0];
				cubismType_.CubismField_188.CubismField_163 = cubismField_[1];
				cubismType_.CubismField_188.CubismField_164 = cubismType_.CubismField_187.CubismField_164;
				cubismType_.CubismField_188.CubismField_165 = cubismType_.CubismField_187.CubismField_165;
				cubismType_.CubismField_188.CubismField_166 = cubismType_.CubismField_187.CubismField_166 - num * UtMath.RAD_TO_DEG_F;
				float totalScale = baseContext.getTotalScale();
				cubismType_.setTotalScale_notForClient(totalScale * cubismType_.CubismField_188.CubismField_164);
				float totalOpacity = baseContext.getTotalOpacity();
				cubismType_.setTotalOpacity(totalOpacity * cubismType_.getInterpolatedOpacity());
				cubismType_.CubismField_188.CubismField_167 = cubismType_.CubismField_187.CubismField_167;
				cubismType_.CubismField_188.CubismField_168 = cubismType_.CubismField_187.CubismField_168;
				cubismType_.setAvailable(baseContext.isAvailable());
			}
			else
			{
				cubismType_.setAvailable(argument0: false);
			}
		}

		public override void transformPoints(ModelContext argument0, IBaseContext argument1, float[] argument2, float[] argument3, int argument4, int argument5, int argument6)
		{
			CubismType_28 cubismType_ = (CubismType_28)argument1;
			CubismType_24 cubismType_2 = ((cubismType_.CubismField_188 != null) ? cubismType_.CubismField_188 : cubismType_.CubismField_187);
			float num = (float)Math.Sin(UtMath.DEG_TO_RAD_F * cubismType_2.CubismField_166);
			float num2 = (float)Math.Cos(UtMath.DEG_TO_RAD_F * cubismType_2.CubismField_166);
			float totalScale = cubismType_.getTotalScale();
			float num3 = ((!cubismType_2.CubismField_167) ? 1 : (-1));
			float num4 = ((!cubismType_2.CubismField_168) ? 1 : (-1));
			float num5 = num2 * totalScale * num3;
			float num6 = (0f - num) * totalScale * num4;
			float num7 = num * totalScale * num3;
			float num8 = num2 * totalScale * num4;
			float cubismField_ = cubismType_2.CubismField_162;
			float cubismField_2 = cubismType_2.CubismField_163;
			int num9 = argument4 * argument6;
			for (int i = argument5; i < num9; i += argument6)
			{
				float num10 = argument2[i];
				float num11 = argument2[i + 1];
				argument3[i] = num5 * num10 + num6 * num11 + cubismField_;
				argument3[i + 1] = num7 * num10 + num8 * num11 + cubismField_2;
			}
		}

		private void _0020(ModelContext argument0, IBaseData argument1, IBaseContext argument2, float[] argument3, float[] argument4, float[] argument5)
		{
			float[] cubismField_ = CubismField_174;
			CubismField_174[0] = argument3[0];
			CubismField_174[1] = argument3[1];
			argument1.transformPoints(argument0, argument2, cubismField_, cubismField_, 1, 0, 2);
			float[] cubismField_2 = CubismField_175;
			float[] cubismField_3 = CubismField_176;
			int num = 10;
			float num2 = 1f;
			for (int i = 0; i < num; i++)
			{
				cubismField_3[0] = argument3[0] + num2 * argument4[0];
				cubismField_3[1] = argument3[1] + num2 * argument4[1];
				argument1.transformPoints(argument0, argument2, cubismField_3, cubismField_2, 1, 0, 2);
				cubismField_2[0] -= cubismField_[0];
				cubismField_2[1] -= cubismField_[1];
				if (cubismField_2[0] != 0f || cubismField_2[1] != 0f)
				{
					argument5[0] = cubismField_2[0];
					argument5[1] = cubismField_2[1];
					break;
				}
				cubismField_3[0] = argument3[0] - num2 * argument4[0];
				cubismField_3[1] = argument3[1] - num2 * argument4[1];
				argument1.transformPoints(argument0, argument2, cubismField_3, cubismField_2, 1, 0, 2);
				cubismField_2[0] -= cubismField_[0];
				cubismField_2[1] -= cubismField_[1];
				if (cubismField_2[0] != 0f || cubismField_2[1] != 0f)
				{
					cubismField_2[0] = 0f - cubismField_2[0];
					cubismField_2[0] = 0f - cubismField_2[0];
					argument5[0] = cubismField_2[0];
					argument5[1] = cubismField_2[1];
					break;
				}
				num2 *= 0.1f;
			}
		}
	}
	public class PivotManager : ISerializableV2
	{
		private List<ParamPivots> CubismField_178;

		internal PivotManager()
		{
		}

		internal void _3000_0020_0020()
		{
			CubismField_178 = new List<ParamPivots>();
		}

		public void readV2(BReader argument0)
		{
			List<object> list = argument0.readObject() as List<object>;
			CubismField_178 = new List<ParamPivots>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_178.Add((ParamPivots)list[i]);
			}
		}

		internal bool _3000_3000(ModelContext argument0)
		{
			if (argument0.requireSetup())
			{
				return true;
			}
			int initVersion = argument0.getInitVersion();
			for (int num = CubismField_178.Count - 1; num >= 0; num--)
			{
				int num2 = CubismField_178[num]._3000(initVersion);
				if (num2 == -2)
				{
					num2 = argument0.getParamIndex(CubismField_178[num]._0020());
				}
				if (argument0._3000_3000(num2))
				{
					return true;
				}
			}
			return false;
		}

		internal int _3000_0020(ModelContext argument0, bool[] argument1)
		{
			int count = CubismField_178.Count;
			int initVersion = argument0.getInitVersion();
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				ParamPivots paramPivots = CubismField_178[i];
				int num2 = paramPivots._3000(initVersion);
				if (num2 == -2)
				{
					num2 = argument0.getParamIndex(paramPivots._0020());
					paramPivots._3000_3000_3000(num2, initVersion);
				}
				if (num2 < 0)
				{
					throw new Exception("PivotManager#calcPivotValue() :tmpParamIndex < 0 " + paramPivots._0020());
				}
				float num3 = ((num2 < 0) ? 0f : argument0.getParamFloat(num2));
				int num4 = paramPivots._3000_0020();
				float[] array = paramPivots._3000_3000();
				int argument2 = -1;
				float argument3 = 0f;
				if (num4 >= 1)
				{
					if (num4 == 1)
					{
						float num5 = array[0];
						if (num5 - 0.0001f < num3 && num3 < num5 + 0.0001f)
						{
							argument2 = 0;
							argument3 = 0f;
						}
						else
						{
							argument2 = 0;
							argument1[0] = true;
						}
					}
					else
					{
						float num5 = array[0];
						if (num3 < num5 - 0.0001f)
						{
							argument2 = 0;
							argument1[0] = true;
						}
						else if (num3 < num5 + 0.0001f)
						{
							argument2 = 0;
						}
						else
						{
							bool flag = false;
							for (int j = 1; j < num4; j++)
							{
								float num6 = array[j];
								if (num3 < num6 + 0.0001f)
								{
									if (num6 - 0.0001f < num3)
									{
										argument2 = j;
									}
									else
									{
										argument2 = j - 1;
										argument3 = (num3 - num5) / (num6 - num5);
										num++;
									}
									flag = true;
									break;
								}
								num5 = num6;
							}
							if (!flag)
							{
								argument2 = num4 - 1;
								argument3 = 0f;
								argument1[0] = true;
							}
						}
					}
				}
				paramPivots._3000_0020_0020_3000(argument2);
				paramPivots._3000_0020_3000_0020(argument3);
			}
			return num;
		}

		internal void _3000(short[] argument0, float[] argument1, int argument2)
		{
			int num = 1 << argument2;
			if (num + 1 > 65)
			{
				UtDebug.print("=================================================\n");
				UtDebug.print("Over the interpolation buffer size {0}\n ", num);
				UtDebug.print("please enlarge Live2D.h / PIVOT_TABLE_SIZE\n");
				UtDebug.print("=================================================\n");
			}
			int count = CubismField_178.Count;
			int num2 = 1;
			int num3 = 1;
			int num4 = 0;
			for (int i = 0; i < num; i++)
			{
				argument0[i] = 0;
			}
			for (int j = 0; j < count; j++)
			{
				ParamPivots paramPivots = CubismField_178[j];
				if (paramPivots._3000_0020_3000() == 0f)
				{
					int num5 = paramPivots._3000_0020_0020() * num2;
					_ = 0;
					for (int k = 0; k < num; k++)
					{
						argument0[k] += (short)num5;
					}
				}
				else
				{
					int num6 = num2 * paramPivots._3000_0020_0020();
					int num7 = num2 * (paramPivots._3000_0020_0020() + 1);
					for (int l = 0; l < num; l++)
					{
						argument0[l] += ((l / num3 % 2 == 0) ? ((short)num6) : ((short)num7));
					}
					argument1[num4++] = paramPivots._3000_0020_3000();
					num3 *= 2;
				}
				num2 *= paramPivots._3000_0020();
			}
			argument0[num] = short.MaxValue;
			argument1[num4] = -1f;
		}

		internal void _0020(ParamID argument0, int argument1, float[] argument2)
		{
			float[] array = new float[argument1];
			for (int i = 0; i < argument1; i++)
			{
				array[i] = argument2[i];
			}
			ParamPivots paramPivots = new ParamPivots();
			paramPivots._3000_3000_0020(argument0);
			paramPivots._3000_0020_0020_0020(argument1, array);
			CubismField_178.Add(paramPivots);
		}

		public int getParamCount()
		{
			return CubismField_178.Count;
		}

		public List<ParamPivots> getParamPivotTableRef()
		{
			return CubismField_178;
		}
	}
	public class IBaseContext
	{
		private IBaseData CubismField_179;

		private int CubismField_180;

		protected bool CubismField_181;

		protected bool CubismField_182 = true;

		protected float CubismField_183 = 1f;

		protected float CubismField_184 = 1f;

		protected float CubismField_185 = 1f;

		public bool isAvailable()
		{
			if (CubismField_182)
			{
				return !CubismField_181;
			}
			return false;
		}

		public void setAvailable(bool argument0)
		{
			CubismField_182 = argument0;
		}

		public IBaseContext(IBaseData argument0)
		{
			CubismField_179 = argument0;
		}

		public IBaseData getSrcPtr()
		{
			return CubismField_179;
		}

		public void setPartsIndex(int argument0)
		{
			CubismField_180 = argument0;
		}

		public int getPartsIndex()
		{
			return CubismField_180;
		}

		public bool isOutsideParam()
		{
			return CubismField_181;
		}

		public void setOutsideParam(bool argument0)
		{
			CubismField_181 = argument0;
		}

		public float getTotalScale()
		{
			return CubismField_183;
		}

		public void setTotalScale_notForClient(float argument0)
		{
			CubismField_183 = argument0;
		}

		public float getInterpolatedOpacity()
		{
			return CubismField_184;
		}

		public void setInterpolatedOpacity(float argument0)
		{
			CubismField_184 = argument0;
		}

		public float getTotalOpacity()
		{
			return CubismField_185;
		}

		public void setTotalOpacity(float argument0)
		{
			CubismField_185 = argument0;
		}
	}
	internal class CubismType_28 : IBaseContext
	{
		internal int CubismField_186 = -2;

		internal CubismType_24 CubismField_187;

		internal CubismType_24 CubismField_188;

		internal CubismType_28(CubismType_25 argument0)
			: base(argument0)
		{
		}
	}
	public class BaseDataID : ID
	{
		private static BaseDataID CubismField_189 = null;

		private static Dictionary<string, BaseDataID> CubismField_190 = new Dictionary<string, BaseDataID>();

		private static readonly object CubismField_191 = new object();

		private BaseDataID()
		{
		}

		private BaseDataID(string argument0)
		{
			CubismField_150 = argument0;
		}

		public static BaseDataID DST_BASE_ID()
		{
			if (CubismField_189 == null)
			{
				CubismField_189 = getID("DST_BASE");
			}
			return CubismField_189;
		}

		public BaseDataID createIDForSerialize()
		{
			return new BaseDataID();
		}

		internal static void _0020()
		{
			CubismField_190.Clear();
			CubismField_189 = null;
		}

		public static BaseDataID getID(string argument0)
		{
			if (CubismField_190.ContainsKey(argument0))
			{
				return CubismField_190[argument0];
			}
			BaseDataID baseDataID = new BaseDataID(argument0);
			CubismField_190.Add(argument0, baseDataID);
			return baseDataID;
		}

		public static BaseDataID getIDAsync(string argument0)
		{
			Monitor.Enter(CubismField_191);
			if (CubismField_190.ContainsKey(argument0))
			{
				Monitor.Exit(CubismField_191);
				return CubismField_190[argument0];
			}
			BaseDataID baseDataID = new BaseDataID(argument0);
			CubismField_190.Add(argument0, baseDataID);
			Monitor.Exit(CubismField_191);
			return baseDataID;
		}
	}
	public class UtMath
	{
		public const double M_PI = Math.PI;

		private static System.Random CubismField_193 = new System.Random();

		public static double DEG_TO_RAD_D = Math.PI / 180.0;

		public static float DEG_TO_RAD_F = (float)Math.PI / 180f;

		public static double RAD_TO_DEG_D = 180.0 / Math.PI;

		public static float RAD_TO_DEG_F = 57.29578f;

		public static float PI_F = (float)Math.PI;

		private static readonly double[] CubismField_199 = new double[128]
		{
			0.0, 0.012368, 0.024734, 0.037097, 0.049454, 0.061803, 0.074143, 0.086471, 0.098786, 0.111087,
			0.12337, 0.135634, 0.147877, 0.160098, 0.172295, 0.184465, 0.196606, 0.208718, 0.220798, 0.232844,
			0.244854, 0.256827, 0.268761, 0.280654, 0.292503, 0.304308, 0.316066, 0.327776, 0.339436, 0.351044,
			0.362598, 0.374097, 0.385538, 0.396921, 0.408243, 0.419502, 0.430697, 0.441826, 0.452888, 0.463881,
			0.474802, 0.485651, 0.496425, 0.507124, 0.517745, 0.528287, 0.538748, 0.549126, 0.559421, 0.56963,
			0.579752, 0.589785, 0.599728, 0.609579, 0.619337, 0.629, 0.638567, 0.648036, 0.657406, 0.666676,
			0.675843, 0.684908, 0.693867, 0.70272, 0.711466, 0.720103, 0.72863, 0.737045, 0.745348, 0.753536,
			0.76161, 0.769566, 0.777405, 0.785125, 0.792725, 0.800204, 0.807561, 0.814793, 0.821901, 0.828884,
			0.835739, 0.842467, 0.849066, 0.855535, 0.861873, 0.868079, 0.874153, 0.880093, 0.885898, 0.891567,
			0.897101, 0.902497, 0.907754, 0.912873, 0.917853, 0.922692, 0.92739, 0.931946, 0.936359, 0.940629,
			0.944755, 0.948737, 0.952574, 0.956265, 0.959809, 0.963207, 0.966457, 0.96956, 0.972514, 0.97532,
			0.977976, 0.980482, 0.982839, 0.985045, 0.987101, 0.989006, 0.990759, 0.992361, 0.993811, 0.995109,
			0.996254, 0.997248, 0.998088, 0.998776, 0.999312, 0.999694, 0.999924, 1.0
		};

		public static double getAngleNotAbs(float[] argument0, float[] argument1)
		{
			double argument2 = Math.Atan2(argument0[1], argument0[0]);
			double argument3 = Math.Atan2(argument1[1], argument1[0]);
			return getAngleDiff(argument2, argument3);
		}

		public static double getAngleDiff(double argument0, double argument1)
		{
			double num;
			for (num = argument0 - argument1; num < -Math.PI; num += Math.PI * 2.0)
			{
			}
			while (num > Math.PI)
			{
				num -= Math.PI * 2.0;
			}
			return num;
		}

		public static double fsin(double argument0)
		{
			return Math.Sin(argument0);
		}

		public static float range(float argument0, float argument1, float argument2)
		{
			if (argument0 < argument1)
			{
				return argument1;
			}
			if (argument0 > argument2)
			{
				return argument2;
			}
			return argument0;
		}

		public static double randDouble()
		{
			return CubismField_193.NextDouble();
		}

		public static int rand(int argument0)
		{
			return CubismField_193.Next(argument0);
		}

		public float map(float argument0, float argument1, float argument2, float argument3, float argument4)
		{
			float num = (argument0 - argument1) / (argument2 - argument1);
			return (argument4 - argument3) * num + argument3;
		}
	}
	internal class CubismType_31 : IBaseData
	{
		internal class CubismType_32 : IBaseContext
		{
			internal int CubismField_205 = -2;

			internal float[] CubismField_206;

			internal float[] CubismField_207;

			internal CubismType_32(CubismType_31 argument0)
				: base(argument0)
			{
			}
		}

		private int CubismField_200;

		private int CubismField_201;

		private PivotManager CubismField_202;

		private List<float[]> CubismField_203;

		private static bool[] CubismField_204 = new bool[1];

		internal CubismType_31()
		{
		}

		internal void _3000()
		{
			CubismField_202 = new PivotManager();
			CubismField_202._3000_0020_0020();
		}

		public override void readV2(BReader argument0)
		{
			base.readV2(argument0);
			CubismField_201 = argument0.readInt();
			CubismField_200 = argument0.readInt();
			CubismField_202 = (PivotManager)argument0.readObject();
			List<object> list = argument0.readObject() as List<object>;
			CubismField_203 = new List<float[]>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_203.Add((float[])list[i]);
			}
			readV2_opacity(argument0);
		}

		public override IBaseContext init(ModelContext argument0)
		{
			CubismType_32 cubismType_ = new CubismType_32(this);
			int num = (CubismField_200 + 1) * (CubismField_201 + 1);
			if (cubismType_.CubismField_206 != null)
			{
				cubismType_.CubismField_206 = null;
			}
			cubismType_.CubismField_206 = new float[num * 2];
			if (cubismType_.CubismField_207 != null)
			{
				cubismType_.CubismField_207 = null;
			}
			if (needTransform())
			{
				cubismType_.CubismField_207 = new float[num * 2];
			}
			else
			{
				cubismType_.CubismField_207 = null;
			}
			return cubismType_;
		}

		public override void setupInterpolate(ModelContext argument0, IBaseContext argument1)
		{
			CubismType_32 cubismType_ = (CubismType_32)argument1;
			if (CubismField_202._3000_3000(argument0))
			{
				int argument2 = _0020();
				bool[] cubismField_ = CubismField_204;
				cubismField_[0] = false;
				CubismType_33._3000_0020(argument0, CubismField_202, cubismField_, argument2, CubismField_203, cubismType_.CubismField_206, 0, 2);
				argument1.setOutsideParam(cubismField_[0]);
				interpolateOpacity(argument0, CubismField_202, argument1, cubismField_);
			}
		}

		public override void setupTransform(ModelContext argument0, IBaseContext argument1)
		{
			CubismType_32 cubismType_ = (CubismType_32)argument1;
			cubismType_.setAvailable(argument0: true);
			if (!needTransform())
			{
				cubismType_.setTotalOpacity(cubismType_.getInterpolatedOpacity());
				return;
			}
			BaseDataID targetBaseDataID = getTargetBaseDataID();
			if (cubismType_.CubismField_205 == -2)
			{
				cubismType_.CubismField_205 = argument0.getBaseDataIndex(targetBaseDataID);
			}
			if (cubismType_.CubismField_205 < 0)
			{
				cubismType_.setAvailable(argument0: false);
				return;
			}
			IBaseData baseData = argument0.getBaseData(cubismType_.CubismField_205);
			IBaseContext baseContext = argument0.getBaseContext(cubismType_.CubismField_205);
			if (baseData != null && baseContext.isAvailable())
			{
				float totalScale = baseContext.getTotalScale();
				cubismType_.setTotalScale_notForClient(totalScale);
				float totalOpacity = baseContext.getTotalOpacity();
				cubismType_.setTotalOpacity(totalOpacity * cubismType_.getInterpolatedOpacity());
				baseData.transformPoints(argument0, baseContext, cubismType_.CubismField_206, cubismType_.CubismField_207, _0020(), 0, 2);
				cubismType_.setAvailable(argument0: true);
			}
			else
			{
				cubismType_.setAvailable(argument0: false);
			}
		}

		public override void transformPoints(ModelContext argument0, IBaseContext argument1, float[] argument2, float[] argument3, int argument4, int argument5, int argument6)
		{
			CubismType_32 cubismType_ = (CubismType_32)argument1;
			float[] argument7 = ((cubismType_.CubismField_207 != null) ? cubismType_.CubismField_207 : cubismType_.CubismField_206);
			_3000_3000(argument2, argument3, argument4, argument5, argument6, argument7, CubismField_200, CubismField_201);
		}

		public static void _3000_3000(float[] argument0, float[] argument1, int argument2, int argument3, int argument4, float[] argument5, int argument6, int argument7)
		{
			int num = argument2 * argument4;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			bool flag = false;
			for (int i = argument3; i < num; i += argument4)
			{
				float num8 = argument0[i];
				float num9 = argument0[i + 1];
				float num10 = num8 * (float)argument6;
				float num11 = num9 * (float)argument7;
				if (num10 < 0f || num11 < 0f || (float)argument6 <= num10 || (float)argument7 <= num11)
				{
					int num12 = argument6 + 1;
					if (!flag)
					{
						flag = true;
						num2 = 0.25f * (argument5[0] + argument5[argument6 * 2] + argument5[argument7 * num12 * 2] + argument5[(argument6 + argument7 * num12) * 2]);
						num3 = 0.25f * (argument5[1] + argument5[argument6 * 2 + 1] + argument5[argument7 * num12 * 2 + 1] + argument5[(argument6 + argument7 * num12) * 2 + 1]);
						float num13 = argument5[(argument6 + argument7 * num12) * 2] - argument5[0];
						float num14 = argument5[(argument6 + argument7 * num12) * 2 + 1] - argument5[1];
						float num15 = argument5[argument6 * 2] - argument5[argument7 * num12 * 2];
						float num16 = argument5[argument6 * 2 + 1] - argument5[argument7 * num12 * 2 + 1];
						num4 = (num13 + num15) * 0.5f;
						num5 = (num14 + num16) * 0.5f;
						num6 = (num13 - num15) * 0.5f;
						num7 = (num14 - num16) * 0.5f;
						if (num4 == 0f)
						{
							_ = 0f;
						}
						if (num6 == 0f)
						{
							_ = 0f;
						}
						num2 -= 0.5f * (num4 + num6);
						num3 -= 0.5f * (num5 + num7);
					}
					if (-2f < num8 && num8 < 3f && -2f < num9 && num9 < 3f)
					{
						if (num8 <= 0f)
						{
							if (num9 <= 0f)
							{
								float num17 = argument5[0];
								float num18 = argument5[1];
								float num19 = num2 - 2f * num4;
								float num20 = num3 - 2f * num5;
								float num21 = num2 - 2f * num6;
								float num22 = num3 - 2f * num7;
								float num23 = num2 - 2f * num4 - 2f * num6;
								float num24 = num3 - 2f * num5 - 2f * num7;
								float num25 = 0.5f * (num8 - -2f);
								float num26 = 0.5f * (num9 - -2f);
								if (num25 + num26 <= 1f)
								{
									argument1[i] = num23 + (num21 - num23) * num25 + (num19 - num23) * num26;
									argument1[i + 1] = num24 + (num22 - num24) * num25 + (num20 - num24) * num26;
								}
								else
								{
									argument1[i] = num17 + (num19 - num17) * (1f - num25) + (num21 - num17) * (1f - num26);
									argument1[i + 1] = num18 + (num20 - num18) * (1f - num25) + (num22 - num18) * (1f - num26);
								}
								continue;
							}
							if (num9 >= 1f)
							{
								float num27 = argument5[argument7 * num12 * 2];
								float num28 = argument5[argument7 * num12 * 2 + 1];
								float num29 = num2 - 2f * num4 + 1f * num6;
								float num30 = num3 - 2f * num5 + 1f * num7;
								float num31 = num2 + 3f * num6;
								float num32 = num3 + 3f * num7;
								float num33 = num2 - 2f * num4 + 3f * num6;
								float num34 = num3 - 2f * num5 + 3f * num7;
								float num35 = 0.5f * (num8 - -2f);
								float num36 = 0.5f * (num9 - 1f);
								if (num35 + num36 <= 1f)
								{
									argument1[i] = num29 + (num27 - num29) * num35 + (num33 - num29) * num36;
									argument1[i + 1] = num30 + (num28 - num30) * num35 + (num34 - num30) * num36;
								}
								else
								{
									argument1[i] = num31 + (num33 - num31) * (1f - num35) + (num27 - num31) * (1f - num36);
									argument1[i + 1] = num32 + (num34 - num32) * (1f - num35) + (num28 - num32) * (1f - num36);
								}
								continue;
							}
							int num37 = (int)num11;
							if (num37 == argument7)
							{
								num37 = argument7 - 1;
							}
							float num38 = 0.5f * (num8 - -2f);
							float num39 = num11 - (float)num37;
							float num40 = (float)num37 / (float)argument7;
							float num41 = (float)(num37 + 1) / (float)argument7;
							float num42 = argument5[num37 * num12 * 2];
							float num43 = argument5[num37 * num12 * 2 + 1];
							float num44 = argument5[(num37 + 1) * num12 * 2];
							float num45 = argument5[(num37 + 1) * num12 * 2 + 1];
							float num46 = num2 - 2f * num4 + num40 * num6;
							float num47 = num3 - 2f * num5 + num40 * num7;
							float num48 = num2 - 2f * num4 + num41 * num6;
							float num49 = num3 - 2f * num5 + num41 * num7;
							if (num38 + num39 <= 1f)
							{
								argument1[i] = num46 + (num42 - num46) * num38 + (num48 - num46) * num39;
								argument1[i + 1] = num47 + (num43 - num47) * num38 + (num49 - num47) * num39;
							}
							else
							{
								argument1[i] = num44 + (num48 - num44) * (1f - num38) + (num42 - num44) * (1f - num39);
								argument1[i + 1] = num45 + (num49 - num45) * (1f - num38) + (num43 - num45) * (1f - num39);
							}
						}
						else if (1f <= num8)
						{
							if (num9 <= 0f)
							{
								float num50 = argument5[argument6 * 2];
								float num51 = argument5[argument6 * 2 + 1];
								float num52 = num2 + 3f * num4;
								float num53 = num3 + 3f * num5;
								float num54 = num2 + 1f * num4 - 2f * num6;
								float num55 = num3 + 1f * num5 - 2f * num7;
								float num56 = num2 + 3f * num4 - 2f * num6;
								float num57 = num3 + 3f * num5 - 2f * num7;
								float num58 = 0.5f * (num8 - 1f);
								float num59 = 0.5f * (num9 - -2f);
								if (num58 + num59 <= 1f)
								{
									argument1[i] = num54 + (num56 - num54) * num58 + (num50 - num54) * num59;
									argument1[i + 1] = num55 + (num57 - num55) * num58 + (num51 - num55) * num59;
								}
								else
								{
									argument1[i] = num52 + (num50 - num52) * (1f - num58) + (num56 - num52) * (1f - num59);
									argument1[i + 1] = num53 + (num51 - num53) * (1f - num58) + (num57 - num53) * (1f - num59);
								}
								continue;
							}
							if (num9 >= 1f)
							{
								float num60 = argument5[(argument6 + argument7 * num12) * 2];
								float num61 = argument5[(argument6 + argument7 * num12) * 2 + 1];
								float num62 = num2 + 3f * num4 + 1f * num6;
								float num63 = num3 + 3f * num5 + 1f * num7;
								float num64 = num2 + 1f * num4 + 3f * num6;
								float num65 = num3 + 1f * num5 + 3f * num7;
								float num66 = num2 + 3f * num4 + 3f * num6;
								float num67 = num3 + 3f * num5 + 3f * num7;
								float num68 = 0.5f * (num8 - 1f);
								float num69 = 0.5f * (num9 - 1f);
								if (num68 + num69 <= 1f)
								{
									argument1[i] = num60 + (num62 - num60) * num68 + (num64 - num60) * num69;
									argument1[i + 1] = num61 + (num63 - num61) * num68 + (num65 - num61) * num69;
								}
								else
								{
									argument1[i] = num66 + (num64 - num66) * (1f - num68) + (num62 - num66) * (1f - num69);
									argument1[i + 1] = num67 + (num65 - num67) * (1f - num68) + (num63 - num67) * (1f - num69);
								}
								continue;
							}
							int num70 = (int)num11;
							if (num70 == argument7)
							{
								num70 = argument7 - 1;
							}
							float num71 = 0.5f * (num8 - 1f);
							float num72 = num11 - (float)num70;
							float num73 = (float)num70 / (float)argument7;
							float num74 = (float)(num70 + 1) / (float)argument7;
							float num75 = argument5[(argument6 + num70 * num12) * 2];
							float num76 = argument5[(argument6 + num70 * num12) * 2 + 1];
							float num77 = argument5[(argument6 + (num70 + 1) * num12) * 2];
							float num78 = argument5[(argument6 + (num70 + 1) * num12) * 2 + 1];
							float num79 = num2 + 3f * num4 + num73 * num6;
							float num80 = num3 + 3f * num5 + num73 * num7;
							float num81 = num2 + 3f * num4 + num74 * num6;
							float num82 = num3 + 3f * num5 + num74 * num7;
							if (num71 + num72 <= 1f)
							{
								argument1[i] = num75 + (num79 - num75) * num71 + (num77 - num75) * num72;
								argument1[i + 1] = num76 + (num80 - num76) * num71 + (num78 - num76) * num72;
							}
							else
							{
								argument1[i] = num81 + (num77 - num81) * (1f - num71) + (num79 - num81) * (1f - num72);
								argument1[i + 1] = num82 + (num78 - num82) * (1f - num71) + (num80 - num82) * (1f - num72);
							}
						}
						else if (num9 <= 0f)
						{
							int num83 = (int)num10;
							if (num83 == argument6)
							{
								num83 = argument6 - 1;
							}
							float num84 = num10 - (float)num83;
							float num85 = 0.5f * (num9 - -2f);
							float num86 = (float)num83 / (float)argument6;
							float num87 = (float)(num83 + 1) / (float)argument6;
							float num88 = argument5[num83 * 2];
							float num89 = argument5[num83 * 2 + 1];
							float num90 = argument5[(num83 + 1) * 2];
							float num91 = argument5[(num83 + 1) * 2 + 1];
							float num92 = num2 + num86 * num4 - 2f * num6;
							float num93 = num3 + num86 * num5 - 2f * num7;
							float num94 = num2 + num87 * num4 - 2f * num6;
							float num95 = num3 + num87 * num5 - 2f * num7;
							if (num84 + num85 <= 1f)
							{
								argument1[i] = num92 + (num94 - num92) * num84 + (num88 - num92) * num85;
								argument1[i + 1] = num93 + (num95 - num93) * num84 + (num89 - num93) * num85;
							}
							else
							{
								argument1[i] = num90 + (num88 - num90) * (1f - num84) + (num94 - num90) * (1f - num85);
								argument1[i + 1] = num91 + (num89 - num91) * (1f - num84) + (num95 - num91) * (1f - num85);
							}
						}
						else if (num9 >= 1f)
						{
							int num96 = (int)num10;
							if (num96 == argument6)
							{
								num96 = argument6 - 1;
							}
							float num97 = num10 - (float)num96;
							float num98 = 0.5f * (num9 - 1f);
							float num99 = (float)num96 / (float)argument6;
							float num100 = (float)(num96 + 1) / (float)argument6;
							float num101 = argument5[(num96 + argument7 * num12) * 2];
							float num102 = argument5[(num96 + argument7 * num12) * 2 + 1];
							float num103 = argument5[(num96 + 1 + argument7 * num12) * 2];
							float num104 = argument5[(num96 + 1 + argument7 * num12) * 2 + 1];
							float num105 = num2 + num99 * num4 + 3f * num6;
							float num106 = num3 + num99 * num5 + 3f * num7;
							float num107 = num2 + num100 * num4 + 3f * num6;
							float num108 = num3 + num100 * num5 + 3f * num7;
							if (num97 + num98 <= 1f)
							{
								argument1[i] = num101 + (num103 - num101) * num97 + (num105 - num101) * num98;
								argument1[i + 1] = num102 + (num104 - num102) * num97 + (num106 - num102) * num98;
							}
							else
							{
								argument1[i] = num107 + (num105 - num107) * (1f - num97) + (num103 - num107) * (1f - num98);
								argument1[i + 1] = num108 + (num106 - num108) * (1f - num97) + (num104 - num108) * (1f - num98);
							}
						}
						else
						{
							UtDebug.error("error calc : %.4f , %.4f\t\t\t\t\t@@BDBoxGrid\n", num8, num9);
						}
					}
					else
					{
						argument1[i] = num2 + num8 * num4 + num9 * num6;
						argument1[i + 1] = num3 + num8 * num5 + num9 * num7;
					}
				}
				else
				{
					float num109 = num10 - (float)(int)num10;
					float num110 = num11 - (float)(int)num11;
					int num111 = 2 * ((int)num10 + (int)num11 * (argument6 + 1));
					if (num109 + num110 < 1f)
					{
						argument1[i] = argument5[num111] * (1f - num109 - num110) + argument5[num111 + 2] * num109 + argument5[num111 + 2 * (argument6 + 1)] * num110;
						argument1[i + 1] = argument5[num111 + 1] * (1f - num109 - num110) + argument5[num111 + 3] * num109 + argument5[num111 + 2 * (argument6 + 1) + 1] * num110;
					}
					else
					{
						argument1[i] = argument5[num111 + 2 * (argument6 + 1) + 2] * (num109 - 1f + num110) + argument5[num111 + 2 * (argument6 + 1)] * (1f - num109) + argument5[num111 + 2] * (1f - num110);
						argument1[i + 1] = argument5[num111 + 2 * (argument6 + 1) + 3] * (num109 - 1f + num110) + argument5[num111 + 2 * (argument6 + 1) + 1] * (1f - num109) + argument5[num111 + 3] * (1f - num110);
					}
				}
			}
		}

		public void _3000_0020(ModelContext argument0, IBaseContext argument1, float[] argument2, float[] argument3, int argument4, int argument5, int argument6)
		{
			CubismType_32 cubismType_ = (CubismType_32)argument1;
			int cubismField_ = CubismField_200;
			int cubismField_2 = CubismField_201;
			int num = argument4 * argument6;
			float[] array = ((cubismType_.CubismField_207 != null) ? cubismType_.CubismField_207 : cubismType_.CubismField_206);
			for (int i = argument5; i < num; i += argument6)
			{
				float num2 = argument2[i];
				float num3 = argument2[i + 1];
				if (num2 < 0f)
				{
					num2 = 0f;
				}
				else if (num2 > 1f)
				{
					num2 = 1f;
				}
				if (num3 < 0f)
				{
					num3 = 0f;
				}
				else if (num3 > 1f)
				{
					num3 = 1f;
				}
				num2 *= (float)cubismField_;
				num3 *= (float)cubismField_2;
				int num4 = (int)num2;
				int num5 = (int)num3;
				if (num4 > cubismField_ - 1)
				{
					num4 = cubismField_ - 1;
				}
				if (num5 > cubismField_2 - 1)
				{
					num5 = cubismField_2 - 1;
				}
				float num6 = num2 - (float)num4;
				float num7 = num3 - (float)num5;
				int num8 = 2 * (num4 + num5 * (cubismField_ + 1));
				if (num6 + num7 < 1f)
				{
					argument3[i] = array[num8] * (1f - num6 - num7) + array[num8 + 2] * num6 + array[num8 + 2 * (cubismField_ + 1)] * num7;
					argument3[i + 1] = array[num8 + 1] * (1f - num6 - num7) + array[num8 + 3] * num6 + array[num8 + 2 * (cubismField_ + 1) + 1] * num7;
				}
				else
				{
					argument3[i] = array[num8 + 2 * (cubismField_ + 1) + 2] * (num6 - 1f + num7) + array[num8 + 2 * (cubismField_ + 1)] * (1f - num6) + array[num8 + 2] * (1f - num7);
					argument3[i + 1] = array[num8 + 2 * (cubismField_ + 1) + 3] * (num6 - 1f + num7) + array[num8 + 2 * (cubismField_ + 1) + 1] * (1f - num6) + array[num8 + 3] * (1f - num7);
				}
			}
		}

		public int _0020()
		{
			return (CubismField_200 + 1) * (CubismField_201 + 1);
		}

		public override int getType()
		{
			return 2;
		}
	}
	internal class CubismType_33
	{
		internal static int _3000(ModelContext argument0, PivotManager argument1, bool[] argument2, int[] argument3)
		{
			int num = argument1._3000_0020(argument0, argument2);
			short[] array = argument0._3000();
			float[] array2 = argument0._3000_0020();
			argument1._3000(array, array2, num);
			if (num <= 0)
			{
				return argument3[array[0]];
			}
			switch (num)
			{
			case 1:
			{
				int num14 = argument3[array[0]];
				int num15 = argument3[array[1]];
				float num16 = array2[0];
				return (int)((float)num14 + (float)(num15 - num14) * num16);
			}
			case 2:
			{
				int num6 = argument3[array[0]];
				int num7 = argument3[array[1]];
				int num8 = argument3[array[2]];
				int num9 = argument3[array[3]];
				float num10 = array2[0];
				float num11 = array2[1];
				int num12 = (int)((float)num6 + (float)(num7 - num6) * num10);
				int num13 = (int)((float)num8 + (float)(num9 - num8) * num10);
				return (int)((float)num12 + (float)(num13 - num12) * num11);
			}
			case 3:
			{
				int num51 = argument3[array[0]];
				int num52 = argument3[array[1]];
				int num53 = argument3[array[2]];
				int num54 = argument3[array[3]];
				int num55 = argument3[array[4]];
				int num56 = argument3[array[5]];
				int num57 = argument3[array[6]];
				int num58 = argument3[array[7]];
				float num59 = array2[0];
				float num60 = array2[1];
				float num61 = array2[2];
				int num62 = (int)((float)num51 + (float)(num52 - num51) * num59);
				int num63 = (int)((float)num53 + (float)(num54 - num53) * num59);
				int num64 = (int)((float)num55 + (float)(num56 - num55) * num59);
				int num65 = (int)((float)num57 + (float)(num58 - num57) * num59);
				int num66 = (int)((float)num62 + (float)(num63 - num62) * num60);
				int num67 = (int)((float)num64 + (float)(num65 - num64) * num60);
				return (int)((float)num66 + (float)(num67 - num66) * num61);
			}
			case 4:
			{
				int num17 = argument3[array[0]];
				int num18 = argument3[array[1]];
				int num19 = argument3[array[2]];
				int num20 = argument3[array[3]];
				int num21 = argument3[array[4]];
				int num22 = argument3[array[5]];
				int num23 = argument3[array[6]];
				int num24 = argument3[array[7]];
				int num25 = argument3[array[8]];
				int num26 = argument3[array[9]];
				int num27 = argument3[array[10]];
				int num28 = argument3[array[11]];
				int num29 = argument3[array[12]];
				int num30 = argument3[array[13]];
				int num31 = argument3[array[14]];
				int num32 = argument3[array[15]];
				float num33 = array2[0];
				float num34 = array2[1];
				float num35 = array2[2];
				float num36 = array2[3];
				int num37 = (int)((float)num17 + (float)(num18 - num17) * num33);
				int num38 = (int)((float)num19 + (float)(num20 - num19) * num33);
				int num39 = (int)((float)num21 + (float)(num22 - num21) * num33);
				int num40 = (int)((float)num23 + (float)(num24 - num23) * num33);
				int num41 = (int)((float)num25 + (float)(num26 - num25) * num33);
				int num42 = (int)((float)num27 + (float)(num28 - num27) * num33);
				int num43 = (int)((float)num29 + (float)(num30 - num29) * num33);
				int num44 = (int)((float)num31 + (float)(num32 - num31) * num33);
				int num45 = (int)((float)num37 + (float)(num38 - num37) * num34);
				int num46 = (int)((float)num39 + (float)(num40 - num39) * num34);
				int num47 = (int)((float)num41 + (float)(num42 - num41) * num34);
				int num48 = (int)((float)num43 + (float)(num44 - num43) * num34);
				int num49 = (int)((float)num45 + (float)(num46 - num45) * num35);
				int num50 = (int)((float)num47 + (float)(num48 - num47) * num35);
				return (int)((float)num49 + (float)(num50 - num49) * num36);
			}
			default:
			{
				int num2 = 1 << num;
				float[] array3 = new float[num2];
				for (int i = 0; i < num2; i++)
				{
					int num3 = i;
					float num4 = 1f;
					for (int j = 0; j < num; j++)
					{
						num4 *= ((num3 % 2 == 0) ? (1f - array2[j]) : array2[j]);
						num3 /= 2;
					}
					array3[i] = num4;
				}
				float[] array4 = new float[num2];
				for (int k = 0; k < num2; k++)
				{
					array4[k] = argument3[array[k]];
				}
				float num5 = 0f;
				for (int l = 0; l < num2; l++)
				{
					num5 += array3[l] * array4[l];
				}
				return (int)(num5 + 0.5f);
			}
			}
		}

		internal static float _0020(ModelContext argument0, PivotManager argument1, bool[] argument2, float[] argument3)
		{
			int num = argument1._3000_0020(argument0, argument2);
			short[] array = argument0._3000();
			float[] array2 = argument0._3000_0020();
			argument1._3000(array, array2, num);
			if (num <= 0)
			{
				return argument3[array[0]];
			}
			switch (num)
			{
			case 1:
			{
				float num12 = argument3[array[0]];
				float num13 = argument3[array[1]];
				float num14 = array2[0];
				return num12 + (num13 - num12) * num14;
			}
			case 2:
			{
				float num6 = argument3[array[0]];
				float num7 = argument3[array[1]];
				float num8 = argument3[array[2]];
				float num9 = argument3[array[3]];
				float num10 = array2[0];
				float num11 = array2[1];
				return (1f - num11) * (num6 + (num7 - num6) * num10) + num11 * (num8 + (num9 - num8) * num10);
			}
			case 3:
			{
				float num35 = argument3[array[0]];
				float num36 = argument3[array[1]];
				float num37 = argument3[array[2]];
				float num38 = argument3[array[3]];
				float num39 = argument3[array[4]];
				float num40 = argument3[array[5]];
				float num41 = argument3[array[6]];
				float num42 = argument3[array[7]];
				float num43 = array2[0];
				float num44 = array2[1];
				float num45 = array2[2];
				return (1f - num45) * ((1f - num44) * (num35 + (num36 - num35) * num43) + num44 * (num37 + (num38 - num37) * num43)) + num45 * ((1f - num44) * (num39 + (num40 - num39) * num43) + num44 * (num41 + (num42 - num41) * num43));
			}
			case 4:
			{
				float num15 = argument3[array[0]];
				float num16 = argument3[array[1]];
				float num17 = argument3[array[2]];
				float num18 = argument3[array[3]];
				float num19 = argument3[array[4]];
				float num20 = argument3[array[5]];
				float num21 = argument3[array[6]];
				float num22 = argument3[array[7]];
				float num23 = argument3[array[8]];
				float num24 = argument3[array[9]];
				float num25 = argument3[array[10]];
				float num26 = argument3[array[11]];
				float num27 = argument3[array[12]];
				float num28 = argument3[array[13]];
				float num29 = argument3[array[14]];
				float num30 = argument3[array[15]];
				float num31 = array2[0];
				float num32 = array2[1];
				float num33 = array2[2];
				float num34 = array2[3];
				return (1f - num34) * ((1f - num33) * ((1f - num32) * (num15 + (num16 - num15) * num31) + num32 * (num17 + (num18 - num17) * num31)) + num33 * ((1f - num32) * (num19 + (num20 - num19) * num31) + num32 * (num21 + (num22 - num21) * num31))) + num34 * ((1f - num33) * ((1f - num32) * (num23 + (num24 - num23) * num31) + num32 * (num25 + (num26 - num25) * num31)) + num33 * ((1f - num32) * (num27 + (num28 - num27) * num31) + num32 * (num29 + (num30 - num29) * num31)));
			}
			default:
			{
				int num2 = 1 << num;
				float[] array3 = new float[num2];
				for (int i = 0; i < num2; i++)
				{
					int num3 = i;
					float num4 = 1f;
					for (int j = 0; j < num; j++)
					{
						num4 *= ((num3 % 2 == 0) ? (1f - array2[j]) : array2[j]);
						num3 /= 2;
					}
					array3[i] = num4;
				}
				float[] array4 = new float[num2];
				for (int k = 0; k < num2; k++)
				{
					array4[k] = argument3[array[k]];
				}
				float num5 = 0f;
				for (int l = 0; l < num2; l++)
				{
					num5 += array3[l] * array4[l];
				}
				return num5;
			}
			}
		}

		internal static void _3000_0020(ModelContext argument0, PivotManager argument1, bool[] argument2, int argument3, List<float[]> argument4, float[] argument5, int argument6, int argument7)
		{
			int num = argument1._3000_0020(argument0, argument2);
			short[] array = argument0._3000();
			float[] array2 = argument0._3000_0020();
			argument1._3000(array, array2, num);
			int num2 = argument3 * 2;
			int num3 = argument6;
			if (num <= 0)
			{
				int index = array[0];
				float[] array3 = argument4[index];
				if (argument7 == 2 && argument6 == 0)
				{
					Array.Copy(array3, 0, argument5, 0, num2);
					return;
				}
				int num4 = 0;
				while (num4 < num2)
				{
					argument5[num3] = array3[num4++];
					argument5[num3 + 1] = array3[num4++];
					num3 += argument7;
				}
				return;
			}
			switch (num)
			{
			case 1:
			{
				float[] array4 = argument4[array[0]];
				float[] array5 = argument4[array[1]];
				float num5 = array2[0];
				float num6 = 1f - num5;
				int num7 = 0;
				while (num7 < num2)
				{
					argument5[num3] = array4[num7] * num6 + array5[num7] * num5;
					num7++;
					argument5[num3 + 1] = array4[num7] * num6 + array5[num7] * num5;
					num7++;
					num3 += argument7;
				}
				return;
			}
			case 2:
			{
				float[] array22 = argument4[array[0]];
				float[] array23 = argument4[array[1]];
				float[] array24 = argument4[array[2]];
				float[] array25 = argument4[array[3]];
				float num33 = array2[0];
				float num34 = array2[1];
				float num35 = 1f - num33;
				float num36 = 1f - num34;
				float num37 = num36 * num35;
				float num38 = num36 * num33;
				float num39 = num34 * num35;
				float num40 = num34 * num33;
				int num41 = 0;
				while (num41 < num2)
				{
					argument5[num3] = num37 * array22[num41] + num38 * array23[num41] + num39 * array24[num41] + num40 * array25[num41];
					num41++;
					argument5[num3 + 1] = num37 * array22[num41] + num38 * array23[num41] + num39 * array24[num41] + num40 * array25[num41];
					num41++;
					num3 += argument7;
				}
				return;
			}
			case 3:
			{
				float[] array26 = argument4[array[0]];
				float[] array27 = argument4[array[1]];
				float[] array28 = argument4[array[2]];
				float[] array29 = argument4[array[3]];
				float[] array30 = argument4[array[4]];
				float[] array31 = argument4[array[5]];
				float[] array32 = argument4[array[6]];
				float[] array33 = argument4[array[7]];
				float num42 = array2[0];
				float num43 = array2[1];
				float num44 = array2[2];
				float num45 = 1f - num42;
				float num46 = 1f - num43;
				float num47 = 1f - num44;
				float num48 = num47 * num46 * num45;
				float num49 = num47 * num46 * num42;
				float num50 = num47 * num43 * num45;
				float num51 = num47 * num43 * num42;
				float num52 = num44 * num46 * num45;
				float num53 = num44 * num46 * num42;
				float num54 = num44 * num43 * num45;
				float num55 = num44 * num43 * num42;
				int num56 = 0;
				while (num56 < num2)
				{
					argument5[num3] = num48 * array26[num56] + num49 * array27[num56] + num50 * array28[num56] + num51 * array29[num56] + num52 * array30[num56] + num53 * array31[num56] + num54 * array32[num56] + num55 * array33[num56];
					num56++;
					argument5[num3 + 1] = num48 * array26[num56] + num49 * array27[num56] + num50 * array28[num56] + num51 * array29[num56] + num52 * array30[num56] + num53 * array31[num56] + num54 * array32[num56] + num55 * array33[num56];
					num56++;
					num3 += argument7;
				}
				return;
			}
			case 4:
			{
				float[] array6 = argument4[array[0]];
				float[] array7 = argument4[array[1]];
				float[] array8 = argument4[array[2]];
				float[] array9 = argument4[array[3]];
				float[] array10 = argument4[array[4]];
				float[] array11 = argument4[array[5]];
				float[] array12 = argument4[array[6]];
				float[] array13 = argument4[array[7]];
				float[] array14 = argument4[array[8]];
				float[] array15 = argument4[array[9]];
				float[] array16 = argument4[array[10]];
				float[] array17 = argument4[array[11]];
				float[] array18 = argument4[array[12]];
				float[] array19 = argument4[array[13]];
				float[] array20 = argument4[array[14]];
				float[] array21 = argument4[array[15]];
				float num8 = array2[0];
				float num9 = array2[1];
				float num10 = array2[2];
				float num11 = array2[3];
				float num12 = 1f - num8;
				float num13 = 1f - num9;
				float num14 = 1f - num10;
				float num15 = 1f - num11;
				float num16 = num15 * num14 * num13 * num12;
				float num17 = num15 * num14 * num13 * num8;
				float num18 = num15 * num14 * num9 * num12;
				float num19 = num15 * num14 * num9 * num8;
				float num20 = num15 * num10 * num13 * num12;
				float num21 = num15 * num10 * num13 * num8;
				float num22 = num15 * num10 * num9 * num12;
				float num23 = num15 * num10 * num9 * num8;
				float num24 = num11 * num14 * num13 * num12;
				float num25 = num11 * num14 * num13 * num8;
				float num26 = num11 * num14 * num9 * num12;
				float num27 = num11 * num14 * num9 * num8;
				float num28 = num11 * num10 * num13 * num12;
				float num29 = num11 * num10 * num13 * num8;
				float num30 = num11 * num10 * num9 * num12;
				float num31 = num11 * num10 * num9 * num8;
				int num32 = 0;
				while (num32 < num2)
				{
					argument5[num3] = num16 * array6[num32] + num17 * array7[num32] + num18 * array8[num32] + num19 * array9[num32] + num20 * array10[num32] + num21 * array11[num32] + num22 * array12[num32] + num23 * array13[num32] + num24 * array14[num32] + num25 * array15[num32] + num26 * array16[num32] + num27 * array17[num32] + num28 * array18[num32] + num29 * array19[num32] + num30 * array20[num32] + num31 * array21[num32];
					num32++;
					argument5[num3 + 1] = num16 * array6[num32] + num17 * array7[num32] + num18 * array8[num32] + num19 * array9[num32] + num20 * array10[num32] + num21 * array11[num32] + num22 * array12[num32] + num23 * array13[num32] + num24 * array14[num32] + num25 * array15[num32] + num26 * array16[num32] + num27 * array17[num32] + num28 * array18[num32] + num29 * array19[num32] + num30 * array20[num32] + num31 * array21[num32];
					num32++;
					num3 += argument7;
				}
				return;
			}
			}
			int num57 = 1 << num;
			float[] array34 = new float[num57];
			for (int i = 0; i < num57; i++)
			{
				int num58 = i;
				float num59 = 1f;
				for (int j = 0; j < num; j++)
				{
					num59 *= ((num58 % 2 == 0) ? (1f - array2[j]) : array2[j]);
					num58 /= 2;
				}
				array34[i] = num59;
			}
			float[][] array35 = new float[num57][];
			for (int k = 0; k < num57; k++)
			{
				array35[k] = argument4[array[k]];
			}
			int num60 = 0;
			while (num60 < num2)
			{
				float num61 = 0f;
				float num62 = 0f;
				int num63 = num60 + 1;
				for (int l = 0; l < num57; l++)
				{
					num61 += array34[l] * array35[l][num60];
					num62 += array34[l] * array35[l][num63];
				}
				num60 += 2;
				argument5[num3] = num61;
				argument5[num3 + 1] = num62;
				num3 += argument7;
			}
		}
	}
	internal class CubismType_34
	{
		private const int CubismField_208 = -1;

		private const int CubismField_209 = 0;

		private const int CubismField_210 = 1;

		private const int CubismField_211 = 0;

		private const int CubismField_212 = 1;

		private const int CubismField_213 = 2;

		private const int CubismField_214 = 4;

		private float CubismField_215 = 1f;

		private float CubismField_216;

		private float CubismField_217;

		private float CubismField_218 = 1f;

		private float CubismField_219;

		private float CubismField_220;

		private int CubismField_221;

		private int CubismField_222;

		internal CubismType_34()
		{
		}

		internal CubismType_34(float argument0, float argument1, float argument2, float argument3, float argument4, float argument5)
		{
			CubismField_215 = argument0;
			CubismField_216 = argument1;
			CubismField_217 = argument2;
			CubismField_218 = argument3;
			CubismField_219 = argument4;
			CubismField_220 = argument5;
			_3000_0020_3000();
		}

		internal void _3000_0020_0020(float[] argument0, float[] argument1, int argument2)
		{
			int num = 0;
			int num2 = 0;
			switch (CubismField_221)
			{
			case 7:
			{
				float cubismField_3 = CubismField_215;
				float cubismField_ = CubismField_217;
				float cubismField_4 = CubismField_219;
				float cubismField_2 = CubismField_216;
				float cubismField_5 = CubismField_218;
				float cubismField_6 = CubismField_220;
				while (--argument2 >= 0)
				{
					float num4 = argument0[num++];
					float num5 = argument0[num++];
					argument1[num2++] = cubismField_3 * num4 + cubismField_ * num5 + cubismField_4;
					argument1[num2++] = cubismField_2 * num4 + cubismField_5 * num5 + cubismField_6;
				}
				break;
			}
			case 6:
			{
				float cubismField_3 = CubismField_215;
				float cubismField_ = CubismField_217;
				float cubismField_2 = CubismField_216;
				float cubismField_5 = CubismField_218;
				while (--argument2 >= 0)
				{
					float num6 = argument0[num++];
					float num7 = argument0[num++];
					argument1[num2++] = cubismField_3 * num6 + cubismField_ * num7;
					argument1[num2++] = cubismField_2 * num6 + cubismField_5 * num7;
				}
				break;
			}
			case 5:
			{
				float cubismField_ = CubismField_217;
				float cubismField_4 = CubismField_219;
				float cubismField_2 = CubismField_216;
				float cubismField_6 = CubismField_220;
				while (--argument2 >= 0)
				{
					float num8 = argument0[num++];
					argument1[num2++] = cubismField_ * argument0[num++] + cubismField_4;
					argument1[num2++] = cubismField_2 * num8 + cubismField_6;
				}
				break;
			}
			case 4:
			{
				float cubismField_ = CubismField_217;
				float cubismField_2 = CubismField_216;
				while (--argument2 >= 0)
				{
					float num3 = argument0[num++];
					argument1[num2++] = cubismField_ * argument0[num++];
					argument1[num2++] = cubismField_2 * num3;
				}
				break;
			}
			case 3:
			{
				float cubismField_3 = CubismField_215;
				float cubismField_4 = CubismField_219;
				float cubismField_5 = CubismField_218;
				float cubismField_6 = CubismField_220;
				while (--argument2 >= 0)
				{
					argument1[num2++] = cubismField_3 * argument0[num++] + cubismField_4;
					argument1[num2++] = cubismField_5 * argument0[num++] + cubismField_6;
				}
				break;
			}
			case 2:
			{
				float cubismField_3 = CubismField_215;
				float cubismField_5 = CubismField_218;
				while (--argument2 >= 0)
				{
					argument1[num2++] = cubismField_3 * argument0[num++];
					argument1[num2++] = cubismField_5 * argument0[num++];
				}
				break;
			}
			case 1:
			{
				float cubismField_4 = CubismField_219;
				float cubismField_6 = CubismField_220;
				while (--argument2 >= 0)
				{
					argument1[num2++] = argument0[num++] + cubismField_4;
					argument1[num2++] = argument0[num++] + cubismField_6;
				}
				break;
			}
			case 0:
				if (argument0 != argument1 || num != num2)
				{
					Array.Copy(argument0, num, argument1, num2, argument2 * 2);
				}
				break;
			}
		}

		internal void _3000_0020_3000()
		{
			if (CubismField_217 == 0f && CubismField_216 == 0f)
			{
				if (CubismField_215 == 1f && CubismField_218 == 1f)
				{
					if (CubismField_219 == 0f && CubismField_220 == 0f)
					{
						CubismField_221 = 0;
						CubismField_222 = 0;
					}
					else
					{
						CubismField_221 = 1;
						CubismField_222 = 1;
					}
				}
				else if (CubismField_219 == 0f && CubismField_220 == 0f)
				{
					CubismField_221 = 2;
					CubismField_222 = -1;
				}
				else
				{
					CubismField_221 = 3;
					CubismField_222 = -1;
				}
			}
			else if (CubismField_215 == 0f && CubismField_218 == 0f)
			{
				if (CubismField_219 == 0f && CubismField_220 == 0f)
				{
					CubismField_221 = 4;
					CubismField_222 = -1;
				}
				else
				{
					CubismField_221 = 5;
					CubismField_222 = -1;
				}
			}
			else if (CubismField_219 == 0f && CubismField_220 == 0f)
			{
				CubismField_221 = 6;
				CubismField_222 = -1;
			}
			else
			{
				CubismField_221 = 7;
				CubismField_222 = -1;
			}
		}

		internal void _0020(float[] argument0)
		{
			_3000(argument0);
			float num = argument0[0];
			float num2 = argument0[2];
			float num3 = argument0[1];
			float num4 = argument0[3];
			float num5 = (float)Math.Sqrt(num * num + num3 * num3);
			float num6 = num * num4 - num2 * num3;
			if (num5 != 0f)
			{
				argument0[0] = num5;
				argument0[1] = num6 / num5;
				argument0[2] = (num3 * num4 + num * num2) / num6;
				argument0[3] = (float)Math.Atan2(num3, num);
			}
		}

		internal void _3000_0020(CubismType_34 argument0, CubismType_34 argument1, float argument2, CubismType_34 argument3)
		{
			float[] array = new float[6];
			float[] array2 = new float[6];
			argument0._0020(array);
			argument1._0020(array2);
			argument3._3000_3000(new float[6]
			{
				array[0] + (array2[0] - array[0]) * argument2,
				array[1] + (array2[1] - array[1]) * argument2,
				array[2] + (array2[2] - array[2]) * argument2,
				array[3] + (array2[3] - array[3]) * argument2,
				array[4] + (array2[4] - array[4]) * argument2,
				array[5] + (array2[5] - array[5]) * argument2
			});
		}

		internal void _3000_3000(float[] argument0)
		{
			float num = (float)Math.Cos(argument0[3]);
			float num2 = (float)Math.Sin(argument0[3]);
			CubismField_215 = argument0[0] * num;
			CubismField_216 = argument0[0] * num2;
			CubismField_217 = argument0[1] * (argument0[2] * num - num2);
			CubismField_218 = argument0[1] * (argument0[2] * num2 + num);
			CubismField_219 = argument0[4];
			CubismField_220 = argument0[5];
			_3000_0020_3000();
		}

		internal void _3000(float[] argument0)
		{
			argument0[0] = CubismField_215;
			argument0[1] = CubismField_216;
			argument0[2] = CubismField_217;
			argument0[3] = CubismField_218;
			argument0[4] = CubismField_219;
			argument0[5] = CubismField_220;
		}
	}
	public class IContextData
	{
	}
	internal class CubismType_36
	{
		internal const bool CubismField_223 = false;

		internal const bool CubismField_224 = false;

		internal const bool CubismField_225 = false;

		internal const bool CubismField_226 = false;

		internal const bool CubismField_227 = false;

		internal const bool CubismField_228 = false;

		internal const bool CubismField_229 = false;

		internal const bool CubismField_230 = false;

		internal const bool CubismField_231 = false;

		internal const bool CubismField_232 = false;

		internal const bool CubismField_233 = false;

		internal const bool CubismField_234 = false;

		internal const bool CubismField_235 = false;

		internal const bool CubismField_236 = true;
	}
	public class Def
	{
		public const int VERTEX_TYPE_OFFSET0_STEP2 = 1;

		public const int VERTEX_TYPE_OFFSET2_STEP5 = 2;

		public const int VERTEX_OFFSET = 0;

		public const int VERTEX_STEP = 2;

		public const int VERTEX_TYPE = 1;

		public const int MAX_INTERPOLATION = 5;

		public const int PIVOT_TABLE_SIZE = 65;

		public const float GOSA = 0.0001f;

		public const float VALUE_MIN = 0.001f;

		public const int VALUE_MIN_PRECISION = 3;
	}
	public class Live2D
	{
		public enum DrawMethodVersion
		{
			FORCE_2_0,
			FORCE_2_1,
			DEFAULT_2_0,
			DEFAULT_2_1
		}

		public const string __L2D_VERSION_STR__ = "2.1.03";

		public const int __L2D_VERSION_NO__ = 201030000;

		public static readonly string __L2D_PLATFORM_STR__;

		internal const bool CubismField_250 = false;

		internal const bool CubismField_251 = false;

		internal const bool CubismField_252 = true;

		internal const bool CubismField_253 = true;

		internal const bool CubismField_254 = true;

		internal const bool CubismField_255 = false;

		internal const bool CubismField_256 = false;

		internal static bool CubismField_257;

		public const int L2D_RENDER_DRAW_MESH_NOW = 0;

		public const int L2D_RENDER_DRAW_MESH = 1;

		internal static DrawMethodVersion CubismField_260;

		internal static int CubismField_261;

		public const int L2D_NO_ERROR = 0;

		public const int L2D_ERROR_LIVE2D_INIT_FAILED = 1000;

		public const int L2D_ERROR_FILE_LOAD_FAILED = 1001;

		public const int L2D_ERROR_MEMORY_ERROR = 1100;

		public const int L2D_ERROR_MODEL_DATA_VERSION_MISMATCH = 2000;

		public const int L2D_ERROR_MODEL_DATA_EOF_ERROR = 2001;

		public const int L2D_ERROR_MODEL_DATA_UNKNOWN_FORMAT = 2002;

		public const int L2D_ERROR_DDTEXTURE_SETUP_TRANSFORM_FAILED = 4000;

		private static int CubismField_270;

		private static bool CubismField_271;

		public static bool TEST_____ABC;

		static Live2D()
		{
			CubismField_257 = false;
			CubismField_260 = DrawMethodVersion.DEFAULT_2_1;
			CubismField_261 = 256;
			CubismField_271 = true;
			__L2D_PLATFORM_STR__ = "Unity";
		}

		public static void init()
		{
			if (CubismField_271)
			{
				CubismField_271 = false;
				string text = "";
				UtDebug.println("Live2D version {0} for {1}\n{2}", "2.1.03", __L2D_PLATFORM_STR__, text);
			}
		}

		public static void dispose()
		{
			ID.releaseStored_notForClientCall();
			CubismType_59._0020();
			CubismType_43._3000_0020_3000();
		}

		public static string getVersionStr()
		{
			return "2.1.03";
		}

		public static int getVersionNo()
		{
			return 201030000;
		}

		internal static void _0020(int argument0)
		{
			CubismField_270 = argument0;
		}

		public static int getError()
		{
			int cubismField_ = CubismField_270;
			CubismField_270 = 0;
			return cubismField_;
		}

		public static void setDrawMethodVersion(DrawMethodVersion argument0)
		{
			CubismField_260 = argument0;
		}

		public static DrawMethodVersion getDrawMethodVersion()
		{
			return CubismField_260;
		}

		public static void setClippingMaskBufferSize(int argument0)
		{
			CubismField_261 = argument0;
		}

		public static int getClippingMaskBufferSize()
		{
			return CubismField_261;
		}
	}
	internal class CubismType_40 : Exception
	{
		internal const long CubismField_280 = -1L;

		internal string CubismField_281;

		internal string CubismField_282;

		internal CubismType_40()
		{
		}

		internal CubismType_40(Exception argument0)
		{
		}

		internal CubismType_40(Exception argument0, string argument1, string argument2)
		{
			CubismField_281 = argument1;
			CubismField_282 = argument2;
		}

		internal string _0020()
		{
			return base.Message + string.Format(" id[%s] %s", CubismField_281, CubismField_282);
		}
	}
	internal abstract class CubismType_42 : DrawParam
	{
		internal const int CubismField_289 = 9999;

		protected Dictionary<string, CubismType_56> CubismField_290;

		protected List<CubismType_57> CubismField_291;

		protected DrawDataID CubismField_292;

		protected int CubismField_293;

		protected List<Texture2D> CubismField_294;

		protected Dictionary<int, bool> CubismField_295;

		protected Matrix4x4 CubismField_296;

		protected CubismType_43.CubismType_44 CubismField_297;

		protected CubismType_43.CubismType_44 CubismField_298;

		public CubismType_42()
		{
			CubismField_290 = new Dictionary<string, CubismType_56>();
			CubismField_291 = new List<CubismType_57>();
			CubismField_294 = new List<Texture2D>();
			CubismField_295 = new Dictionary<int, bool>();
			CubismField_296 = default(Matrix4x4);
			CubismField_297 = null;
			CubismField_298 = null;
		}

		public new virtual void _3000_3000_3000()
		{
			base.releaseDrawParam_testImpl();
			foreach (CubismType_56 value in CubismField_290.Values)
			{
				value._0020();
			}
			foreach (CubismType_57 item in CubismField_291)
			{
				item._3000_0020();
			}
		}

		internal void _3000_0020_3000_3000(CubismType_43.CubismType_44 argument0)
		{
			CubismField_297 = argument0;
		}

		internal void _3000_0020_3000_0020(CubismType_43.CubismType_44 argument0)
		{
			CubismField_298 = argument0;
		}

		public void _3000_3000_0020_3000(Matrix4x4 argument0)
		{
			for (int i = 0; i < 16; i++)
			{
				CubismField_69[i] = argument0[i];
			}
		}

		public override void setupDraw()
		{
			int count = CubismField_291.Count;
			for (int i = 0; i < count; i++)
			{
				CubismField_291[i].commands.Clear();
			}
			CubismField_74 = 0;
			CubismField_293 = 0;
			CubismField_292 = null;
			for (int j = 0; j < 16; j++)
			{
				CubismField_296[j] = CubismField_69[j];
			}
		}

		public List<Texture2D> _3000_0020_3000()
		{
			return CubismField_294;
		}

		protected bool _3000_0020(int argument0, int argument1, ushort[] argument2, float[] argument3, float[] argument4, float argument5, int argument6)
		{
			if ((double)argument5 < 0.01 && CubismField_297 == null && clippingProcess == 0)
			{
				return false;
			}
			if (CubismField_294.Count <= argument0)
			{
				return false;
			}
			return true;
		}

		public void _3000_3000_3000_0020(int argument0, Texture2D argument1)
		{
			if (CubismField_294.Count < argument0 + 1)
			{
				for (int i = CubismField_294.Count; i < argument0 + 1; i++)
				{
					CubismField_294.Add(null);
				}
			}
			CubismField_294[argument0] = argument1;
			createTextureInfo(argument0);
		}

		public override void setCurrentDDID(DrawDataID argument0)
		{
			CubismField_292 = argument0;
		}

		public override int generateModelTextureNo()
		{
			int num = -1;
			int num2 = 0;
			while (true)
			{
				if (9999 < num2)
				{
					UtDebug.println("error generate tex no");
					return -1;
				}
				bool flag = false;
				if (CubismField_294.Count > num2 && CubismField_294[num2] != null)
				{
					flag = true;
				}
				else if (CubismField_295.ContainsKey(num2) && CubismField_295[num2])
				{
					flag = true;
				}
				if (!flag)
				{
					break;
				}
				num2++;
			}
			num = num2;
			CubismField_295[num] = true;
			return num;
		}

		public override void releaseModelTextureNo(int argument0)
		{
			_3000_0020_0020_0020(argument0);
			if (CubismField_295.ContainsKey(argument0))
			{
				CubismField_295[argument0] = false;
			}
		}

		internal void _3000_0020_0020_0020(int argument0)
		{
			if (CubismField_294.Count > argument0)
			{
				CubismField_294[argument0] = null;
			}
		}

		internal void _3000_0020_0020_3000()
		{
			for (int i = 0; i < CubismField_294.Count; i++)
			{
				_3000_0020_0020_0020(i);
			}
		}

		public void _3000_3000_0020_0020(int argument0, Texture2D argument1)
		{
			ColorConverterUnity obj = _3000(argument0) as ColorConverterUnity;
			obj.data.methodType = ColorConvertMethod.COLOR_LIST;
			obj.setTexture(argument1);
		}

		public void _3000_3000_0020(int argument0, int argument1, ushort[] argument2, float[] argument3, float[] argument4, float argument5, int argument6)
		{
		}

		public string _3000_3000_3000_3000()
		{
			string text = "DrawParam";
			for (int i = 0; i < CubismField_291.Count; i++)
			{
				text = text + "\n\t" + CubismField_291[i]._3000_3000(1);
			}
			return text;
		}

		protected CubismType_57 _3000_3000(int argument0, int argument1)
		{
			if (CubismField_291.Count <= CubismField_293)
			{
				CubismField_291.Add(new CubismType_57());
			}
			else if (!CubismField_291[CubismField_293]._0020(argument0, CubismField_67, argument1, CubismField_73, clippingProcess, CubismField_297, CubismField_298))
			{
				CubismField_293++;
				if (CubismField_291.Count <= CubismField_293)
				{
					CubismField_291.Add(new CubismType_57());
				}
			}
			CubismType_57 cubismType_ = CubismField_291[CubismField_293];
			cubismType_.textureNo = argument0;
			cubismType_.colorGroupNo = CubismField_73;
			cubismType_.colorType = argument1;
			cubismType_.culling = CubismField_67;
			cubismType_.clippingProcess = clippingProcess;
			cubismType_.clipBufPre_clipContextMask = CubismField_297;
			cubismType_.clipBufPre_clipContextDraw = CubismField_298;
			return cubismType_;
		}

		protected CubismType_56 _3000_0020_0020(string argument0)
		{
			CubismType_56 cubismType_;
			if (!CubismField_290.ContainsKey(argument0))
			{
				cubismType_ = new CubismType_56();
				CubismField_290.Add(argument0, cubismType_);
			}
			else
			{
				cubismType_ = CubismField_290[argument0];
			}
			return cubismType_;
		}

		public override void drawTexture(int argument0, int argument1, ushort[] argument2, float[] argument3, float[] argument4, float argument5, int argument6)
		{
			if (_3000_0020(argument0, argument1, argument2, argument3, argument4, argument5, argument6))
			{
				string argument7 = CubismField_292.ToString();
				Color color = new Color(CubismField_62, CubismField_64, CubismField_65, CubismField_62);
				CubismType_56 cubismType_ = _3000_0020_0020(argument7);
				if (cubismType_.combine.mesh == null)
				{
					cubismType_._3000(_3000(argument3, argument4, argument2, argument5, color));
				}
				else
				{
					_3000_0020_0020_0020_0020(cubismType_, argument3, argument4, argument2, argument5, color, CubismField_71[argument0]);
				}
				_3000_3000(argument0, argument6).commands.Add(cubismType_);
				CubismField_74++;
			}
		}

		private void _3000_0020_0020_0020_0020(CubismType_56 argument0, float[] argument1, float[] argument2, ushort[] argument3, float argument4, Color argument5, TextureInfo argument6)
		{
			Mesh mesh = argument0.combine.mesh;
			Color color = argument0.color;
			Vector3[] vertices = argument0.vertices;
			int num = vertices.Length;
			for (int i = 0; i < num; i++)
			{
				vertices[i].x = argument1[i * 2];
				vertices[i].y = argument1[i * 2 + 1];
			}
			_3000_0020_0020_0020_3000(vertices);
			mesh.vertices = vertices;
			if (CubismField_66 || color.a != CubismField_62 * argument4 || color.r != CubismField_63 || color.g != CubismField_64 || color.b != CubismField_65)
			{
				Color[] colors = argument0.colors;
				int num2 = colors.Length;
				float r = argument6.r;
				float g = argument6.g;
				float b = argument6.b;
				color.a = CubismField_62 * argument4;
				color.r = CubismField_63 * r;
				color.g = CubismField_64 * g;
				color.b = CubismField_65 * b;
				for (int j = 0; j < num2; j++)
				{
					colors[j] = color;
				}
				mesh.colors = colors;
				CubismField_66 = false;
			}
		}

		public Mesh _3000(float[] argument0, float[] argument1, ushort[] argument2, float argument3, Color argument4)
		{
			Mesh mesh = new Mesh();
			mesh.MarkDynamic();
			mesh.Optimize();
			Vector3[] array = new Vector3[argument0.Length / 2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i].x = argument0[i * 2];
				array[i].y = argument0[i * 2 + 1];
			}
			_3000_0020_0020_0020_3000(array);
			Vector2[] array2 = new Vector2[argument0.Length / 2];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].x = argument1[j * 2];
				array2[j].y = 1f - argument1[j * 2 + 1];
			}
			int[] array3 = new int[argument2.Length];
			for (int k = 0; k < argument2.Length; k++)
			{
				array3[k] = argument2[k];
			}
			Color color = new Color(argument4.r, argument4.g, argument4.b, argument4.a * argument3);
			Color[] array4 = new Color[argument0.Length / 2];
			for (int l = 0; l < array4.Length; l++)
			{
				array4[l] = color;
			}
			mesh.vertices = array;
			mesh.triangles = array3;
			mesh.uv = array2;
			mesh.colors = array4;
			return mesh;
		}

		protected abstract void _3000_0020_0020_0020_3000(Vector3[] argument0);

		public abstract override void drawLast();

		internal override void _0020(string argument0)
		{
			if (CubismField_290.ContainsKey(argument0))
			{
				CubismField_290[argument0]._0020();
				CubismField_290.Remove(argument0);
			}
		}
	}
	internal class CubismType_43
	{
		internal class CubismType_44
		{
			private CubismType_43 CubismField_319;

			internal DrawDataID[] CubismField_320;

			internal int[] CubismField_321;

			internal List<CubismType_46> CubismField_322 = new List<CubismType_46>();

			internal bool CubismField_323;

			internal int CubismField_324;

			internal LDRectF CubismField_325;

			internal LDRectF CubismField_326;

			internal Matrix4x4 CubismField_327;

			internal Matrix4x4 CubismField_328;

			public CubismType_44(CubismType_43 argument0, ModelContext argument1, DrawDataID[] argument2)
			{
				CubismField_319 = argument0;
				CubismField_320 = argument2;
				CubismField_321 = new int[argument2.Length];
				for (int i = 0; i < argument2.Length; i++)
				{
					CubismField_321[i] = argument1.getDrawDataIndex(argument2[i]);
				}
			}

			public void _0020(DrawDataID argument0, int argument1)
			{
				CubismType_46 item = new CubismType_46(argument0, argument1);
				CubismField_322.Add(item);
			}

			public CubismType_43 _3000()
			{
				return CubismField_319;
			}
		}

		internal class CubismType_45
		{
			internal RenderTexture CubismField_329;

			internal int CubismField_330 = -100;

			internal CubismType_45(int argument0, RenderTexture argument1)
			{
				CubismField_329 = argument1;
				CubismField_330 = argument0;
			}
		}

		internal class CubismType_46
		{
			public DrawDataID drawDataID;

			public int drawDataIndex;

			public LDRectF rect;

			internal CubismType_46(DrawDataID argument0, int argument1)
			{
				drawDataID = argument0;
				drawDataIndex = argument1;
			}
		}

		internal const int CubismField_299 = 4;

		internal const bool CubismField_300 = false;

		internal const FilterMode CubismField_301 = FilterMode.Bilinear;

		internal RenderTexture CubismField_302;

		private static CubismType_48 CubismField_303 = new CubismType_48();

		private static CubismType_48 CubismField_304 = new CubismType_48();

		private static CubismType_48 CubismField_305 = new CubismType_48();

		private static CubismType_48 CubismField_306 = new CubismType_48();

		private static LDPointF CubismField_307 = default(LDPointF);

		private static LDRectF CubismField_308 = default(LDRectF);

		private static LDRectF CubismField_309 = default(LDRectF);

		internal List<CubismType_44> CubismField_310 = new List<CubismType_44>();

		private CubismType_50 CubismField_311 = new CubismType_50(argument0: true);

		private static List<CubismType_45> CubismField_312 = new List<CubismType_45>();

		private static int CubismField_313 = -1;

		private static int CubismField_314 = 0;

		private const int CubismField_315 = -100;

		private static bool CubismField_316 = true;

		private static bool CubismField_317 = true;

		private static Color[] CubismField_318 = new Color[4]
		{
			new Color(1f, 0f, 0f, 0f),
			new Color(0f, 1f, 0f, 0f),
			new Color(0f, 0f, 1f, 0f),
			new Color(0f, 0f, 0f, 1f)
		};

		internal CubismType_43()
		{
		}

		internal void _3000_3000_0020()
		{
			CubismField_311._3000_3000_3000();
		}

		internal void _3000_0020_0020(ModelContext argument0, List<IDrawData> argument1, List<IDrawContext> argument2)
		{
			bool flag = false;
			for (int i = 0; i < argument1.Count; i++)
			{
				DrawDataID[] array = ((CubismType_19)argument1[i])._3000();
				if (array != null)
				{
					CubismType_44 cubismType_ = _3000(array);
					if (cubismType_ == null)
					{
						cubismType_ = new CubismType_44(this, argument0, array);
						CubismField_310.Add(cubismType_);
					}
					DrawDataID drawDataID = argument1[i].getDrawDataID();
					int drawDataIndex = argument0.getDrawDataIndex(drawDataID);
					cubismType_._0020(drawDataID, drawDataIndex);
					argument2[i].CubismField_100 = cubismType_;
					flag = true;
				}
			}
			if (CubismField_317 && flag)
			{
				CubismField_317 = false;
				if (!CubismType_51._3000())
				{
					UnityEngine.Debug.LogError("Clipping mask is not supported with Unity4 Free.\nPlease use Unity4 Pro or Unity5.");
				}
			}
		}

		private CubismType_44 _3000(DrawDataID[] argument0)
		{
			for (int i = 0; i < CubismField_310.Count; i++)
			{
				CubismType_44 cubismType_ = CubismField_310[i];
				int num = cubismType_.CubismField_320.Length;
				if (num != argument0.Length)
				{
					continue;
				}
				int num2 = 0;
				for (int j = 0; j < num; j++)
				{
					DrawDataID drawDataID = cubismType_.CubismField_320[j];
					for (int k = 0; k < num; k++)
					{
						if (argument0[k] == drawDataID)
						{
							num2++;
							break;
						}
					}
				}
				if (num2 == num)
				{
					return cubismType_;
				}
			}
			return null;
		}

		public static RenderTexture _3000_3000()
		{
			int frameCount = Time.frameCount;
			if (frameCount != CubismField_313)
			{
				CubismField_313 = frameCount;
				_3000_3000_3000();
			}
			RenderTexture renderTexture = null;
			foreach (CubismType_45 item in CubismField_312)
			{
				if (item.CubismField_329 != null && item.CubismField_330 == frameCount - 1)
				{
					item.CubismField_330 = frameCount;
					renderTexture = item.CubismField_329;
					break;
				}
			}
			if (renderTexture == null)
			{
				int clippingMaskBufferSize = Live2D.getClippingMaskBufferSize();
				renderTexture = RenderTexture.GetTemporary(clippingMaskBufferSize, clippingMaskBufferSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
				CubismField_312.Add(new CubismType_45(frameCount, renderTexture));
				renderTexture.filterMode = FilterMode.Bilinear;
				CubismField_314++;
			}
			return renderTexture;
		}

		public static void _3000_3000_3000()
		{
			int frameCount = Time.frameCount;
			foreach (CubismType_45 item in CubismField_312)
			{
				if (item.CubismField_330 == frameCount - 2)
				{
					RenderTexture.ReleaseTemporary(item.CubismField_329);
					item.CubismField_330 = -100;
					item.CubismField_329 = null;
					CubismField_314--;
				}
			}
		}

		public static void _3000_0020_3000()
		{
			foreach (CubismType_45 item in CubismField_312)
			{
				if (item.CubismField_329 != null)
				{
					RenderTexture.ReleaseTemporary(item.CubismField_329);
					item.CubismField_330 = -100;
					item.CubismField_329 = null;
					CubismField_314--;
				}
			}
			CubismField_312.Clear();
		}

		internal void _3000_0020_0020_0020(ModelContext argument0, CubismType_42 argument1)
		{
			int num = 0;
			for (int i = 0; i < CubismField_310.Count; i++)
			{
				CubismType_44 cubismType_ = CubismField_310[i];
				_0020(argument0, cubismType_);
				if (cubismType_.CubismField_323)
				{
					num++;
				}
			}
			if (num <= 0)
			{
				return;
			}
			RenderTexture active = RenderTexture.active;
			CubismField_302 = _3000_3000();
			_ = CubismField_302.width;
			_ = CubismField_302.height;
			if (active != null)
			{
				_ = active.width;
				_ = active.height;
			}
			else
			{
				_ = Screen.width;
				_ = Screen.height;
			}
			float[] matrix = argument1.getMatrix();
			CubismField_311.setupDraw();
			CubismField_311.setMatrix(matrix);
			List<Texture2D> list = argument1._3000_0020_3000();
			for (int j = 0; j < list.Count; j++)
			{
				CubismField_311._3000_3000_3000_0020(j, list[j]);
			}
			Matrix4x4 matrix4x = default(Matrix4x4);
			for (int k = 0; k < 16; k++)
			{
				matrix4x[k] = matrix[k];
			}
			_3000_0020_0020_3000(num);
			CubismField_302.DiscardContents(discardColor: true, discardDepth: true);
			RenderTexture.active = CubismField_302;
			GL.Clear(clearDepth: false, clearColor: true, Color.white);
			for (int l = 0; l < CubismField_310.Count; l++)
			{
				CubismType_44 cubismType_2 = CubismField_310[l];
				LDRectF cubismField_ = cubismType_2.CubismField_326;
				_ = cubismType_2.CubismField_324;
				LDRectF cubismField_2 = cubismType_2.CubismField_325;
				float num2 = 0.05f;
				CubismField_309.setRect(cubismField_);
				CubismField_309.expand(cubismField_.width * num2, cubismField_.height * num2);
				float argument2 = cubismField_2.width / CubismField_309.width;
				float argument3 = cubismField_2.height / CubismField_309.height;
				CubismField_304._3000_0020();
				CubismField_304._3000_3000_0020_0020(-1f, -1f, 0f);
				CubismField_304._3000_0020_0020_3000(2f, 2f, 1f);
				CubismField_304._3000_3000_0020_0020(cubismField_2.x, cubismField_2.y, 0f);
				CubismField_304._3000_0020_0020_3000(argument2, argument3, 1f);
				CubismField_304._3000_3000_0020_0020(0f - CubismField_309.x, 0f - CubismField_309.y, 0f);
				CubismField_305._3000_0020_3000_3000(CubismField_304);
				CubismField_304._3000_0020();
				CubismField_304._3000_3000_0020_0020(cubismField_2.x, cubismField_2.y, 0f);
				CubismField_304._3000_0020_0020_3000(argument2, argument3, 1f);
				CubismField_304._3000_3000_0020_0020(0f - CubismField_309.x, 0f - CubismField_309.y, 0f);
				CubismField_306._3000_0020_3000_3000(CubismField_304);
				float[] array = CubismField_305._0020();
				for (int m = 0; m < 16; m++)
				{
					cubismType_2.CubismField_327[m] = array[m];
				}
				float[] array2 = CubismField_306._0020();
				for (int n = 0; n < 16; n++)
				{
					cubismType_2.CubismField_328[n] = array2[n];
				}
				int num3 = cubismType_2.CubismField_321.Length;
				for (int num4 = 0; num4 < num3; num4++)
				{
					int argument4 = cubismType_2.CubismField_321[num4];
					IDrawData drawData = argument0.getDrawData(argument4);
					IDrawContext drawContext = argument0.getDrawContext(argument4);
					CubismField_311._3000_0020_3000_3000(cubismType_2);
					drawData.draw(CubismField_311, argument0, drawContext);
				}
			}
			CubismField_311.drawLast();
			RenderTexture.active = active;
			CubismField_311._3000_0020_3000_3000(null);
		}

		private static void _0020(ModelContext argument0, CubismType_44 argument1)
		{
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			float num3 = float.MinValue;
			float num4 = float.MinValue;
			int count = argument1.CubismField_322.Count;
			for (int i = 0; i < count; i++)
			{
				int drawDataIndex = argument1.CubismField_322[i].drawDataIndex;
				CubismType_15.CubismType_16 cubismType_ = argument0.getDrawContext(drawDataIndex) as CubismType_15.CubismType_16;
				if (!cubismType_.isAvailable())
				{
					continue;
				}
				float[] array = cubismType_._0020();
				int num5 = array.Length;
				float num6 = float.MaxValue;
				float num7 = float.MaxValue;
				float num8 = float.MinValue;
				float num9 = float.MinValue;
				for (int j = 0; j < num5; j += 2)
				{
					float num10 = array[j];
					float num11 = array[j + 1];
					if (num10 < num6)
					{
						num6 = num10;
					}
					if (num10 > num8)
					{
						num8 = num10;
					}
					if (num11 < num7)
					{
						num7 = num11;
					}
					if (num11 > num9)
					{
						num9 = num11;
					}
				}
				if (num6 != float.MaxValue)
				{
					if (num6 < num)
					{
						num = num6;
					}
					if (num7 < num2)
					{
						num2 = num7;
					}
					if (num8 > num3)
					{
						num3 = num8;
					}
					if (num9 > num4)
					{
						num4 = num9;
					}
				}
			}
			if (num == float.MaxValue)
			{
				argument1.CubismField_326.setRect(0f, 0f, 0f, 0f);
				argument1.CubismField_323 = false;
				return;
			}
			argument1.CubismField_323 = true;
			float argument2 = num3 - num;
			float argument3 = num4 - num2;
			argument1.CubismField_326.setRect(num, num2, argument2, argument3);
		}

		private void _3000_0020_0020_3000(int argument0)
		{
			int num = argument0 / 4;
			int num2 = argument0 % 4;
			int num3 = 0;
			for (int i = 0; i < 4; i++)
			{
				int num4 = num + ((i < num2) ? 1 : 0);
				if (num4 == 0)
				{
					continue;
				}
				if (num4 == 1)
				{
					CubismType_44 cubismType_ = CubismField_310[num3++];
					cubismType_.CubismField_324 = i;
					cubismType_.CubismField_325.setRect(0f, 0f, 1f, 1f);
				}
				else if (num4 == 2)
				{
					for (int j = 0; j < num4; j++)
					{
						int num5 = j % 2;
						CubismType_44 cubismType_2 = CubismField_310[num3++];
						cubismType_2.CubismField_324 = i;
						cubismType_2.CubismField_325.setRect((float)num5 * 0.5f, 0f, 0.5f, 1f);
					}
				}
				else if (num4 <= 4)
				{
					for (int k = 0; k < num4; k++)
					{
						int num6 = k % 2;
						int num7 = k / 2;
						CubismType_44 cubismType_3 = CubismField_310[num3++];
						cubismType_3.CubismField_324 = i;
						cubismType_3.CubismField_325.setRect((float)num6 * 0.5f, (float)num7 * 0.5f, 0.5f, 0.5f);
					}
				}
				else if (num4 <= 9)
				{
					for (int l = 0; l < num4; l++)
					{
						int num8 = l % 3;
						int num9 = l / 3;
						CubismType_44 cubismType_4 = CubismField_310[num3++];
						cubismType_4.CubismField_324 = i;
						cubismType_4.CubismField_325.setRect((float)num8 / 3f, (float)num9 / 3f, 1f / 3f, 1f / 3f);
					}
				}
				else
				{
					UtDebug.error("not supported mask count : " + num4);
				}
			}
		}

		internal static Color _3000_0020(int argument0)
		{
			return CubismField_318[argument0];
		}
	}
	public struct LDRectF
	{
		public float x;

		public float y;

		public float width;

		public float height;

		public LDRectF(float argument0, float argument1, float argument2, float argument3)
		{
			x = argument0;
			y = argument1;
			width = argument2;
			height = argument3;
		}

		public float getCenterX()
		{
			return x + 0.5f * width;
		}

		public float getCenterY()
		{
			return y + 0.5f * height;
		}

		public float getRight()
		{
			return x + width;
		}

		public float getBottom()
		{
			return y + height;
		}

		public void setRect(float argument0, float argument1, float argument2, float argument3)
		{
			x = argument0;
			y = argument1;
			width = argument2;
			height = argument3;
		}

		public void setRect(LDRectF argument0)
		{
			x = argument0.x;
			y = argument0.y;
			width = argument0.width;
			height = argument0.height;
		}

		public bool contains(float argument0, float argument1)
		{
			if (x <= argument0 && y <= argument1 && argument0 <= x + width)
			{
				return argument1 <= y + height;
			}
			return false;
		}

		public void add(float argument0, float argument1)
		{
			if (x < argument0)
			{
				if (x + width < argument0)
				{
					width = argument0 - x;
				}
			}
			else
			{
				width = x + width - argument0;
				x = argument0;
			}
			if (y < argument1)
			{
				if (y + height < argument1)
				{
					height = argument1 - y;
				}
			}
			else
			{
				height = y + height - argument1;
				y = argument1;
			}
		}

		public new string ToString()
		{
			return x + " , " + y + " , " + width + " , " + height;
		}

		public void expand(float argument0, float argument1)
		{
			x -= argument0;
			y -= argument1;
			width += argument0 * 2f;
			height += argument1 * 2f;
		}
	}
	internal class CubismType_48
	{
		protected float[] CubismField_338 = new float[16];

		public CubismType_48()
		{
			_3000_0020();
		}

		public CubismType_48(float[] argument0)
		{
			_3000_0020_3000_3000(argument0);
		}

		public void _3000_0020()
		{
			for (int i = 0; i < 16; i++)
			{
				CubismField_338[i] = ((i % 5 == 0) ? 1 : 0);
			}
		}

		public float[] _0020()
		{
			return CubismField_338;
		}

		public float[] _3000()
		{
			return (float[])CubismField_338.Clone();
		}

		public void _3000_0020_3000_3000(float[] argument0)
		{
			if (argument0 != null && CubismField_338.Length == argument0.Length)
			{
				for (int i = 0; i < 16; i++)
				{
					CubismField_338[i] = argument0[i];
				}
			}
		}

		public void _3000_0020_3000_3000(CubismType_48 argument0)
		{
			for (int i = 0; i < 16; i++)
			{
				CubismField_338[i] = argument0.CubismField_338[i];
			}
		}

		public override string ToString()
		{
			string text = "";
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					text += $"{CubismField_338[i + (j << 2)]:F4} ";
				}
				text += "\n";
			}
			return text;
		}

		public CubismType_48 _3000_3000(CubismType_48 argument0, CubismType_48 argument1, bool argument2)
		{
			if (argument1 == null)
			{
				argument1 = new CubismType_48();
			}
			if (this == argument1)
			{
				_3000_0020_3000(CubismField_338, argument0.CubismField_338, argument1.CubismField_338, argument2);
			}
			else
			{
				_3000_0020_0020(CubismField_338, argument0.CubismField_338, argument1.CubismField_338, argument2);
			}
			return argument1;
		}

		private void _3000_0020_3000(float[] argument0, float[] argument1, float[] argument2, bool argument3)
		{
			if (argument0 == argument2)
			{
				float[] array = new float[16];
				_3000_0020_0020(argument0, argument1, array, argument3);
				for (int num = 15; num >= 0; num--)
				{
					argument2[num] = array[num];
				}
			}
			else
			{
				_3000_0020_0020(argument0, argument1, argument2, argument3);
			}
		}

		private void _3000_0020_0020(float[] argument0, float[] argument1, float[] argument2, bool argument3)
		{
			if (argument3)
			{
				argument2[0] = argument0[0] * argument1[0] + argument0[4] * argument1[1] + argument0[8] * argument1[2];
				argument2[4] = argument0[0] * argument1[4] + argument0[4] * argument1[5] + argument0[8] * argument1[6];
				argument2[8] = argument0[0] * argument1[8] + argument0[4] * argument1[9] + argument0[8] * argument1[10];
				argument2[12] = argument0[0] * argument1[12] + argument0[4] * argument1[13] + argument0[8] * argument1[14] + argument0[12];
				argument2[1] = argument0[1] * argument1[0] + argument0[5] * argument1[1] + argument0[9] * argument1[2];
				argument2[5] = argument0[1] * argument1[4] + argument0[5] * argument1[5] + argument0[9] * argument1[6];
				argument2[9] = argument0[1] * argument1[8] + argument0[5] * argument1[9] + argument0[9] * argument1[10];
				argument2[13] = argument0[1] * argument1[12] + argument0[5] * argument1[13] + argument0[9] * argument1[14] + argument0[13];
				argument2[2] = argument0[2] * argument1[0] + argument0[6] * argument1[1] + argument0[10] * argument1[2];
				argument2[6] = argument0[2] * argument1[4] + argument0[6] * argument1[5] + argument0[10] * argument1[6];
				argument2[10] = argument0[2] * argument1[8] + argument0[6] * argument1[9] + argument0[10] * argument1[10];
				argument2[14] = argument0[2] * argument1[12] + argument0[6] * argument1[13] + argument0[10] * argument1[14] + argument0[14];
				argument2[3] = (argument2[7] = (argument2[11] = 0f));
				argument2[15] = 1f;
			}
			else
			{
				argument2[0] = argument0[0] * argument1[0] + argument0[4] * argument1[1] + argument0[8] * argument1[2] + argument0[12] * argument1[3];
				argument2[4] = argument0[0] * argument1[4] + argument0[4] * argument1[5] + argument0[8] * argument1[6] + argument0[12] * argument1[7];
				argument2[8] = argument0[0] * argument1[8] + argument0[4] * argument1[9] + argument0[8] * argument1[10] + argument0[12] * argument1[11];
				argument2[12] = argument0[0] * argument1[12] + argument0[4] * argument1[13] + argument0[8] * argument1[14] + argument0[12] * argument1[15];
				argument2[1] = argument0[1] * argument1[0] + argument0[5] * argument1[1] + argument0[9] * argument1[2] + argument0[13] * argument1[3];
				argument2[5] = argument0[1] * argument1[4] + argument0[5] * argument1[5] + argument0[9] * argument1[6] + argument0[13] * argument1[7];
				argument2[9] = argument0[1] * argument1[8] + argument0[5] * argument1[9] + argument0[9] * argument1[10] + argument0[13] * argument1[11];
				argument2[13] = argument0[1] * argument1[12] + argument0[5] * argument1[13] + argument0[9] * argument1[14] + argument0[13] * argument1[15];
				argument2[2] = argument0[2] * argument1[0] + argument0[6] * argument1[1] + argument0[10] * argument1[2] + argument0[14] * argument1[3];
				argument2[6] = argument0[2] * argument1[4] + argument0[6] * argument1[5] + argument0[10] * argument1[6] + argument0[14] * argument1[7];
				argument2[10] = argument0[2] * argument1[8] + argument0[6] * argument1[9] + argument0[10] * argument1[10] + argument0[14] * argument1[11];
				argument2[14] = argument0[2] * argument1[12] + argument0[6] * argument1[13] + argument0[10] * argument1[14] + argument0[14] * argument1[15];
				argument2[3] = argument0[3] * argument1[0] + argument0[7] * argument1[1] + argument0[11] * argument1[2] + argument0[15] * argument1[3];
				argument2[7] = argument0[3] * argument1[4] + argument0[7] * argument1[5] + argument0[11] * argument1[6] + argument0[15] * argument1[7];
				argument2[11] = argument0[3] * argument1[8] + argument0[7] * argument1[9] + argument0[11] * argument1[10] + argument0[15] * argument1[11];
				argument2[15] = argument0[3] * argument1[12] + argument0[7] * argument1[13] + argument0[11] * argument1[14] + argument0[15] * argument1[15];
			}
		}

		public void _3000_3000_0020_0020(float argument0, float argument1, float argument2)
		{
			CubismField_338[12] = CubismField_338[0] * argument0 + CubismField_338[4] * argument1 + CubismField_338[8] * argument2 + CubismField_338[12];
			CubismField_338[13] = CubismField_338[1] * argument0 + CubismField_338[5] * argument1 + CubismField_338[9] * argument2 + CubismField_338[13];
			CubismField_338[14] = CubismField_338[2] * argument0 + CubismField_338[6] * argument1 + CubismField_338[10] * argument2 + CubismField_338[14];
			CubismField_338[15] = CubismField_338[3] * argument0 + CubismField_338[7] * argument1 + CubismField_338[11] * argument2 + CubismField_338[15];
		}

		public void _3000_0020_0020_3000(float argument0, float argument1, float argument2)
		{
			CubismField_338[0] *= argument0;
			CubismField_338[4] *= argument1;
			CubismField_338[8] *= argument2;
			CubismField_338[1] *= argument0;
			CubismField_338[5] *= argument1;
			CubismField_338[9] *= argument2;
			CubismField_338[2] *= argument0;
			CubismField_338[6] *= argument1;
			CubismField_338[10] *= argument2;
			CubismField_338[3] *= argument0;
			CubismField_338[7] *= argument1;
			CubismField_338[11] *= argument2;
		}

		public void _3000_3000_0020(float argument0)
		{
			float num = Mathf.Cos(argument0);
			float num2 = Mathf.Sin(argument0);
			float num3 = CubismField_338[4];
			CubismField_338[4] = num3 * num + CubismField_338[8] * num2;
			CubismField_338[8] = num3 * (0f - num2) + CubismField_338[8] * num;
			num3 = CubismField_338[5];
			CubismField_338[5] = num3 * num + CubismField_338[9] * num2;
			CubismField_338[9] = num3 * (0f - num2) + CubismField_338[9] * num;
			num3 = CubismField_338[6];
			CubismField_338[6] = num3 * num + CubismField_338[10] * num2;
			CubismField_338[10] = num3 * (0f - num2) + CubismField_338[10] * num;
			num3 = CubismField_338[7];
			CubismField_338[7] = num3 * num + CubismField_338[11] * num2;
			CubismField_338[11] = num3 * (0f - num2) + CubismField_338[11] * num;
		}

		public void _3000_3000_3000(float argument0)
		{
			float num = Mathf.Cos(argument0);
			float num2 = Mathf.Sin(argument0);
			float num3 = CubismField_338[0];
			CubismField_338[0] = num3 * num + CubismField_338[8] * (0f - num2);
			CubismField_338[8] = num3 * num2 + CubismField_338[8] * num;
			num3 = CubismField_338[1];
			CubismField_338[1] = num3 * num + CubismField_338[9] * (0f - num2);
			CubismField_338[9] = num3 * num2 + CubismField_338[9] * num;
			num3 = CubismField_338[2];
			CubismField_338[2] = num3 * num + CubismField_338[10] * (0f - num2);
			CubismField_338[10] = num3 * num2 + CubismField_338[10] * num;
			num3 = CubismField_338[3];
			CubismField_338[3] = num3 * num + CubismField_338[11] * (0f - num2);
			CubismField_338[11] = num3 * num2 + CubismField_338[11] * num;
		}

		public void _3000_0020_0020_0020(float argument0)
		{
			float num = Mathf.Cos(argument0);
			float num2 = Mathf.Sin(argument0);
			float num3 = CubismField_338[0];
			CubismField_338[0] = num3 * num + CubismField_338[4] * num2;
			CubismField_338[4] = num3 * (0f - num2) + CubismField_338[4] * num;
			num3 = CubismField_338[1];
			CubismField_338[1] = num3 * num + CubismField_338[5] * num2;
			CubismField_338[5] = num3 * (0f - num2) + CubismField_338[5] * num;
			num3 = CubismField_338[2];
			CubismField_338[2] = num3 * num + CubismField_338[6] * num2;
			CubismField_338[6] = num3 * (0f - num2) + CubismField_338[6] * num;
			num3 = CubismField_338[3];
			CubismField_338[3] = num3 * num + CubismField_338[7] * num2;
			CubismField_338[7] = num3 * (0f - num2) + CubismField_338[7] * num;
		}

		public LDPointF _3000_0020_3000_0020(LDPointF argument0)
		{
			float argument = CubismField_338[0] * argument0.x + CubismField_338[4] * argument0.y + CubismField_338[12];
			float argument2 = CubismField_338[1] * argument0.x + CubismField_338[5] * argument0.y + CubismField_338[13];
			return new LDPointF(argument, argument2);
		}
	}
	public struct LDPointF
	{
		public float x;

		public float y;

		public LDPointF(float argument0, float argument1)
		{
			x = argument0;
			y = argument1;
		}

		public LDPointF(LDPointF argument0)
		{
			x = argument0.x;
			y = argument0.y;
		}

		public void setPoint(float argument0, float argument1)
		{
			x = argument0;
			y = argument1;
		}

		public void setPoint(LDPointF argument0)
		{
			x = argument0.x;
			y = argument0.y;
		}
	}
	internal class CubismType_50 : CubismType_42
	{
		private static bool CubismField_341 = true;

		private bool CubismField_342;

		public CubismType_50(bool argument0)
		{
			CubismField_342 = argument0;
		}

		public override void _3000_3000_3000()
		{
			base._3000_3000_3000();
		}

		public override void setupDraw()
		{
			base.setupDraw();
			if (0 < CubismField_70)
			{
				CubismType_59._3000_0020()._3000_0020_0020_3000(CubismField_70);
			}
		}

		protected override void _3000_0020_0020_0020_3000(Vector3[] argument0)
		{
			for (int i = 0; i < argument0.Length; i++)
			{
				argument0[i].z = 0f;
			}
		}

		public override void drawLast()
		{
			if (CubismField_341 && !CubismField_342 && Application.isPlaying)
			{
				if (!CubismType_51._3000_0020())
				{
					UnityEngine.Debug.LogError("Live2D Cubism 2.1 / RenderMode.DrawMeshNow Error\nlive2dmodel.draw() must be called in OnPostRender() or OnRenderObject() .\n-------------------\n");
				}
				CubismField_341 = false;
			}
			CubismType_59 cubismType_ = CubismType_59._3000_0020();
			int count = CubismField_291.Count;
			for (int i = 0; i < count; i++)
			{
				CubismType_57 cubismType_2 = CubismField_291[i];
				if (cubismType_2.commands.Count != 0)
				{
					int textureNo = cubismType_2.textureNo;
					if (CubismField_294.Count > textureNo)
					{
						Texture2D argument = CubismField_294[textureNo];
						cubismType_._3000_0020_3000_3000(this, cubismType_2, argument, CubismField_76);
						cubismType_._3000(cubismType_2, CubismField_296, null, 0);
					}
				}
			}
		}

		internal override ColorConverter _3000(int argument0)
		{
			if (argument0 < 0)
			{
				return null;
			}
			if (!CubismField_72.ContainsKey(argument0))
			{
				CubismField_72[argument0] = new ColorConverterUnity();
			}
			return CubismField_72[argument0];
		}
	}
	internal class CubismType_51
	{
		private static bool CubismField_343;

		private static bool CubismField_344;

		private static bool CubismField_345;

		internal static void _0020(UnityEngine.Object argument0)
		{
			UnityEngine.Object.DestroyImmediate(argument0, allowDestroyingAssets: true);
		}

		internal static void _3000_0020_0020_3000(bool argument0, Material argument1, MaterialPropertyBlock argument2, string argument3, Texture argument4)
		{
			if (argument0)
			{
				argument1.SetTexture(argument3, argument4);
			}
			else
			{
				argument2.SetTexture(argument3, argument4);
			}
		}

		internal static void _3000_0020_0020_3000(bool argument0, Material argument1, MaterialPropertyBlock argument2, int argument3, Texture argument4)
		{
			if (argument0)
			{
				argument1.SetTexture(argument3, argument4);
			}
			else
			{
				argument2.SetTexture(argument3, argument4);
			}
		}

		internal static void _3000_0020_0020_0020(bool argument0, Material argument1, MaterialPropertyBlock argument2, string argument3, Matrix4x4 argument4)
		{
			if (argument0)
			{
				argument1.SetMatrix(argument3, argument4);
			}
			else
			{
				argument2.SetMatrix(argument3, argument4);
			}
		}

		internal static void _3000_0020_0020_0020(bool argument0, Material argument1, MaterialPropertyBlock argument2, int argument3, Matrix4x4 argument4)
		{
			if (argument0)
			{
				argument1.SetMatrix(argument3, argument4);
			}
			else
			{
				argument2.SetMatrix(argument3, argument4);
			}
		}

		internal static void _3000_3000_3000(bool argument0, Material argument1, MaterialPropertyBlock argument2, string argument3, float argument4)
		{
			if (argument0)
			{
				argument1.SetFloat(argument3, argument4);
			}
			else
			{
				argument2.SetFloat(argument3, argument4);
			}
		}

		internal static void _3000_3000_3000(bool argument0, Material argument1, MaterialPropertyBlock argument2, int argument3, float argument4)
		{
			if (argument0)
			{
				argument1.SetFloat(argument3, argument4);
			}
			else
			{
				argument2.SetFloat(argument3, argument4);
			}
		}

		internal static void _3000_0020_3000_0020(bool argument0, Material argument1, MaterialPropertyBlock argument2, string argument3, Vector4 argument4)
		{
			if (argument0)
			{
				argument1.SetVector(argument3, argument4);
			}
			else
			{
				argument2.SetVector(argument3, argument4);
			}
		}

		internal static void _3000_0020_3000_0020(bool argument0, Material argument1, MaterialPropertyBlock argument2, int argument3, Vector4 argument4)
		{
			if (argument0)
			{
				argument1.SetVector(argument3, argument4);
			}
			else
			{
				argument2.SetVector(argument3, argument4);
			}
		}

		internal static void _3000_3000_0020(bool argument0, Material argument1, MaterialPropertyBlock argument2, string argument3, Color argument4)
		{
			if (argument0)
			{
				argument1.SetColor(argument3, argument4);
			}
			else
			{
				argument2.SetColor(argument3, argument4);
			}
		}

		internal static void _3000_3000_0020(bool argument0, Material argument1, MaterialPropertyBlock argument2, int argument3, Color argument4)
		{
			if (argument0)
			{
				argument1.SetColor(argument3, argument4);
			}
			else
			{
				argument2.SetColor(argument3, argument4);
			}
		}

		internal static bool _3000_3000()
		{
			string text = StackTraceUtility.ExtractStackTrace();
			int num = text.LastIndexOf('\n', text.Length - 3);
			if (text.IndexOf("UnityEditor.", num) == num)
			{
				num = text.LastIndexOf('\n', num - 1);
			}
			if (text.IndexOf(":Update()", num) >= 0 || text.IndexOf(":LateUpdate()", num) >= 0)
			{
				return true;
			}
			return false;
		}

		internal static bool _3000_0020()
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				return true;
			}
			string text = StackTraceUtility.ExtractStackTrace();
			if (text.IndexOf("OnPostRender()") == -1 && text.IndexOf("OnRenderObject()") == -1)
			{
				return false;
			}
			return true;
		}

		internal static bool _3000_0020_0020()
		{
			return Application.unityVersion.StartsWith("4");
		}

		internal static bool _3000()
		{
			if (CubismField_344)
			{
				return CubismField_345;
			}
			CubismField_344 = true;
			RenderTexture renderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32);
			CubismField_345 = renderTexture.Create();
			_0020(renderTexture);
			return CubismField_345;
		}
	}
	public enum ColorConvertMethod
	{
		NONE,
		SPECIFIC,
		COLOR_LIST,
		HSL
	}
	public class ColorConvertData
	{
		public ColorConvertMethod methodType;

		public float mainLineHigh = 1f;

		public float mainLineLow = 0.5f;

		protected float[] CubismField_354 = new float[16];

		public void setMatrix(float[] argument0)
		{
			if (argument0.Length == 16)
			{
				for (int i = 0; i < 16; i++)
				{
					CubismField_354[i] = argument0[i];
				}
			}
		}

		public float[] getMatrix()
		{
			return CubismField_354;
		}

		public override string ToString()
		{
			return base.ToString() + $" type:{methodType.ToString()}";
		}
	}
	public class ColorConverter
	{
		public ColorConvertData data = new ColorConvertData();

		public virtual void clearCache()
		{
		}

		public override string ToString()
		{
			return base.ToString() + " data:" + data.ToString();
		}
	}
	public class ColorConverterUnity : ColorConverter
	{
		protected Texture2D CubismField_356;

		protected Matrix4x4 CubismField_357;

		protected bool CubismField_358;

		public void setTexture(Texture2D argument0)
		{
			CubismField_356 = argument0;
		}

		public Texture2D getTexture()
		{
			return CubismField_356;
		}

		public Matrix4x4 getConvertMatrix()
		{
			if (CubismField_358)
			{
				return CubismField_357;
			}
			float[] matrix = data.getMatrix();
			for (int i = 0; i < 16; i++)
			{
				CubismField_357[i] = matrix[i];
			}
			CubismField_358 = true;
			return CubismField_357;
		}

		public override void clearCache()
		{
			CubismField_358 = false;
		}
	}
	internal class CubismType_56
	{
		public string name;

		public CombineInstance combine;

		public int opacity;

		public Color color;

		public Vector3[] vertices;

		public Color[] colors;

		public CubismType_56()
		{
			combine = default(CombineInstance);
			combine.transform = Matrix4x4.identity;
			color.a = 0f;
			opacity = -1;
		}

		~CubismType_56()
		{
		}

		public void _0020()
		{
			CubismType_51._0020(combine.mesh);
		}

		public void _3000(Mesh argument0)
		{
			combine.mesh = argument0;
			combine.mesh.hideFlags = HideFlags.DontSaveInEditor;
			vertices = argument0.vertices;
			colors = argument0.colors;
		}

		public string _3000_0020()
		{
			return $"DrawCommand {name} opacity:{opacity} color{color.ToString()}";
		}
	}
	internal class CubismType_57
	{
		public Mesh mesh;

		public int textureNo;

		public int colorGroupNo;

		public int clippingProcess;

		public bool culling;

		public int colorType;

		public CubismType_43.CubismType_44 clipBufPre_clipContextMask;

		public CubismType_43.CubismType_44 clipBufPre_clipContextDraw;

		public List<CubismType_56> commands;

		private CombineInstance[] CubismField_374;

		public CubismType_57()
		{
			mesh = new Mesh();
			mesh.MarkDynamic();
			mesh.Optimize();
			commands = new List<CubismType_56>();
			textureNo = -1;
			culling = false;
			colorType = 0;
			clippingProcess = 0;
		}

		public void _3000_0020()
		{
			CubismType_51._0020(mesh);
		}

		public CombineInstance[] _3000()
		{
			int count = commands.Count;
			int num = ((CubismField_374 != null) ? CubismField_374.Length : 0);
			if (count != num)
			{
				CubismField_374 = new CombineInstance[count];
			}
			for (int i = 0; i < count; i++)
			{
				CubismField_374[i] = commands[i].combine;
			}
			return CubismField_374;
		}

		public bool _0020(int argument0, bool argument1, int argument2, int argument3, int argument4, CubismType_43.CubismType_44 argument5, CubismType_43.CubismType_44 argument6)
		{
			if (textureNo != argument0 || culling != argument1 || colorType != argument2 || colorGroupNo != argument3 || clippingProcess != argument4 || clipBufPre_clipContextMask != argument5 || clipBufPre_clipContextDraw != argument6)
			{
				return false;
			}
			return true;
		}

		public string _3000_3000(int argument0 = 0)
		{
			string text = $"DrawCommandBatch texture:{textureNo} colorGroupNo:{colorGroupNo} culling:{culling} colorType:{colorType} clip:{clippingProcess} length:{commands.Count}";
			for (int i = 0; i < commands.Count; i++)
			{
				text += "\n\t";
				for (int j = 0; j < argument0; j++)
				{
					text += "\t";
				}
				text += commands[i]._3000_0020();
			}
			return text;
		}
	}
	internal class CubismType_59
	{
		internal class CubismType_61
		{
			internal Material CubismField_423;
		}

		private const int CubismField_379 = 0;

		private const int CubismField_380 = 4;

		private const int CubismField_381 = 8;

		private const int CubismField_382 = 12;

		private const int CubismField_383 = 24;

		private const int CubismField_384 = 5;

		private const int CubismField_385 = 10;

		private const int CubismField_386 = 1;

		private const int CubismField_387 = 0;

		private const int CubismField_388 = 2;

		private const int CubismField_389 = 2;

		private const int CubismField_390 = 0;

		private readonly string[] CubismField_391 = new string[36]
		{
			"Live2D/Live2D", "Live2D/Live2D Masked", "Live2D/Live2D PremultipliedAlpha", "Live2D/Live2D Masked PremultipliedAlpha", "Live2D/Live2D", "Live2D/Live2D Masked", "Live2D/Live2D PremultipliedAlpha", "Live2D/Live2D Masked PremultipliedAlpha", "Live2D/Live2D", "Live2D/Live2D Masked",
			"Live2D/Live2D PremultipliedAlpha", "Live2D/Live2D Masked PremultipliedAlpha", "Live2D/HSL Normal", "Live2D/HSL Normal Masked", "Live2D/HSL Normal PremultipliedAlpha", "Live2D/HSL Normal Masked PremultipliedAlpha", "Live2D/HSL Add", "Live2D/HSL Add Masked", "Live2D/HSL Add PremultipliedAlpha", "Live2D/HSL Add Masked PremultipliedAlpha",
			"Live2D/HSL Mult", "Live2D/HSL Mult Masked", "Live2D/HSL Mult PremultipliedAlpha", "Live2D/HSL Mult Masked PremultipliedAlpha", "Live2D/ColorList Normal", "Live2D/ColorList Normal Masked", "Live2D/ColorList Normal PremultipliedAlpha", "Live2D/ColorList Normal Masked PremultipliedAlpha", "Live2D/ColorList Add", "Live2D/ColorList Add Masked",
			"Live2D/ColorList Add PremultipliedAlpha", "Live2D/ColorList Add Masked PremultipliedAlpha", "Live2D/ColorList Mult", "Live2D/ColorList Mult Masked", "Live2D/ColorList Mult PremultipliedAlpha", "Live2D/ColorList Mult Masked PremultipliedAlpha"
		};

		private const string CubismField_392 = "Live2D/SetupMask";

		private static CubismType_59 CubismField_393;

		private CubismType_61[] CubismField_394 = new CubismType_61[36];

		private CubismType_61 CubismField_395;

		private bool CubismField_396 = true;

		private Material CubismField_397;

		private MaterialPropertyBlock CubismField_398 = new MaterialPropertyBlock();

		private int CubismField_399 = -1;

		private static bool CubismField_400 = Application.platform != RuntimePlatform.Android || Application.platform != RuntimePlatform.IPhonePlayer;

		private static HideFlags CubismField_401 = HideFlags.DontSaveInEditor;

		internal int CubismField_402 = Shader.PropertyToID("_Cull");

		internal int CubismField_403 = Shader.PropertyToID("_SrcColor");

		internal int CubismField_404 = Shader.PropertyToID("_DstColor");

		internal int CubismField_405 = Shader.PropertyToID("_SrcAlpha");

		internal int CubismField_406 = Shader.PropertyToID("_DstAlpha");

		internal int CubismField_407 = Shader.PropertyToID("_MainTex");

		internal int CubismField_408 = Shader.PropertyToID("_SubTex");

		internal int CubismField_409 = Shader.PropertyToID("_ConvertMat");

		internal int CubismField_410 = Shader.PropertyToID("_ChannelFlag");

		internal int CubismField_411 = Shader.PropertyToID("_MaskMatrix");

		internal int CubismField_412 = Shader.PropertyToID("_Bounds");

		internal int CubismField_413 = Shader.PropertyToID("_ClipTex");

		internal int CubismField_414 = Shader.PropertyToID("_ClipMatrix");

		internal int CubismField_415 = Shader.PropertyToID("_MainLineHigh");

		internal int CubismField_416 = Shader.PropertyToID("_MainLineLow");

		private static bool CubismField_417 = false;

		public static CubismType_59 _3000_0020()
		{
			if (CubismField_393 == null)
			{
				CubismField_393 = new CubismType_59();
				CubismField_393._3000_0020_3000();
			}
			return CubismField_393;
		}

		public static void _0020()
		{
			if (CubismField_393 != null)
			{
				CubismField_393._3000_3000_3000();
				CubismField_393 = null;
			}
		}

		private CubismType_59()
		{
		}

		private void _3000_0020_3000()
		{
		}

		internal CubismType_61 _3000_0020_0020(int argument0, int argument1, int argument2, int argument3, int argument4, int argument5)
		{
			if (CubismField_394[argument0] == null)
			{
				CubismField_394[argument0] = _3000_3000_0020(CubismField_391[argument0]);
				_3000_3000_0020_0020(CubismField_394[argument0].CubismField_423);
				if (CubismField_399 >= 0)
				{
					CubismField_394[argument0].CubismField_423.mainTexture.anisoLevel = CubismField_399;
				}
				_3000_0020_3000_0020(CubismField_394[argument0].CubismField_423, argument1, argument2, argument3, argument4, argument5);
			}
			return CubismField_394[argument0];
		}

		internal CubismType_61 _3000_3000()
		{
			if (CubismField_395 == null)
			{
				CubismField_395 = _3000_3000_0020("Live2D/SetupMask");
				_3000_3000_0020_0020(CubismField_395.CubismField_423);
			}
			return CubismField_395;
		}

		internal void _3000_3000_3000()
		{
			for (int num = CubismField_394.Length - 1; num >= 0; num--)
			{
				if (CubismField_394[num] != null)
				{
					_3000_0020_0020_0020(CubismField_394[num].CubismField_423);
					CubismField_394[num].CubismField_423 = null;
					CubismField_394[num] = null;
				}
			}
			if (CubismField_395 != null)
			{
				_3000_0020_0020_0020(CubismField_395.CubismField_423);
				CubismField_395.CubismField_423 = null;
				CubismField_395 = null;
			}
		}

		private static void _3000_3000_0020_0020(Material argument0)
		{
			if (!(argument0 == null))
			{
				if (CubismField_400)
				{
					argument0.hideFlags = CubismField_401;
				}
				else
				{
					argument0.hideFlags = HideFlags.None;
				}
			}
		}

		private static void _3000_0020_0020_0020(Material argument0)
		{
			if (!(argument0 == null))
			{
				CubismType_51._0020(argument0);
			}
		}

		internal CubismType_61 _3000_3000_0020(string argument0)
		{
			Material cubismField_ = new Material(Kairisei.Recovery.OriginalLive2DShaders.Resolve(argument0));
			return new CubismType_61
			{
				CubismField_423 = cubismField_
			};
		}

		internal void _3000_0020_0020_3000(int argument0)
		{
			CubismField_399 = argument0;
			for (int num = CubismField_394.Length - 1; num >= 0; num--)
			{
				if (CubismField_394[num] != null)
				{
					CubismField_394[num].CubismField_423.mainTexture.anisoLevel = argument0;
				}
			}
		}

		public void _3000_0020_3000_3000(CubismType_42 argument0, CubismType_57 argument1, Texture2D argument2, bool argument3)
		{
			CubismField_396 = argument0 is CubismType_50;
			Material material = null;
			ColorConverterUnity colorConverterUnity = ((argument1.colorGroupNo < 0) ? null : (argument0._3000(argument1.colorGroupNo) as ColorConverterUnity));
			if (argument1.clipBufPre_clipContextMask != null && !CubismField_417)
			{
				material = _3000_3000().CubismField_423;
				Color color = CubismType_43._3000_0020(argument1.clipBufPre_clipContextMask.CubismField_324);
				material.SetColor(CubismField_410, color);
				material.SetMatrix(CubismField_411, argument1.clipBufPre_clipContextMask.CubismField_327);
				LDRectF cubismField_ = argument1.clipBufPre_clipContextMask.CubismField_325;
				material.SetVector(CubismField_412, new Vector4(cubismField_.x * 2f - 1f, cubismField_.y * 2f - 1f, cubismField_.getRight() * 2f - 1f, cubismField_.getBottom() * 2f - 1f));
			}
			else
			{
				bool culling = argument1.culling;
				bool flag = argument1.clipBufPre_clipContextDraw != null && !CubismField_417;
				int num = (flag ? 1 : 0) + (argument3 ? 2 : 0);
				if (argument1.colorGroupNo != -1 && colorConverterUnity != null)
				{
					_ = argument1.colorType;
					switch (colorConverterUnity.data.methodType)
					{
					case ColorConvertMethod.COLOR_LIST:
						if (colorConverterUnity.getTexture() != null)
						{
							material = _3000_0020_0020(24 + num, 5, 10, 1, 10, culling ? 2 : 0).CubismField_423;
							CubismType_51._3000_0020_0020_3000(CubismField_396, material, CubismField_398, CubismField_408, colorConverterUnity.getTexture());
						}
						break;
					case ColorConvertMethod.HSL:
						material = _3000_0020_0020(12 + num, 5, 10, 1, 10, culling ? 2 : 0).CubismField_423;
						CubismType_51._3000_0020_0020_0020(CubismField_396, material, CubismField_398, CubismField_409, colorConverterUnity.getConvertMatrix());
						CubismType_51._3000_3000_3000(CubismField_396, material, CubismField_398, CubismField_415, colorConverterUnity.data.mainLineHigh);
						CubismType_51._3000_3000_3000(CubismField_396, material, CubismField_398, CubismField_416, colorConverterUnity.data.mainLineLow);
						break;
					}
				}
				if (material == null)
				{
					material = argument1.colorType switch
					{
						1 => _3000_0020_0020(4 + num, 5, 1, 0, 1, culling ? 2 : 0).CubismField_423,
						2 => _3000_0020_0020(8 + num, 2, 10, 0, 1, culling ? 2 : 0).CubismField_423,
						_ => _3000_0020_0020(num, 1, 10, 1, 10, culling ? 2 : 0).CubismField_423,
					};
				}
				if (flag)
				{
					RenderTexture cubismField_2 = argument1.clipBufPre_clipContextDraw._3000().CubismField_302;
					CubismType_51._3000_0020_0020_3000(CubismField_396, material, CubismField_398, CubismField_413, cubismField_2);
					Matrix4x4 cubismField_3 = argument1.clipBufPre_clipContextDraw.CubismField_328;
					CubismType_51._3000_0020_0020_0020(CubismField_396, material, CubismField_398, CubismField_414, cubismField_3);
					Color argument4 = CubismType_43._3000_0020(argument1.clipBufPre_clipContextDraw.CubismField_324);
					CubismType_51._3000_3000_0020(CubismField_396, material, CubismField_398, CubismField_410, argument4);
				}
			}
			if (material == null)
			{
				material = _3000_0020_0020(0, 5, 10, 1, 10, 2).CubismField_423;
			}
			CubismType_51._3000_0020_0020_3000(CubismField_396, material, CubismField_398, CubismField_407, argument2);
			if (CubismField_396)
			{
				material.SetPass(0);
			}
			CubismField_397 = material;
		}

		private void _3000_0020_3000_0020(Material argument0, int argument1, int argument2, int argument3, int argument4, int argument5)
		{
			argument0.SetInt(CubismField_403, argument1);
			argument0.SetInt(CubismField_404, argument2);
			argument0.SetInt(CubismField_405, argument3);
			argument0.SetInt(CubismField_406, argument4);
			argument0.SetFloat(CubismField_402, argument5);
		}

		public void _3000(CubismType_57 argument0, Matrix4x4 argument1, Camera argument2, int argument3)
		{
			Mesh mesh = argument0.mesh;
			if (CubismField_400 && Application.isEditor)
			{
				mesh.hideFlags = CubismField_401;
			}
			else
			{
				mesh.hideFlags = HideFlags.None;
			}
			mesh.CombineMeshes(argument0._3000());
			if (CubismField_396)
			{
				Graphics.DrawMeshNow(mesh, argument1);
			}
			else
			{
				Graphics.DrawMesh(mesh, argument1, CubismField_397, argument3, argument2, 0, CubismField_398);
			}
		}
	}
	internal class CubismType_60 : CubismType_42
	{
		protected CubismType_59 CubismField_418;

		private int CubismField_419;

		private float CubismField_420;

		private const float CubismField_421 = 0.01f;

		private static bool CubismField_422 = true;

		public CubismType_60()
		{
			CubismField_418 = CubismType_59._3000_0020();
		}

		public override void _3000_3000_3000()
		{
			base._3000_3000_3000();
		}

		public override void setupDraw()
		{
			base.setupDraw();
			CubismField_420 = 0f;
			if (0 < CubismField_70)
			{
				CubismField_418._3000_0020_0020_3000(CubismField_70);
			}
		}

		protected override void _3000_0020_0020_0020_3000(Vector3[] argument0)
		{
			for (int i = 0; i < argument0.Length; i++)
			{
				argument0[i].z = CubismField_420;
			}
			CubismField_420 += 0.01f;
		}

		public override void drawLast()
		{
			int count = CubismField_291.Count;
			for (int i = 0; i < count; i++)
			{
				CubismType_57 cubismType_ = CubismField_291[i];
				if (cubismType_.commands.Count != 0)
				{
					int textureNo = cubismType_.textureNo;
					if (CubismField_294.Count > textureNo)
					{
						Texture2D argument = CubismField_294[textureNo];
						CubismField_418._3000_0020_3000_3000(this, cubismType_, argument, CubismField_76);
						CubismField_418._3000(cubismType_, CubismField_296, null, CubismField_419);
					}
				}
			}
		}

		public new void _3000(int argument0)
		{
			CubismField_419 = argument0;
		}

		public int _0020()
		{
			return CubismField_419;
		}
	}
	internal class CubismType_62
	{
		public const int LIVE2D_FORMAT_VERSION_V2_6_INTIAL = 6;

		public const int LIVE2D_FORMAT_VERSION_V2_7_OPACITY = 7;

		public const int LIVE2D_FORMAT_VERSION_V2_8_TEX_OPTION = 8;

		public const int LIVE2D_FORMAT_VERSION_V2_9_AVATAR_PARTS = 9;

		public const int LIVE2D_FORMAT_VERSION_V2_10_SDK2 = 10;

		public const int LIVE2D_FORMAT_VERSION_V2_11_SDK2_1 = 11;

		public const int LIVE2D_FORMAT_VERSION_AVAILABLE = 11;

		public const int LIVE2D_FORMAT_EOF_VALUE = -2004318072;

		public const int NULL_NO = 0;

		public const int ARRAY_NO = 23;

		public const int OBJECT_REF = 33;

		private static void _3000(int argument0)
		{
			UtDebug.print("FileFormat1 :: not implemented classNo : {0}\n", argument0);
		}

		public static object _0020(int argument0)
		{
			if (argument0 < 40)
			{
				_3000(argument0);
				return null;
			}
			if (argument0 < 50)
			{
				_3000(argument0);
				return null;
			}
			if (argument0 < 60)
			{
				_3000(argument0);
				return null;
			}
			if (argument0 < 100)
			{
				switch (argument0)
				{
				case 65:
					return new CubismType_31();
				case 66:
					return new PivotManager();
				case 67:
					return new ParamPivots();
				case 68:
					return new CubismType_25();
				case 69:
					return new CubismType_24();
				case 70:
					return new CubismType_15();
				default:
					_3000(argument0);
					return null;
				}
			}
			switch (argument0)
			{
			case 131:
				return new ParamDefFloat();
			case 133:
				return new PartsData();
			case 136:
				return new ModelImpl();
			case 137:
				return new ParamDefSet();
			case 142:
				return new AvatarPartsItem();
			default:
				_3000(argument0);
				return null;
			}
		}
	}
	public struct LDColor
	{
		public int color;

		public LDColor(int argument0, bool argument1)
		{
			if (!argument1)
			{
				argument0 |= -16777216;
			}
			color = argument0;
		}
	}
	public struct LDRect
	{
		public int x;

		public int y;

		public int width;

		public int height;

		public LDRect(int argument0, int argument1, int argument2, int argument3)
		{
			x = argument0;
			y = argument1;
			width = argument2;
			height = argument3;
		}

		public LDRect(LDRect argument0)
		{
			x = argument0.x;
			y = argument0.y;
			width = argument0.width;
			height = argument0.height;
		}

		public float getCenterX()
		{
			return 0.5f * (float)(x + x + width);
		}

		public float getCenterY()
		{
			return 0.5f * (float)(y + y + height);
		}

		public int getRight()
		{
			return x + width;
		}

		public int getBottom()
		{
			return y + height;
		}

		public void setRect(int argument0, int argument1, int argument2, int argument3)
		{
			x = argument0;
			y = argument1;
			width = argument2;
			height = argument3;
		}

		public void setRect(LDRect argument0)
		{
			x = argument0.x;
			y = argument0.y;
			width = argument0.width;
			height = argument0.height;
		}
	}
	public struct LDPoint
	{
		public int x;

		public int y;

		public LDPoint(int argument0, int argument1)
		{
			x = argument0;
			y = argument1;
		}

		public LDPoint(LDPoint argument0)
		{
			x = argument0.x;
			y = argument0.y;
		}

		public void setPoint(LDPoint argument0)
		{
			x = argument0.x;
			y = argument0.y;
		}

		public void setPoint(int argument0, int argument1)
		{
			x = argument0;
			y = argument1;
		}
	}
	public class ParamPivots : ISerializableV2
	{
		internal const int CubismField_442 = -2;

		private int CubismField_443;

		private ParamID CubismField_444;

		private float[] CubismField_445;

		private int CubismField_446 = -2;

		private int CubismField_447 = -1;

		private int CubismField_448;

		private float CubismField_449;

		internal ParamPivots()
		{
		}

		public void readV2(BReader argument0)
		{
			CubismField_444 = (ParamID)argument0.readObject();
			CubismField_443 = argument0.readInt();
			CubismField_445 = (float[])argument0.readObject();
		}

		internal int _3000(int argument0)
		{
			if (CubismField_447 != argument0)
			{
				CubismField_446 = -2;
			}
			return CubismField_446;
		}

		internal void _3000_3000_3000(int argument0, int argument1)
		{
			CubismField_446 = argument0;
			CubismField_447 = argument1;
		}

		internal ParamID _0020()
		{
			return CubismField_444;
		}

		internal void _3000_3000_0020(ParamID argument0)
		{
			CubismField_444 = argument0;
		}

		internal int _3000_0020()
		{
			return CubismField_443;
		}

		internal float[] _3000_3000()
		{
			return CubismField_445;
		}

		internal void _3000_0020_0020_0020(int argument0, float[] argument1)
		{
			CubismField_443 = argument0;
			CubismField_445 = argument1;
		}

		internal int _3000_0020_0020()
		{
			return CubismField_448;
		}

		internal void _3000_0020_0020_3000(int argument0)
		{
			CubismField_448 = argument0;
		}

		internal float _3000_0020_3000()
		{
			return CubismField_449;
		}

		internal void _3000_0020_3000_0020(float argument0)
		{
			CubismField_449 = argument0;
		}
	}
	public class ParamDefFloat : ISerializableV2
	{
		private float CubismField_450;

		private float CubismField_451;

		private float CubismField_452;

		private ParamID CubismField_453;

		public ParamDefFloat()
		{
		}

		public ParamDefFloat(ParamID argument0, float argument1, float argument2, float argument3)
		{
			CubismField_453 = argument0;
			CubismField_450 = argument1;
			CubismField_451 = argument2;
			CubismField_452 = argument3;
		}

		public void readV2(BReader argument0)
		{
			CubismField_450 = argument0.readFloat();
			CubismField_451 = argument0.readFloat();
			CubismField_452 = argument0.readFloat();
			CubismField_453 = (ParamID)argument0.readObject();
		}

		public float getMinValue()
		{
			return CubismField_450;
		}

		public float getMaxValue()
		{
			return CubismField_451;
		}

		public float getDefaultValue()
		{
			return CubismField_452;
		}

		public ParamID getParamID()
		{
			return CubismField_453;
		}
	}
	public class ParamDefSet : ISerializableV2
	{
		private List<ParamDefFloat> CubismField_454;

		public List<ParamDefFloat> getParamDefFloatList()
		{
			return CubismField_454;
		}

		public void initDirect()
		{
			CubismField_454 = new List<ParamDefFloat>();
		}

		public void readV2(BReader argument0)
		{
			List<object> list = argument0.readObject() as List<object>;
			CubismField_454 = new List<ParamDefFloat>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				CubismField_454.Add((ParamDefFloat)list[i]);
			}
		}

		internal void _0020(ParamDefFloat argument0)
		{
			CubismField_454.Add(argument0);
		}
	}
	public class Live2DModelUnity : ALive2DModel
	{
		private int CubismField_460;

		private CubismType_42 CubismField_461;

		private Stream CubismField_462;

		private static bool CubismField_463 = true;

		public void releaseModel()
		{
			_0020()._3000_3000_3000();
			CubismField_5._3000_0020_3000();
		}

		[Obsolete]
		public override void deleteTextures()
		{
			UtDebug.println("not used. Live2DModelUnity.deleteTextures().");
		}

		public void setMatrix(Matrix4x4 argument0)
		{
			_0020()._3000_3000_0020_3000(argument0);
		}

		public void setMatrix(float[] argument0)
		{
			_0020().setMatrix(argument0);
		}

		public override void update()
		{
			if (!CubismField_7)
			{
				base.update();
				CubismField_5.preDraw(_0020());
			}
		}

		public override void draw()
		{
			if (!CubismField_7)
			{
				CubismField_5.draw(_0020());
			}
		}

		public void setTexture(int argument0, Texture2D argument1)
		{
			_0020()._3000_3000_3000_0020(argument0, argument1);
		}

		internal static Live2DModelUnity _3000_3000(string argument0)
		{
			return _3000_3000(UtFile.loadFile(argument0));
		}

		internal static Live2DModelUnity _3000_3000(byte[] argument0)
		{
			return _3000_3000(new MemoryStream(argument0));
		}

		internal static Live2DModelUnity _3000_3000(Stream argument0)
		{
			Live2DModelUnity live2DModelUnity = new Live2DModelUnity();
			live2DModelUnity.CubismField_7 = true;
			live2DModelUnity.CubismField_462 = argument0;
			new Thread(live2DModelUnity._3000_0020).Start();
			return live2DModelUnity;
		}

		internal void _3000_0020()
		{
			ALive2DModel.loadModel_exe(this, CubismField_462);
			CubismField_462 = null;
		}

		internal bool _3000()
		{
			return CubismField_7;
		}

		public static Live2DModelUnity loadModel(string argument0)
		{
			try
			{
				Live2DModelUnity live2DModelUnity = new Live2DModelUnity();
				ALive2DModel.loadModel_exe(live2DModelUnity, argument0);
				return live2DModelUnity;
			}
			catch (Exception)
			{
				UtDebug.print("Failed to load model : " + argument0);
			}
			return null;
		}

		public static Live2DModelUnity loadModel(Stream argument0)
		{
			Live2DModelUnity live2DModelUnity = new Live2DModelUnity();
			ALive2DModel.loadModel_exe(live2DModelUnity, argument0);
			return live2DModelUnity;
		}

		public static Live2DModelUnity loadModel(byte[] argument0)
		{
			return loadModel(new MemoryStream(argument0));
		}

		public override DrawParam getDrawParam()
		{
			return _0020();
		}

		private CubismType_42 _0020()
		{
			if (CubismField_461 == null)
			{
				if (CubismField_460 == 1)
				{
					CubismField_461 = new CubismType_60();
				}
				else
				{
					CubismField_461 = new CubismType_50(argument0: false);
				}
				setCulling(argument0: false);
			}
			return CubismField_461;
		}

		private void _3000_0020_0020()
		{
			CubismField_461 = null;
		}

		public void enableCullingSetting(bool argument0)
		{
			_0020().setLockCulling(!argument0);
		}

		[Obsolete]
		public void setCulling(bool argument0)
		{
			CubismType_42 cubismType_ = _0020();
			cubismType_.setLockCulling(argument0: false);
			cubismType_.setCulling(argument0);
			cubismType_.setLockCulling(argument0: true);
		}

		[Obsolete]
		public bool getCulling()
		{
			return _0020().getCulling();
		}

		public void setLayer(int argument0)
		{
			if (!(_0020() is CubismType_60 cubismType_))
			{
				UtDebug.error("Layer can not set.");
			}
			else
			{
				cubismType_._3000(argument0);
			}
		}

		public int getLayer()
		{
			if (!(_0020() is CubismType_60 cubismType_))
			{
				UtDebug.error("Layer can not set.");
				return -1;
			}
			return cubismType_._0020();
		}

		public void setRenderMode(int argument0)
		{
			if (CubismField_460 != argument0)
			{
				if (CubismField_461 != null)
				{
					UnityEngine.Debug.LogWarning("Live2DModelUnity#setRenderMode() must be called first.");
					_3000_0020_0020();
				}
				switch (argument0)
				{
				case 0:
					CubismField_460 = argument0;
					break;
				case 1:
					CubismField_460 = argument0;
					break;
				default:
					UtDebug.print("Invalid render mode");
					break;
				}
			}
		}

		public int getRenderMode()
		{
			return CubismField_460;
		}

		public void setTextureColor(int argument0, float argument1, float argument2, float argument3)
		{
			_0020().setTextureColor(argument0, argument1, argument2, argument3, 1f);
		}

		public void setConvertTexture(int argument0, Texture2D argument1)
		{
			_0020()._3000_3000_0020_0020(argument0, argument1);
		}

		public string toStringDrawCommand()
		{
			return _0020()._3000_3000_3000_3000();
		}

		internal void _3000_0020_3000(int argument0)
		{
			_0020().CubismField_75 = argument0;
		}
	}
	public abstract class AMotion
	{
		protected int CubismField_466;

		protected int CubismField_467;

		protected float CubismField_468;

		public AMotion()
		{
			CubismField_466 = 1000;
			CubismField_467 = 1000;
			CubismField_468 = 1f;
			_0020();
		}

		internal void _0020()
		{
		}

		public void setFadeIn(int argument0)
		{
			CubismField_466 = argument0;
		}

		public void setFadeOut(int argument0)
		{
			CubismField_467 = argument0;
		}

		public void setWeight(float argument0)
		{
			CubismField_468 = argument0;
		}

		public int getFadeOut()
		{
			return CubismField_467;
		}

		public int getFadeIn()
		{
			return CubismField_466;
		}

		public float getWeight()
		{
			return CubismField_468;
		}

		public virtual int getDurationMSec()
		{
			return -1;
		}

		public virtual int getLoopDurationMSec()
		{
			return -1;
		}

		public static float getEasing(float argument0, float argument1, float argument2)
		{
			float num = argument0 / argument1;
			float num2 = argument2 / argument1;
			float num3 = num2;
			float num4 = 1f / 3f;
			float num5 = 2f / 3f;
			float num6 = 1f - (1f - num2) * (1f - num2);
			float num7 = 1f - (1f - num3) * (1f - num3);
			float num8 = 0f;
			float num9 = (1f - num2) * num4 * num6 + (num3 * num5 + (1f - num3) * num4) * (1f - num6);
			float num10 = (num3 + (1f - num3) * num5) * num7 + (num2 * num4 + (1f - num2) * num5) * (1f - num7);
			float num11 = 1f - 3f * num10 + 3f * num9 - num8;
			float num12 = 3f * num10 - 6f * num9 + 3f * num8;
			float num13 = 3f * num9 - 3f * num8;
			float num14 = num8;
			if (num <= 0f)
			{
				return 0f;
			}
			if (num >= 1f)
			{
				return 1f;
			}
			float num15 = num;
			float num16 = num15 * num15;
			float num17 = num15 * num16;
			return num11 * num17 + num12 * num16 + num13 * num15 + num14;
		}

		public void updateParam(ALive2DModel argument0, MotionQueueEnt argument1)
		{
			if (!argument1.CubismField_470 || argument1.CubismField_471)
			{
				return;
			}
			long userTimeMSec = UtSystem.getUserTimeMSec();
			if (argument1.CubismField_472 < 0)
			{
				argument1.CubismField_472 = userTimeMSec;
				argument1.CubismField_473 = userTimeMSec;
				int durationMSec = getDurationMSec();
				if (argument1.CubismField_474 < 0)
				{
					argument1.CubismField_474 = ((durationMSec <= 0) ? (-1) : (argument1.CubismField_472 + durationMSec));
				}
			}
			float cubismField_ = CubismField_468;
			float num = ((CubismField_466 == 0) ? 1f : CubismType_75._0020((float)(userTimeMSec - argument1.CubismField_473) / (float)CubismField_466));
			float num2 = ((CubismField_467 == 0 || argument1.CubismField_474 < 0) ? 1f : CubismType_75._0020((float)(argument1.CubismField_474 - userTimeMSec) / (float)CubismField_467));
			cubismField_ = cubismField_ * num * num2;
			updateParamExe(argument0, userTimeMSec, cubismField_, argument1);
			if (argument1.CubismField_474 > 0 && argument1.CubismField_474 < userTimeMSec)
			{
				argument1.CubismField_471 = true;
			}
		}

		public abstract void updateParamExe(ALive2DModel argument0, long argument1, float argument2, MotionQueueEnt argument3);
	}
	public class MotionQueueEnt
	{
		internal AMotion CubismField_469;

		internal bool CubismField_470 = true;

		internal bool CubismField_471;

		internal long CubismField_472 = -1L;

		internal long CubismField_473 = -1L;

		internal long CubismField_474 = -1L;

		internal static int CubismField_475;

		internal int CubismField_476;

		internal MotionQueueEnt()
		{
			CubismField_476 = CubismField_475++;
		}

		internal bool _3000()
		{
			return CubismField_471;
		}

		internal void _3000_0020(long argument0)
		{
			long num = UtSystem.getUserTimeMSec() + argument0;
			if (CubismField_474 < 0 || num < CubismField_474)
			{
				CubismField_474 = num;
			}
		}

		internal int _0020()
		{
			return CubismField_476;
		}
	}
	public class UtSystem
	{
		private static Stopwatch CubismField_477 = null;

		public const long USER_TIME_AUTO = -1L;

		private static long CubismField_479 = -1L;

		public static bool isBigEndian()
		{
			return !BitConverter.IsLittleEndian;
		}

		public static void sleepMSec(long argument0)
		{
			Thread.Sleep(new TimeSpan(0, 0, (int)(argument0 / 1000)));
		}

		public static long getUserTimeMSec()
		{
			if (CubismField_479 != -1)
			{
				return CubismField_479;
			}
			return getSystemTimeMSec();
		}

		public static void setUserTimeMSec(long argument0)
		{
			CubismField_479 = argument0;
		}

		public static long updateUserTimeMSec()
		{
			return CubismField_479 = getSystemTimeMSec();
		}

		public static long getTimeMSec()
		{
			if (CubismField_477 == null)
			{
				CubismField_477 = new Stopwatch();
				CubismField_477.Start();
			}
			return CubismField_477.ElapsedMilliseconds;
		}

		public static long getSystemTimeMSec()
		{
			return getTimeMSec();
		}

		public static void resetUserTimeMSec()
		{
			CubismField_479 = -1L;
		}

		public static void exit(int argument0)
		{
			Environment.Exit(argument0);
		}
	}
	internal class CubismType_75
	{
		internal static float _0020(float argument0)
		{
			if (argument0 < 0f)
			{
				return 0f;
			}
			if (argument0 > 1f)
			{
				return 1f;
			}
			return (float)(0.5 - 0.5 * Math.Cos(argument0 * UtMath.PI_F));
		}
	}
	public class EyeBlinkMotion
	{
		private enum CubismType_77
		{
			STATE_FIRST,
			STATE_INTERVAL,
			STATE_CLOSING,
			STATE_CLOSED,
			STATE_OPENING
		}

		private long CubismField_480;

		private long CubismField_481;

		private CubismType_77 CubismField_482;

		private bool CubismField_483;

		private string CubismField_484;

		private string CubismField_485;

		private int CubismField_486;

		private int CubismField_487;

		private int CubismField_488;

		private int CubismField_489;

		public EyeBlinkMotion()
		{
			CubismField_482 = CubismType_77.STATE_FIRST;
			CubismField_486 = 4000;
			CubismField_487 = 100;
			CubismField_488 = 50;
			CubismField_489 = 150;
			CubismField_483 = true;
			CubismField_484 = "PARAM_EYE_L_OPEN";
			CubismField_485 = "PARAM_EYE_R_OPEN";
		}

		public long calcNextBlink()
		{
			long userTimeMSec = UtSystem.getUserTimeMSec();
			double num = new System.Random().NextDouble();
			return (long)((double)userTimeMSec + num * (double)(2 * CubismField_486 - 1));
		}

		public void setInterval(int argument0)
		{
			CubismField_486 = argument0;
		}

		public void setEyeMotion(int argument0, int argument1, int argument2)
		{
			CubismField_487 = argument0;
			CubismField_488 = argument1;
			CubismField_489 = argument2;
		}

		public void setParam(ALive2DModel argument0)
		{
			long userTimeMSec = UtSystem.getUserTimeMSec();
			float num = 0f;
			float num2;
			switch (CubismField_482)
			{
			case CubismType_77.STATE_CLOSING:
				num = (float)(userTimeMSec - CubismField_481) / (float)CubismField_487;
				if (num >= 1f)
				{
					num = 1f;
					CubismField_482 = CubismType_77.STATE_CLOSED;
					CubismField_481 = userTimeMSec;
				}
				num2 = 1f - num;
				break;
			case CubismType_77.STATE_CLOSED:
				num = (float)(userTimeMSec - CubismField_481) / (float)CubismField_488;
				if (num >= 1f)
				{
					CubismField_482 = CubismType_77.STATE_OPENING;
					CubismField_481 = userTimeMSec;
				}
				num2 = 0f;
				break;
			case CubismType_77.STATE_OPENING:
				num = (float)(userTimeMSec - CubismField_481) / (float)CubismField_489;
				if (num >= 1f)
				{
					num = 1f;
					CubismField_482 = CubismType_77.STATE_INTERVAL;
					CubismField_480 = calcNextBlink();
				}
				num2 = num;
				break;
			case CubismType_77.STATE_INTERVAL:
				if (CubismField_480 < userTimeMSec)
				{
					CubismField_482 = CubismType_77.STATE_CLOSING;
					CubismField_481 = userTimeMSec;
				}
				num2 = 1f;
				break;
			default:
				CubismField_482 = CubismType_77.STATE_INTERVAL;
				CubismField_480 = calcNextBlink();
				num2 = 1f;
				break;
			}
			if (!CubismField_483)
			{
				num2 = 0f - num2;
			}
			argument0.setParamFloat(CubismField_484, num2);
			argument0.setParamFloat(CubismField_485, num2);
		}
	}
	public class Live2DMotion : AMotion
	{
		internal class CubismType_79
		{
			internal const int CubismField_509 = 0;

			internal const int CubismField_510 = 1;

			internal const int CubismField_511 = 2;

			internal const int CubismField_512 = 3;

			internal const int CubismField_513 = 100;

			internal const int CubismField_514 = 101;

			internal const int CubismField_515 = 102;

			internal const int CubismField_516 = 103;

			internal const int CubismField_517 = 104;

			internal const int CubismField_518 = 105;

			internal string CubismField_519;

			internal float[] CubismField_520;

			internal int CubismField_521;

			public int fadeInMsec = -1;

			public int fadeOutMsec = -1;

			internal CubismType_79()
			{
			}
		}

		private const string CubismField_496 = "VISIBLE:";

		private const string CubismField_497 = "LAYOUT:";

		private List<CubismType_79> CubismField_498 = new List<CubismType_79>();

		private float CubismField_499;

		private int CubismField_500;

		private int CubismField_501;

		private bool CubismField_502;

		private bool CubismField_503 = true;

		private int CubismField_504;

		private float CubismField_505;

		private static int CubismField_506 = 0;

		private static List<float> CubismField_507 = new List<float>();

		private const int CubismField_508 = 1;

		public Live2DMotion()
		{
			CubismField_504 = CubismField_506++;
			CubismField_499 = 30f;
			CubismField_500 = 0;
			CubismField_502 = false;
			CubismField_501 = -1;
			CubismField_505 = 0f;
		}

		public static Live2DMotion loadMotion(string argument0)
		{
			try
			{
				return loadMotion(UtFile.loadFile(argument0));
			}
			catch (Exception)
			{
				UtDebug.print("Failed to load motion : " + argument0);
			}
			return null;
		}

		public static Live2DMotion loadMotion(Stream argument0)
		{
			return loadMotion(UtFile.load(argument0));
		}

		public static Live2DMotion loadMotion(byte[] argument0)
		{
			Live2DMotion live2DMotion = new Live2DMotion();
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			int[] array = new int[1];
			int num = argument0.Length;
			live2DMotion.CubismField_500 = 0;
			new List<CubismType_79>();
			for (int i = 0; i < num; i++)
			{
				char c = (char)(argument0[i] & 0xFFu);
				switch (c)
				{
				case '#':
					for (; i < num && argument0[i] != 10 && argument0[i] != 13; i++)
					{
					}
					continue;
				case '$':
				{
					int num2 = i;
					int num3 = -1;
					int num4 = -1;
					for (; i < num; i++)
					{
						switch ((ushort)(argument0[i] & 0xFF))
						{
						case 58:
							num4 = i + 1;
							continue;
						case 61:
							num3 = i;
							break;
						default:
							continue;
						case 10:
						case 13:
							break;
						}
						break;
					}
					if (num3 >= 0)
					{
						float num5 = -1f;
						for (i = num3 + 1; i < num; i++)
						{
							switch ((ushort)(argument0[i] & 0xFF))
							{
							default:
							{
								float num6 = (float)CubismType_80._3000_3000(argument0, num, i, array);
								if (array[0] > 0)
								{
									num5 = num6;
								}
								i = array[0] - 1;
								continue;
							}
							case 9:
							case 32:
							case 44:
								continue;
							case 10:
							case 13:
								break;
							}
							break;
						}
						if (num3 == num2 + 4 && argument0[num2 + 1] == 102 && argument0[num2 + 2] == 112 && argument0[num2 + 3] == 115 && 5f < num5 && num5 < 121f)
						{
							live2DMotion.CubismField_499 = num5;
						}
						if (argument0[num2 + 1] == 102 && argument0[num2 + 2] == 97 && argument0[num2 + 3] == 100 && argument0[num2 + 4] == 101 && argument0[num2 + 5] == 105 && argument0[num2 + 6] == 110)
						{
							if (num3 == num2 + 7)
							{
								if (0f <= num5)
								{
									live2DMotion.CubismField_466 = (int)num5;
								}
							}
							else if (num4 == num2 + 8)
							{
								dictionary.Add(Encoding.UTF8.GetString(argument0, num4, num3 - num4), (int)num5);
							}
						}
						if (argument0[num2 + 1] == 102 && argument0[num2 + 2] == 97 && argument0[num2 + 3] == 100 && argument0[num2 + 4] == 101 && argument0[num2 + 5] == 111 && argument0[num2 + 6] == 117 && argument0[num2 + 7] == 116)
						{
							if (num3 == num2 + 8)
							{
								if (0f <= num5)
								{
									live2DMotion.CubismField_467 = (int)num5;
								}
							}
							else if (num4 == num2 + 9)
							{
								dictionary2.Add(Encoding.UTF8.GetString(argument0, num4, num3 - num4), (int)num5);
							}
						}
					}
					for (; i < num && argument0[i] != 10 && argument0[i] != 13; i++)
					{
					}
					continue;
				}
				case '\n':
				case '\r':
					continue;
				}
				if (('a' > c || c > 'z') && ('A' > c || c > 'Z') && c != '_')
				{
					continue;
				}
				int num7 = i;
				int num8 = -1;
				for (; i < num; i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					case 61:
						num8 = i;
						break;
					default:
						continue;
					case 10:
					case 13:
						break;
					}
					break;
				}
				if (num8 < 0)
				{
					continue;
				}
				CubismType_79 cubismType_ = new CubismType_79();
				if (CubismType_80._3000_0020(argument0, num7, "VISIBLE:"))
				{
					cubismType_.CubismField_521 = 1;
					cubismType_.CubismField_519 = Encoding.UTF8.GetString(argument0, num7, num8 - num7);
				}
				else if (CubismType_80._3000_0020(argument0, num7, "LAYOUT:"))
				{
					cubismType_.CubismField_519 = Encoding.UTF8.GetString(argument0, num7 + 7, num8 - num7 - 7);
					if (CubismType_80._3000_0020(argument0, num7 + 7, "ANCHOR_X"))
					{
						cubismType_.CubismField_521 = 102;
					}
					else if (CubismType_80._3000_0020(argument0, num7 + 7, "ANCHOR_Y"))
					{
						cubismType_.CubismField_521 = 103;
					}
					else if (CubismType_80._3000_0020(argument0, num7 + 7, "SCALE_X"))
					{
						cubismType_.CubismField_521 = 104;
					}
					else if (CubismType_80._3000_0020(argument0, num7 + 7, "SCALE_Y"))
					{
						cubismType_.CubismField_521 = 105;
					}
					else if (CubismType_80._3000_0020(argument0, num7 + 7, "X"))
					{
						cubismType_.CubismField_521 = 100;
					}
					else if (CubismType_80._3000_0020(argument0, num7 + 7, "Y"))
					{
						cubismType_.CubismField_521 = 101;
					}
				}
				else
				{
					cubismType_.CubismField_521 = 0;
					cubismType_.CubismField_519 = Encoding.UTF8.GetString(argument0, num7, num8 - num7);
				}
				live2DMotion.CubismField_498.Add(cubismType_);
				int num9 = 0;
				CubismField_507.Clear();
				for (i = num8 + 1; i < num; i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					default:
					{
						float item = (float)CubismType_80._3000_3000(argument0, num, i, array);
						if (array[0] <= 0)
						{
							continue;
						}
						CubismField_507.Add(item);
						num9++;
						int num10 = array[0];
						if (num10 < i)
						{
							Console.Write("Illegal state . @Live2DMotion loadMotion()\n");
							break;
						}
						i = num10 - 1;
						continue;
					}
					case 9:
					case 32:
					case 44:
						continue;
					case 10:
					case 13:
						break;
					}
					break;
				}
				cubismType_.CubismField_520 = CubismField_507.ToArray();
				if (num9 > live2DMotion.CubismField_500)
				{
					live2DMotion.CubismField_500 = num9;
				}
			}
			live2DMotion.CubismField_501 = (int)((float)(1000 * live2DMotion.CubismField_500) / live2DMotion.CubismField_499);
			foreach (KeyValuePair<string, int> item2 in dictionary)
			{
				live2DMotion._3000_0020(item2.Key, item2.Value);
			}
			foreach (KeyValuePair<string, int> item3 in dictionary2)
			{
				live2DMotion._3000_3000(item3.Key, item3.Value);
			}
			return live2DMotion;
		}

		public override int getDurationMSec()
		{
			if (!CubismField_502)
			{
				return CubismField_501;
			}
			return -1;
		}

		public override int getLoopDurationMSec()
		{
			return CubismField_501;
		}

		public override void updateParamExe(ALive2DModel argument0, long argument1, float argument2, MotionQueueEnt argument3)
		{
			float num = (float)(argument1 - argument3.CubismField_472) * CubismField_499 / 1000f;
			int num2 = (int)num;
			float num3 = num - (float)num2;
			float num4 = ((CubismField_466 == 0) ? 1f : CubismType_75._0020((float)(argument1 - argument3.CubismField_473) / (float)CubismField_466));
			float num5 = ((CubismField_467 == 0 || argument3.CubismField_474 < 0) ? 1f : CubismType_75._0020((float)(argument3.CubismField_474 - argument1) / (float)CubismField_467));
			for (int i = 0; i < CubismField_498.Count; i++)
			{
				CubismType_79 cubismType_ = CubismField_498[i];
				int num6 = cubismType_.CubismField_520.Length;
				string cubismField_ = cubismType_.CubismField_519;
				if (cubismType_.CubismField_521 == 1)
				{
					float argument4 = cubismType_.CubismField_520[(num2 >= num6) ? (num6 - 1) : num2];
					argument0.setParamFloat(cubismField_, argument4);
				}
				else if (100 > cubismType_.CubismField_521 || cubismType_.CubismField_521 > 105)
				{
					int paramIndex = argument0.getParamIndex(cubismField_);
					ModelContext modelContext = argument0.getModelContext();
					float paramMax = modelContext.getParamMax(paramIndex);
					float paramMin = modelContext.getParamMin(paramIndex);
					float num7 = 0.4f * (paramMax - paramMin);
					float paramFloat = modelContext.getParamFloat(paramIndex);
					float num8 = cubismType_.CubismField_520[(num2 >= num6) ? (num6 - 1) : num2];
					float num9 = cubismType_.CubismField_520[(num2 + 1 >= num6) ? (num6 - 1) : (num2 + 1)];
					float num10 = (((!(num8 < num9) || !(num9 - num8 > num7)) && (!(num8 > num9) || !(num8 - num9 > num7))) ? (num8 + (num9 - num8) * num3) : num8);
					float argument5;
					if (cubismType_.fadeInMsec < 0 && cubismType_.fadeOutMsec < 0)
					{
						argument5 = paramFloat + (num10 - paramFloat) * argument2;
					}
					else
					{
						float num11 = ((cubismType_.fadeInMsec >= 0) ? ((cubismType_.fadeInMsec == 0) ? 1f : CubismType_75._0020((float)(argument1 - argument3.CubismField_473) / (float)cubismType_.fadeInMsec)) : num4);
						float num12 = ((cubismType_.fadeOutMsec >= 0) ? ((cubismType_.fadeOutMsec == 0 || argument3.CubismField_474 < 0) ? 1f : CubismType_75._0020((float)(argument3.CubismField_474 - argument1) / (float)cubismType_.fadeOutMsec)) : num5);
						float num13 = CubismField_468 * num11 * num12;
						argument5 = paramFloat + (num10 - paramFloat) * num13;
					}
					argument0.setParamFloat(cubismField_, argument5);
				}
			}
			if (num2 >= CubismField_500)
			{
				if (CubismField_502)
				{
					argument3.CubismField_472 = argument1;
					if (CubismField_503)
					{
						argument3.CubismField_473 = argument1;
					}
				}
				else
				{
					argument3.CubismField_471 = true;
				}
			}
			CubismField_505 = CubismField_468;
		}

		public bool isLoop()
		{
			return CubismField_502;
		}

		public void setLoop(bool argument0)
		{
			CubismField_502 = argument0;
		}

		public void setLoopFadeIn(bool argument0)
		{
			CubismField_503 = argument0;
		}

		public bool isLoopFadeIn()
		{
			return CubismField_503;
		}

		public float getFPS()
		{
			return CubismField_499;
		}

		public void setFPS(float argument0)
		{
			CubismField_499 = argument0;
		}

		private void _3000_0020(string argument0, int argument1)
		{
			for (int i = 0; i < CubismField_498.Count; i++)
			{
				CubismType_79 cubismType_ = CubismField_498[i];
				if (argument0 == cubismType_.CubismField_519)
				{
					cubismType_.fadeInMsec = argument1;
					break;
				}
			}
		}

		private void _3000_3000(string argument0, int argument1)
		{
			for (int i = 0; i < CubismField_498.Count; i++)
			{
				CubismType_79 cubismType_ = CubismField_498[i];
				if (argument0 == cubismType_.CubismField_519)
				{
					cubismType_.fadeOutMsec = argument1;
					break;
				}
			}
		}

		private float _0020(string argument0)
		{
			for (int i = 0; i < CubismField_498.Count; i++)
			{
				CubismType_79 cubismType_ = CubismField_498[i];
				if (argument0 == cubismType_.CubismField_519)
				{
					return cubismType_.fadeInMsec;
				}
			}
			return -1f;
		}

		private float _3000(string argument0)
		{
			for (int i = 0; i < CubismField_498.Count; i++)
			{
				CubismType_79 cubismType_ = CubismField_498[i];
				if (argument0 == cubismType_.CubismField_519)
				{
					return cubismType_.fadeOutMsec;
				}
			}
			return -1f;
		}
	}
	internal class CubismType_80
	{
		internal static bool _3000_0020(byte[] argument0, int argument1, string argument2)
		{
			int num = argument1 + argument2.Length;
			if (num >= argument0.Length)
			{
				return false;
			}
			for (int i = argument1; i < num; i++)
			{
				if (argument0[i] != argument2.Substring(i - argument1, 1).ToCharArray()[0])
				{
					return false;
				}
			}
			return true;
		}

		internal static byte[] _0020(string argument0)
		{
			return Encoding.ASCII.GetBytes(argument0 + "\0");
		}

		internal static byte[] _3000(string argument0)
		{
			return Encoding.ASCII.GetBytes(argument0 + "\n\0");
		}

		internal static double _3000_3000(byte[] argument0, int argument1, int argument2, int[] argument3)
		{
			int i = argument2;
			bool flag = false;
			bool flag2 = false;
			double num = 0.0;
			char c = (char)(argument0[i] & 0xFFu);
			if (c == '-')
			{
				flag = true;
				i++;
			}
			for (; i < argument1; i++)
			{
				switch ((ushort)(argument0[i] & 0xFF))
				{
				case 48:
					num *= 10.0;
					continue;
				case 49:
					num = num * 10.0 + 1.0;
					continue;
				case 50:
					num = num * 10.0 + 2.0;
					continue;
				case 51:
					num = num * 10.0 + 3.0;
					continue;
				case 52:
					num = num * 10.0 + 4.0;
					continue;
				case 53:
					num = num * 10.0 + 5.0;
					continue;
				case 54:
					num = num * 10.0 + 6.0;
					continue;
				case 55:
					num = num * 10.0 + 7.0;
					continue;
				case 56:
					num = num * 10.0 + 8.0;
					continue;
				case 57:
					num = num * 10.0 + 9.0;
					continue;
				case 46:
					flag2 = true;
					i++;
					break;
				}
				break;
			}
			if (flag2)
			{
				for (double num2 = 0.1; i < argument1; num2 *= 0.1, i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					case 49:
						num += num2 * 1.0;
						continue;
					case 50:
						num += num2 * 2.0;
						continue;
					case 51:
						num += num2 * 3.0;
						continue;
					case 52:
						num += num2 * 4.0;
						continue;
					case 53:
						num += num2 * 5.0;
						continue;
					case 54:
						num += num2 * 6.0;
						continue;
					case 55:
						num += num2 * 7.0;
						continue;
					case 56:
						num += num2 * 8.0;
						continue;
					case 57:
						num += num2 * 9.0;
						continue;
					case 48:
						continue;
					}
					break;
				}
			}
			if (flag)
			{
				num = 0.0 - num;
			}
			argument3[0] = i;
			return num;
		}
	}
	public class MotionQueueManager
	{
		private List<MotionQueueEnt> CubismField_524;

		private bool CubismField_525;

		public MotionQueueManager()
		{
			CubismField_524 = new List<MotionQueueEnt>();
		}

		internal List<MotionQueueEnt> _0020()
		{
			return CubismField_524;
		}

		public int startMotion(AMotion argument0, bool argument1 = false)
		{
			MotionQueueEnt motionQueueEnt = null;
			int count = CubismField_524.Count;
			for (int i = 0; i < count; i++)
			{
				motionQueueEnt = CubismField_524[i];
				if (motionQueueEnt != null)
				{
					motionQueueEnt._3000_0020(motionQueueEnt.CubismField_469.getFadeOut());
					if (CubismField_525)
					{
						UtDebug.println("MotionQueueManager[size:{0}]->startMotion() / start fade out (m{1})", count, motionQueueEnt.CubismField_476);
					}
				}
			}
			if (argument0 == null)
			{
				return -1;
			}
			motionQueueEnt = new MotionQueueEnt();
			motionQueueEnt.CubismField_469 = argument0;
			CubismField_524.Add(motionQueueEnt);
			int cubismField_ = motionQueueEnt.CubismField_476;
			if (CubismField_525)
			{
				UtDebug.println("MotionQueueManager[size:{0}]->startMotion() / new motion (m{1})", count, cubismField_);
			}
			return cubismField_;
		}

		public virtual bool updateParam(ALive2DModel argument0)
		{
			try
			{
				bool result = false;
				for (int i = 0; i < CubismField_524.Count; i++)
				{
					MotionQueueEnt motionQueueEnt = CubismField_524[i];
					if (motionQueueEnt == null)
					{
						CubismField_524.Remove(motionQueueEnt);
						i--;
						continue;
					}
					AMotion cubismField_ = motionQueueEnt.CubismField_469;
					if (cubismField_ == null)
					{
						CubismField_524.Remove(motionQueueEnt);
						i--;
						continue;
					}
					cubismField_.updateParam(argument0, motionQueueEnt);
					result = true;
					if (motionQueueEnt._3000())
					{
						if (CubismField_525)
						{
							UtDebug.println("MotionQueueManager[size:{0}]->updateParam() / finish motion (m{1})\n", CubismField_524.Count - 1, motionQueueEnt.CubismField_476);
						}
						CubismField_524.Remove(motionQueueEnt);
						i--;
					}
				}
				return result;
			}
			catch (Exception ex)
			{
				UtDebug.println(ex.Message);
				return true;
			}
		}

		public bool isFinished()
		{
			for (int i = 0; i < CubismField_524.Count; i++)
			{
				MotionQueueEnt motionQueueEnt = CubismField_524[i];
				if (motionQueueEnt == null)
				{
					CubismField_524.Remove(motionQueueEnt);
					i--;
				}
				else if (motionQueueEnt.CubismField_469 == null)
				{
					CubismField_524.Remove(motionQueueEnt);
					i--;
				}
				else if (!motionQueueEnt._3000())
				{
					return false;
				}
			}
			return true;
		}

		public bool isFinished(int argument0)
		{
			IEnumerator<MotionQueueEnt> enumerator = CubismField_524.GetEnumerator();
			while (enumerator.MoveNext())
			{
				MotionQueueEnt current = enumerator.Current;
				if (current != null && current.CubismField_476 == argument0 && !current._3000())
				{
					return false;
				}
			}
			return true;
		}

		public void stopAllMotions()
		{
			int num;
			for (num = 0; num < CubismField_524.Count; num++)
			{
				MotionQueueEnt motionQueueEnt = CubismField_524[num];
				if (motionQueueEnt == null)
				{
					CubismField_524.Remove(motionQueueEnt);
					num--;
				}
				else if (motionQueueEnt.CubismField_469 == null)
				{
					CubismField_524.Remove(motionQueueEnt);
					num--;
				}
				else
				{
					CubismField_524.Remove(motionQueueEnt);
					num--;
				}
			}
		}

		public void setMotionDebugMode(bool argument0)
		{
			CubismField_525 = argument0;
		}
	}
	public class PhysicsHair
	{
		public enum Src
		{
			SRC_TO_X,
			SRC_TO_Y,
			SRC_TO_G_ANGLE
		}

		public enum Target
		{
			TARGET_FROM_ANGLE,
			TARGET_FROM_ANGLE_V
		}

		internal class CubismType_85
		{
			internal float CubismField_545 = 1f;

			internal float CubismField_546;

			internal float CubismField_547;

			internal float CubismField_548;

			internal float CubismField_549;

			internal float CubismField_550;

			internal float CubismField_551;

			internal float CubismField_552;

			internal float CubismField_553;

			internal float CubismField_554;

			internal float CubismField_555;

			internal float CubismField_556;

			internal float CubismField_557;

			internal CubismType_85()
			{
			}

			internal void _0020()
			{
				CubismField_554 = CubismField_546;
				CubismField_555 = CubismField_547;
				CubismField_556 = CubismField_548;
				CubismField_557 = CubismField_549;
			}
		}

		internal class CubismType_86
		{
			internal string CubismField_558;

			internal float CubismField_559;

			internal float CubismField_560;

			internal CubismType_86(string argument0, float argument1, float argument2)
			{
				CubismField_558 = argument0;
				CubismField_559 = argument1;
				CubismField_560 = argument2;
			}

			internal virtual void _0020(ALive2DModel argument0, PhysicsHair argument1)
			{
			}
		}

		internal class CubismType_87 : CubismType_86
		{
			private Src CubismField_561;

			internal CubismType_87(Src argument0, string argument1, float argument2, float argument3)
				: base(argument1, argument2, argument3)
			{
				CubismField_561 = argument0;
			}

			internal override void _0020(ALive2DModel argument0, PhysicsHair argument1)
			{
				float num = CubismField_559 * argument0.getParamFloat(CubismField_558);
				CubismType_85 cubismType_ = argument1._3000_0020_0020();
				switch (CubismField_561)
				{
				default:
					cubismType_.CubismField_546 += (num - cubismType_.CubismField_546) * CubismField_560;
					break;
				case Src.SRC_TO_Y:
					cubismType_.CubismField_547 += (num - cubismType_.CubismField_547) * CubismField_560;
					break;
				case Src.SRC_TO_G_ANGLE:
				{
					float num2 = argument1._3000_3000();
					num2 += (num - num2) * CubismField_560;
					argument1._3000_3000_0020(num2);
					break;
				}
				}
			}
		}

		internal class CubismType_88
		{
			internal string CubismField_562;

			internal float CubismField_563;

			internal float CubismField_564;

			internal CubismType_88(string argument0, float argument1, float argument2)
			{
				CubismField_562 = argument0;
				CubismField_563 = argument1;
				CubismField_564 = argument2;
			}

			internal virtual void _0020(ALive2DModel argument0, PhysicsHair argument1)
			{
			}
		}

		internal class CubismType_89 : CubismType_88
		{
			internal Target CubismField_565;

			internal CubismType_89(Target argument0, string argument1, float argument2, float argument3)
				: base(argument1, argument2, argument3)
			{
				CubismField_565 = argument0;
			}

			internal override void _0020(ALive2DModel argument0, PhysicsHair argument1)
			{
				Target cubismField_ = CubismField_565;
				if (cubismField_ == Target.TARGET_FROM_ANGLE || cubismField_ != Target.TARGET_FROM_ANGLE_V)
				{
					argument0.setParamFloat(CubismField_562, CubismField_563 * argument1._3000(), CubismField_564);
				}
				else
				{
					argument0.setParamFloat(CubismField_562, CubismField_563 * argument1._3000_0020(), CubismField_564);
				}
			}
		}

		private CubismType_85 CubismField_526 = new CubismType_85();

		private CubismType_85 CubismField_527 = new CubismType_85();

		private float CubismField_528;

		private float CubismField_529;

		private float CubismField_530;

		private float CubismField_531;

		private float CubismField_532;

		private float CubismField_533;

		private long CubismField_534;

		private long CubismField_535;

		private List<CubismType_86> CubismField_536 = new List<CubismType_86>();

		private List<CubismType_88> CubismField_537 = new List<CubismType_88>();

		public PhysicsHair()
		{
			setup(0.3f, 0.5f, 0.1f);
		}

		public PhysicsHair(float argument0, float argument1, float argument2)
		{
			setup(argument0, argument1, argument2);
		}

		public void setup(float argument0, float argument1, float argument2)
		{
			CubismField_528 = argument0;
			CubismField_530 = argument1;
			CubismField_526.CubismField_545 = argument2;
			CubismField_527.CubismField_545 = argument2;
			CubismField_527.CubismField_547 = argument0;
			setup();
		}

		internal CubismType_85 _3000_0020_0020()
		{
			return CubismField_526;
		}

		internal CubismType_85 _3000_0020_3000()
		{
			return CubismField_527;
		}

		internal float _3000_3000()
		{
			return CubismField_529;
		}

		internal void _3000_3000_0020(float argument0)
		{
			CubismField_529 = argument0;
		}

		internal float _3000()
		{
			return CubismField_531;
		}

		internal float _3000_0020()
		{
			return CubismField_533;
		}

		internal float _0020()
		{
			return (float)(-180.0 * Math.Atan2(CubismField_526.CubismField_546 - CubismField_527.CubismField_546, 0f - (CubismField_526.CubismField_547 - CubismField_527.CubismField_547)) / Math.PI);
		}

		public void setup()
		{
			CubismField_532 = _0020();
			CubismField_527._0020();
		}

		public void addSrcParam(Src argument0, string argument1, float argument2, float argument3)
		{
			CubismType_87 item = new CubismType_87(argument0, argument1, argument2, argument3);
			CubismField_536.Add(item);
		}

		public void addTargetParam(Target argument0, string argument1, float argument2, float argument3)
		{
			CubismType_89 item = new CubismType_89(argument0, argument1, argument2, argument3);
			CubismField_537.Add(item);
		}

		public void update(ALive2DModel argument0, long argument1)
		{
			if (CubismField_534 == 0L)
			{
				CubismField_534 = (CubismField_535 = argument1);
				CubismField_528 = (float)Math.Sqrt((CubismField_526.CubismField_546 - CubismField_527.CubismField_546) * (CubismField_526.CubismField_546 - CubismField_527.CubismField_546) + (CubismField_526.CubismField_547 - CubismField_527.CubismField_547) * (CubismField_526.CubismField_547 - CubismField_527.CubismField_547));
				return;
			}
			float num = (float)(argument1 - CubismField_535) / 1000f;
			if (num != 0f)
			{
				for (int num2 = CubismField_536.Count - 1; num2 >= 0; num2--)
				{
					CubismField_536[num2]._0020(argument0, this);
				}
				_3000_3000_3000(argument0, num);
				CubismField_531 = _0020();
				CubismField_533 = (CubismField_531 - CubismField_532) / num;
				CubismField_532 = CubismField_531;
			}
			for (int num3 = CubismField_537.Count - 1; num3 >= 0; num3--)
			{
				CubismField_537[num3]._0020(argument0, this);
			}
			CubismField_535 = argument1;
		}

		internal void _3000_3000_3000(ALive2DModel argument0, float argument1)
		{
			if (argument1 > 0.033f)
			{
				argument1 = 0.033f;
			}
			float num = 1f / argument1;
			CubismField_526.CubismField_548 = (CubismField_526.CubismField_546 - CubismField_526.CubismField_554) * num;
			CubismField_526.CubismField_549 = (CubismField_526.CubismField_547 - CubismField_526.CubismField_555) * num;
			CubismField_526.CubismField_550 = (CubismField_526.CubismField_548 - CubismField_526.CubismField_556) * num;
			CubismField_526.CubismField_551 = (CubismField_526.CubismField_549 - CubismField_526.CubismField_557) * num;
			CubismField_526.CubismField_552 = CubismField_526.CubismField_550 * CubismField_526.CubismField_545;
			CubismField_526.CubismField_553 = CubismField_526.CubismField_551 * CubismField_526.CubismField_545;
			CubismField_526._0020();
			float num2 = 0f - (float)Math.Atan2(CubismField_526.CubismField_547 - CubismField_527.CubismField_547, CubismField_526.CubismField_546 - CubismField_527.CubismField_546);
			float num3 = (float)Math.Cos(num2);
			float num4 = (float)Math.Sin(num2);
			float num5 = 9.8f * CubismField_527.CubismField_545;
			float num6 = CubismField_529 * UtMath.DEG_TO_RAD_F;
			float num7 = (float)((double)num5 * Math.Cos(num2 - num6));
			float num8 = num7 * num4;
			float num9 = num7 * num3;
			float num10 = (0f - CubismField_526.CubismField_552) * num4 * num4;
			float num11 = (0f - CubismField_526.CubismField_553) * num4 * num3;
			float num12 = (0f - CubismField_527.CubismField_548) * CubismField_530;
			float num13 = (0f - CubismField_527.CubismField_549) * CubismField_530;
			CubismField_527.CubismField_552 = num8 + num10 + num12;
			CubismField_527.CubismField_553 = num9 + num11 + num13;
			CubismField_527.CubismField_550 = CubismField_527.CubismField_552 / CubismField_527.CubismField_545;
			CubismField_527.CubismField_551 = CubismField_527.CubismField_553 / CubismField_527.CubismField_545;
			CubismField_527.CubismField_548 += CubismField_527.CubismField_550 * argument1;
			CubismField_527.CubismField_549 += CubismField_527.CubismField_551 * argument1;
			CubismField_527.CubismField_546 += CubismField_527.CubismField_548 * argument1;
			CubismField_527.CubismField_547 += CubismField_527.CubismField_549 * argument1;
			float num14 = (float)Math.Sqrt((CubismField_526.CubismField_546 - CubismField_527.CubismField_546) * (CubismField_526.CubismField_546 - CubismField_527.CubismField_546) + (CubismField_526.CubismField_547 - CubismField_527.CubismField_547) * (CubismField_526.CubismField_547 - CubismField_527.CubismField_547));
			CubismField_527.CubismField_546 = CubismField_526.CubismField_546 + CubismField_528 * (CubismField_527.CubismField_546 - CubismField_526.CubismField_546) / num14;
			CubismField_527.CubismField_547 = CubismField_526.CubismField_547 + CubismField_528 * (CubismField_527.CubismField_547 - CubismField_526.CubismField_547) / num14;
			CubismField_527.CubismField_548 = (CubismField_527.CubismField_546 - CubismField_527.CubismField_554) * num;
			CubismField_527.CubismField_549 = (CubismField_527.CubismField_547 - CubismField_527.CubismField_555) * num;
			CubismField_527._0020();
		}
	}
	public class Json
	{
		private char[] CubismField_566;

		private int CubismField_567;

		private int CubismField_568;

		private Value CubismField_569;

		public Json(char[] argument0)
		{
			CubismField_566 = argument0;
			CubismField_567 = argument0.Length;
		}

		public Value parse()
		{
			try
			{
				int[] argument = new int[1];
				CubismField_569 = _3000_3000(CubismField_566, CubismField_567, 0, argument);
				return CubismField_569;
			}
			catch (Exception ex)
			{
				throw new Exception("JSON error @line:" + CubismField_568 + " / " + ex.Message, ex);
			}
		}

		public static Value parseFromBytes(char[] argument0)
		{
			return new Json(argument0).parse();
		}

		public static Value parseFromString(string argument0)
		{
			return new Json(argument0.ToCharArray()).parse();
		}

		private static string _3000_0020(char[] argument0, int argument1, int argument2, int[] argument3)
		{
			int i = argument2;
			StringBuilder stringBuilder = null;
			int num = argument2;
			for (; i < argument1; i++)
			{
				switch ((ushort)(argument0[i] & 0xFF))
				{
				case 34:
					argument3[0] = i + 1;
					if (stringBuilder != null)
					{
						if (i - 1 > num)
						{
							stringBuilder.Append(new string(argument0, num, i - 1 - num));
						}
						return stringBuilder.ToString();
					}
					return new string(argument0, argument2, i - argument2);
				case 92:
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder();
					}
					if (i > num)
					{
						stringBuilder.Append(new string(argument0, num, i - num));
					}
					i++;
					if (i < argument1)
					{
						switch ((ushort)(argument0[i] & 0xFF))
						{
						case 92:
							stringBuilder.Append('\\');
							break;
						case 34:
							stringBuilder.Append('"');
							break;
						case 47:
							stringBuilder.Append('/');
							break;
						case 98:
							stringBuilder.Append('\b');
							break;
						case 102:
							stringBuilder.Append('\f');
							break;
						case 110:
							stringBuilder.Append('\n');
							break;
						case 114:
							stringBuilder.Append('\r');
							break;
						case 116:
							stringBuilder.Append('\t');
							break;
						case 117:
							throw new Exception("parse string/unicode escape not supported");
						}
						num = i + 1;
						break;
					}
					throw new Exception("parse string/escape error");
				}
			}
			throw new Exception("parse string/illegal end");
		}

		private Value _3000(char[] argument0, int argument1, int argument2, int[] argument3)
		{
			Dictionary<string, Value> dictionary = new Dictionary<string, Value>();
			string key = null;
			int i = argument2;
			int[] array = new int[1];
			bool flag = false;
			while (i < argument1)
			{
				for (; i < argument1; i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					case 34:
						break;
					case 125:
						argument3[0] = i + 1;
						return new Value(dictionary);
					case 58:
						throw new Exception("illegal ':' position");
					default:
						continue;
					}
					key = _3000_0020(argument0, argument1, i + 1, array);
					i = array[0];
					flag = true;
					break;
				}
				if (!flag)
				{
					throw new Exception("key not found");
				}
				flag = false;
				for (; i < argument1; i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					case 58:
						break;
					case 125:
						throw new Exception("illegal '}' position");
					case 10:
						CubismField_568++;
						continue;
					default:
						continue;
					}
					flag = true;
					i++;
					break;
				}
				if (!flag)
				{
					throw new Exception("':' not found");
				}
				Value value = _3000_3000(argument0, argument1, i, array);
				i = array[0];
				dictionary.Add(key, value);
				for (; i < argument1; i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					case 125:
						argument3[0] = i + 1;
						return new Value(dictionary);
					case 10:
						CubismField_568++;
						continue;
					default:
						continue;
					case 44:
						break;
					}
					break;
				}
				i++;
			}
			throw new Exception("illegal end of parseObject");
		}

		private Value _0020(char[] argument0, int argument1, int argument2, int[] argument3)
		{
			List<Value> list = new List<Value>();
			int num = argument2;
			int[] array = new int[1];
			while (num < argument1)
			{
				Value value = _3000_3000(argument0, argument1, num, array);
				num = array[0];
				if (value != null)
				{
					list.Add(value);
				}
				for (; num < argument1; num++)
				{
					switch ((ushort)(argument0[num] & 0xFF))
					{
					case 93:
						argument3[0] = num + 1;
						return new Value(list);
					case 10:
						CubismField_568++;
						continue;
					default:
						continue;
					case 44:
						break;
					}
					break;
				}
				num++;
			}
			throw new Exception("illegal end of parseObject");
		}

		public static double strToDouble(char[] argument0, int argument1, int argument2, int[] argument3)
		{
			int i = argument2;
			bool flag = false;
			bool flag2 = false;
			double num = 0.0;
			char c = (char)(argument0[i] & 0xFFu);
			if (c == '-')
			{
				flag = true;
				i++;
			}
			for (; i < argument1; i++)
			{
				switch ((ushort)(argument0[i] & 0xFF))
				{
				case 48:
					num *= 10.0;
					continue;
				case 49:
					num = num * 10.0 + 1.0;
					continue;
				case 50:
					num = num * 10.0 + 2.0;
					continue;
				case 51:
					num = num * 10.0 + 3.0;
					continue;
				case 52:
					num = num * 10.0 + 4.0;
					continue;
				case 53:
					num = num * 10.0 + 5.0;
					continue;
				case 54:
					num = num * 10.0 + 6.0;
					continue;
				case 55:
					num = num * 10.0 + 7.0;
					continue;
				case 56:
					num = num * 10.0 + 8.0;
					continue;
				case 57:
					num = num * 10.0 + 9.0;
					continue;
				case 46:
					flag2 = true;
					i++;
					break;
				}
				break;
			}
			if (flag2)
			{
				for (double num2 = 0.1; i < argument1; num2 *= 0.1, i++)
				{
					switch ((ushort)(argument0[i] & 0xFF))
					{
					case 49:
						num += num2 * 1.0;
						continue;
					case 50:
						num += num2 * 2.0;
						continue;
					case 51:
						num += num2 * 3.0;
						continue;
					case 52:
						num += num2 * 4.0;
						continue;
					case 53:
						num += num2 * 5.0;
						continue;
					case 54:
						num += num2 * 6.0;
						continue;
					case 55:
						num += num2 * 7.0;
						continue;
					case 56:
						num += num2 * 8.0;
						continue;
					case 57:
						num += num2 * 9.0;
						continue;
					case 48:
						continue;
					}
					break;
				}
			}
			if (flag)
			{
				num = 0.0 - num;
			}
			argument3[0] = i;
			return num;
		}

		private Value _3000_3000(char[] argument0, int argument1, int argument2, int[] argument3)
		{
			for (int i = argument2; i < argument1; i++)
			{
				switch ((ushort)(argument0[i] & 0xFF))
				{
				case 45:
				case 46:
				case 48:
				case 49:
				case 50:
				case 51:
				case 52:
				case 53:
				case 54:
				case 55:
				case 56:
				case 57:
					return new Value(strToDouble(argument0, argument1, i, argument3));
				case 34:
					return new Value(_3000_0020(argument0, argument1, i + 1, argument3));
				case 91:
					return _0020(argument0, argument1, i + 1, argument3);
				case 93:
					argument3[0] = i;
					return null;
				case 123:
					return _3000(argument0, argument1, i + 1, argument3);
				case 110:
					if (i + 3 < argument1)
					{
						return null;
					}
					throw new Exception("parse null");
				case 116:
					if (i + 3 < argument1)
					{
						return new Value(true);
					}
					throw new Exception("parse true");
				case 102:
					if (i + 4 < argument1)
					{
						return new Value(false);
					}
					throw new Exception("parse false");
				case 44:
					throw new Exception("illegal ',' position");
				case 10:
					CubismField_568++;
					break;
				}
			}
			return null;
		}
	}
	public class Value
	{
		private object CubismField_570;

		public Value(object argument0)
		{
			CubismField_570 = argument0;
		}

		public string toString()
		{
			return toString("");
		}

		public string toString(string argument0)
		{
			if (CubismField_570 is string)
			{
				return (string)CubismField_570;
			}
			if (CubismField_570 is List<Value>)
			{
				string text = argument0 + "[\n";
				foreach (Value item in (List<Value>)CubismField_570)
				{
					text = text + argument0 + "\t" + item.toString(argument0 + "\t") + "\n";
				}
				return text + argument0 + "]\n";
			}
			if (CubismField_570 is Dictionary<string, Value>)
			{
				string text2 = argument0 + "{\n";
				foreach (KeyValuePair<string, Value> item2 in (Dictionary<string, Value>)CubismField_570)
				{
					Value value = item2.Value;
					text2 = text2 + argument0 + "\t" + item2.Key + " : " + value.toString(argument0 + "\t") + "\n";
				}
				return text2 + argument0 + "}\n";
			}
			return string.Concat(CubismField_570);
		}

		public int toInt()
		{
			return toInt(0);
		}

		public int toInt(int argument0)
		{
			if (CubismField_570 is double)
			{
				return (int)(double)CubismField_570;
			}
			return argument0;
		}

		public float toFloat()
		{
			return toFloat(0f);
		}

		public float toFloat(float argument0)
		{
			if (CubismField_570 is double)
			{
				return (float)(double)CubismField_570;
			}
			return argument0;
		}

		public double toDouble()
		{
			return toDouble(0.0);
		}

		public double toDouble(double argument0)
		{
			if (CubismField_570 is double)
			{
				return (double)CubismField_570;
			}
			return argument0;
		}

		public List<Value> getVector(List<Value> argument0)
		{
			if (CubismField_570 is List<Value>)
			{
				return (List<Value>)CubismField_570;
			}
			return argument0;
		}

		public Value get(int argument0)
		{
			if (CubismField_570 is List<Value>)
			{
				return ((List<Value>)CubismField_570)[argument0];
			}
			return null;
		}

		public Dictionary<string, Value> getMap(Dictionary<string, Value> argument0)
		{
			if (CubismField_570 is Dictionary<string, Value>)
			{
				return (Dictionary<string, Value>)CubismField_570;
			}
			return argument0;
		}

		public Value get(string argument0)
		{
			if (CubismField_570 is Dictionary<string, Value>)
			{
				return ((Dictionary<string, Value>)CubismField_570)[argument0];
			}
			return null;
		}

		public List<string> keySet()
		{
			if (CubismField_570 is Dictionary<string, Value>)
			{
				return new List<string>(((Dictionary<string, Value>)CubismField_570).Keys);
			}
			return null;
		}

		public Dictionary<string, Value> toMap()
		{
			if (CubismField_570 is Dictionary<string, Value>)
			{
				return (Dictionary<string, Value>)CubismField_570;
			}
			return null;
		}

		public bool isNull()
		{
			return CubismField_570 == null;
		}

		public bool isBoolean()
		{
			return CubismField_570 is bool;
		}

		public bool isDouble()
		{
			return CubismField_570 is double;
		}

		public bool isstring()
		{
			return CubismField_570 is string;
		}

		public bool isArray()
		{
			return CubismField_570 is List<Value>;
		}

		public bool isMap()
		{
			return CubismField_570 is Dictionary<string, Value>;
		}
	}
	internal class CubismType_92
	{
	}
}

namespace _0020
{
	[DebuggerNonUserCode]
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[CompilerGenerated]
	internal class CubismType_96
	{
		private static ResourceManager CubismField_574;

		private static CultureInfo CubismField_575;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager CubismProperty_1
		{
			get
			{
				if (CubismField_574 == null)
				{
					CubismField_574 = new ResourceManager(" . ", typeof(CubismType_96).Assembly);
				}
				return CubismField_574;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo CubismProperty_2
		{
			get
			{
				return CubismField_575;
			}
			set
			{
				CubismField_575 = value;
			}
		}

		internal static string CubismProperty_3 => CubismProperty_1.GetString("Shader", CubismField_575);

		internal CubismType_96()
		{
		}
	}
}
