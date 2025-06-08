using UnityEngine;

/// <summary>
/// Classe para modificadores temporários de stats
/// </summary>
[System.Serializable]
public class StatModifier
{
    public string statName;
    public float value;
    public float duration;
    public float startTime;
    public string source;
    public bool isPercentage;
    
    public StatModifier(string stat, float val, float dur, string src = "", bool percentage = false)
    {
        statName = stat;
        value = val;
        duration = dur;
        source = src;
        isPercentage = percentage;
        startTime = Time.time;
    }
    
    public bool HasExpired()
    {
        return Time.time >= startTime + duration;
    }
    
    public float GetCurrentValue()
    {
        if (HasExpired()) return 0f;
        return value;
    }
    
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, (startTime + duration) - Time.time);
    }
}