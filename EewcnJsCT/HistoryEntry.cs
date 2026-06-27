namespace EewcnJsCT;

public struct HistoryEntry
{
    public string id;//字符串型事件ID
    public string O_TIME;//YYYY-MM-DD HH:MM:SS格式发震时间
    public string EPI_LAT;//字符串型震中纬度
    public string EPI_LON;//字符串型震中经度
    public float EPI_DEPTH;//数值型震源深度（公里）
    public string AUTO_FLAG;//字符串型自动测定标记，例如A，M等
    public string EQ_TYPE;//"M"
    public string M;//字符串型震级
    public string LOCATION_C;//字符串型震源地名称
    public float intensity;//可选，表示最大烈度（数值型），若没有此项则采用程序内置的算法计算
}