using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Cryville.EEW;
using Microsoft.ClearScript;

namespace EewcnJsCT
{
    public class Util
    {
        public static string ReadAllFile(string filePath)
        {
            try
            {
                StreamReader file = new StreamReader(filePath, Encoding.UTF8);
                string s = file.ReadToEnd();
                file.Close();
                file.Dispose();
                return s;
            }
            catch
            {
                return "";
            }
        }

        public static string GetString(string key,[NotNull]ref CultureInfo? culture)
        {
            using var lres = new LocalizedResource("", ref culture);
            var res = lres.RootMessageStringSet;
            return res.GetStringRequired(key);
        }

        public static string GetString(string key, string langTag)
        {
            CultureInfo cultureInfo=new CultureInfo(langTag);
            return GetString(key,ref cultureInfo);
        }

        /// <summary>
        /// 判断对象是哪种数据
        /// </summary>
        /// <param name="scriptObject">脚本对象</param>
        /// <returns>0:未知 1:EEW 2:History</returns>
        public static int GetScriptObjectDataType(ref ScriptObject scriptObject)
        {
            if (scriptObject == null || scriptObject is Undefined)
                return 0;
            if (!(scriptObject.GetProperty("data") is Undefined))
                return 1;
            if (!(scriptObject.GetProperty("shuju") is Undefined))
                return 2;
            return 0;
        }

        public static List<int> FindUpdatedEEWObjectIndex(ref ScriptObject oldObj, ref ScriptObject newObj)
        {
            List<int>indexes=new List<int>();
            /*
             * eventId:"字符串型事件ID",
                updates:数值型第几报,
                latitude:数值型震中纬度,
                longitude:数值型震中经度,
                depth:数值型震源深度（公里）,
                epicenter:"字符串型震源地名称",
                startAt:数值型发震时间戳（毫秒）,
                magnitude:数值型震级,
             */
            //EEW:data
            for (int i = 0; i < GetObjectCount(ref newObj,"data"); i++)
            {
                bool found=false;
                for (int j = 0; j < GetObjectCount(ref oldObj, "data"); j++)
                {
                    if (GetObjectIndexString(ref newObj, "data", i, "eventId") == GetObjectIndexString(ref oldObj, "data", j, "eventId") &&
                        GetObjectIndexInt(ref newObj, "data", i, "updates") == GetObjectIndexInt(ref oldObj, "data", j, "updates") &&
                        GetObjectIndexFloat(ref newObj, "data", i, "latitude") == GetObjectIndexFloat(ref oldObj, "data", j, "latitude") &&
                        GetObjectIndexFloat(ref newObj, "data", i, "longitude") == GetObjectIndexFloat(ref oldObj, "data", j, "longitude") &&
                        GetObjectIndexFloat(ref newObj, "data", i, "depth") == GetObjectIndexFloat(ref oldObj, "data", j, "depth") &&
                        GetObjectIndexString(ref newObj, "data", i, "epicenter") == GetObjectIndexString(ref oldObj, "data", j, "epicenter") &&
                        GetObjectIndexLong(ref newObj, "data", i, "startAt") == GetObjectIndexLong(ref oldObj, "data", j, "startAt") &&
                        GetObjectIndexFloat(ref newObj, "data", i, "magnitude") == GetObjectIndexFloat(ref oldObj, "data", j, "magnitude"))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    indexes.Add(i);
            }

            return indexes;
        }

        public static List<int> FindUpdatedHistoryObjectIndex(ref ScriptObject oldObj, ref ScriptObject newObj)
        {
            List<int>indexes=new List<int>();
            /*
             * id:"字符串型事件ID",
                O_TIME:"YYYY-MM-DD HH:MM:SS格式发震时间",
                EPI_LAT:"字符串型震中纬度",
                EPI_LON:"字符串型震中经度",
                EPI_DEPTH:数值型震源深度（公里）,
                AUTO_FLAG:"字符串型自动测定标记，例如A，M等"
                M:"字符串型震级",
                LOCATION_C:"字符串型震源地名称"
             */
            //History:shuju
            for (int i = 0; i < GetObjectCount(ref newObj,"shuju"); i++)
            {
                bool found=false;
                for (int j = 0; j < GetObjectCount(ref oldObj, "shuju"); j++)
                {
                    if (GetObjectIndexString(ref newObj,"shuju",i,"id")==GetObjectIndexString(ref oldObj,"shuju",j,"id")&&
                        GetObjectIndexString(ref newObj,"shuju",i,"O_TIME")==GetObjectIndexString(ref oldObj,"shuju",j,"O_TIME")&&
                        GetObjectIndexString(ref newObj,"shuju",i,"EPI_LAT")==GetObjectIndexString(ref oldObj,"shuju",j,"EPI_LAT")&&
                        GetObjectIndexString(ref newObj,"shuju",i,"EPI_LON")==GetObjectIndexString(ref oldObj,"shuju",j,"EPI_LON")&&
                        GetObjectIndexFloat(ref newObj,"shuju",i,"EPI_DEPTH")==GetObjectIndexFloat(ref oldObj,"shuju",j,"EPI_DEPTH")&&
                        GetObjectIndexString(ref newObj,"shuju",i,"AUTO_FLAG")==GetObjectIndexString(ref oldObj,"shuju",j,"AUTO_FLAG")&&
                        GetObjectIndexString(ref newObj,"shuju",i,"M")==GetObjectIndexString(ref oldObj,"shuju",j,"M")&&
                        GetObjectIndexString(ref newObj,"shuju",i,"LOCATION_C")==GetObjectIndexString(ref oldObj,"shuju",j,"LOCATION_C"))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    indexes.Add(i);
            }

            return indexes;
        }

        public static int GetObjectCount(ref ScriptObject obj,string rootKey)
        {
            if (obj == null)
                return 0;
            return (int)((ScriptObject)obj.GetProperty(rootKey)).GetProperty("length");
        }

        public static string GetObjectIndexString(ref ScriptObject obj, string rootKey, int index, string subKey)
        {
            return ((ScriptObject)(((ScriptObject)obj.GetProperty(rootKey))[index])).GetProperty(subKey).ToString()??"";
        }

        public static float GetObjectIndexFloat(ref ScriptObject obj, string rootKey, int index, string subKey)
        {
            object v=((ScriptObject)(((ScriptObject)obj.GetProperty(rootKey))[index])).GetProperty(subKey);
            return v is Undefined?0f:Convert.ToSingle(v);
        }

        public static int GetObjectIndexInt(ref ScriptObject obj, string rootKey, int index, string subKey)
        {
            object v=((ScriptObject)(((ScriptObject)obj.GetProperty(rootKey))[index])).GetProperty(subKey);
            return v is Undefined?0:Convert.ToInt32(v);
        }

        public static long GetObjectIndexLong(ref ScriptObject obj, string rootKey, int index, string subKey)
        {
            object v=((ScriptObject)(((ScriptObject)obj.GetProperty(rootKey))[index])).GetProperty(subKey);
            return v is Undefined?0:Convert.ToInt64(v);
        }

        public static IEnumerable<string> GetObjectIndexKeys(ref ScriptObject obj, string rootKey, int index)
        {
            return ((ScriptObject)((ScriptObject)obj.GetProperty(rootKey))[index]).PropertyNames;
        }

        public static ScriptObject GetObjectIndex(ref ScriptObject obj, string rootKey, int index)
        {
            return (ScriptObject)((ScriptObject)obj.GetProperty(rootKey))[index];
        }

        public static string JSRootDataToString(ref ScriptObject obj,string rootKey,ref List<int>selectedIndexes)
        {
            ScriptObject arrayObj = (ScriptObject)obj.GetProperty(rootKey);
            int length = (int)arrayObj.GetProperty("length");
            string s = "========ScriptObject========\n";
            for (int i = 0; i < selectedIndexes.Count; i++)
            {
                s += rootKey + "[" + selectedIndexes[i] + "]:\n";
                s += JSElemDataToString(ref obj, rootKey, selectedIndexes[i]);
            }

            return s+"========End=================\n";
        }

        public static string JSElemDataToString(ref ScriptObject obj,string rootKey,int index)
        {
            ScriptObject arrayObj = (ScriptObject)obj.GetProperty(rootKey);
            ScriptObject e = (ScriptObject)arrayObj[index];
            return Worker.lastInstance.eewcnJs.ScriptObjectToString(ref e)+"\n";
        }

        public static int CalcMaxInt(float magnitude,float depth)
        {
            double a = 1.65 * magnitude;
            double b = depth < 10 ? 1.21 * Math.Log10(10) : 1.21 * Math.Log10(depth);
            return (int)Math.Min(Math.Max(0.0,Math.Round(a / b)),12.0);
        }
        
        public static float MaxDistanceSWaveSpread(float magnitude,float depth){
            if(magnitude<=3.0) return 400;
            if(magnitude<=4.0) return 600;
            if(magnitude<=5.0) return 1100;
            if(magnitude<=5.5) return 1500;
            if(magnitude<=6.0) return 2200;
            if(magnitude<=6.2) return 2500;
            if(magnitude<=6.5) return 3200;
            if(magnitude<=7.0) return 4000;
            if(magnitude<=7.2) return 5000;
            if(magnitude<=7.5) return 6000;
            return 6500;
        }

        public const float rEarth = 6371;

        public static float SurfaceDistanceToStraight(float surfaceLength, float depth)
        {
            float rCenter = rEarth - depth;
            float theta = surfaceLength / rEarth;
            return (float)Math.Sqrt(rEarth * rEarth + rCenter * rCenter - 2 * rEarth * rCenter * Math.Cos(theta));
        }

        public static DateTimeOffset CalcLiveTimeTo(long startAt,float magnitude,float depth)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(startAt)
                .AddSeconds(SurfaceDistanceToStraight(MaxDistanceSWaveSpread(magnitude, depth), depth) / 4);
        }
    }
}