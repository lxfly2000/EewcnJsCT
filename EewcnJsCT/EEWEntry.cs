namespace EewcnJsCT;

public struct EEWEntry
{
    public string eventId;//字符串型事件ID
    public int updates;//数值型第几报
    public float latitude;//数值型震中纬度
    public float longitude;//数值型震中经度
    public float depth;//数值型震源深度（公里）
    public string epicenter;//字符串型震源地名称
    public long startAt;//数值型发震时间戳（毫秒）
    public float magnitude;//数值型震级
    public float intensity;//可选，表示最大烈度（数值型）
}