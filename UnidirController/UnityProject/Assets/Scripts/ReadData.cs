using UnityEngine;
using System;
using System.Threading;

public class ReadData : SerialController
{
    private float _lastSentAngle = 0f;
    private const float AngleThreshold = 0.5f;
    private bool isSend = false;
    
    
    public Rigidbody rigidBody;
    

    protected override void Start()
    {
        base.Start();
        rigidBody = this.gameObject.GetComponent<Rigidbody>();
    }

    protected override void Update()
    {
        ReadFromCache();
    }

    private void ReadFromCache()
    {
        if (this.NewDataReceived)
        {
            lock (DataLock)
            {
                this.CurrentAngle = this.ThreadSafeAngle;
                this.ThreadSafeDataFlag = false;
            }
            this.NewDataReceived = false;
            this.targetCube.rotation = Quaternion.Euler(0f, this.CurrentAngle, 0f);
        }
    }
    
    
    

    /// <summary>
    /// 计算从起点到终点的最短路径角度
    /// </summary>
    private float CalculateShortestPathAngle(float startAngle, float endAngle)
    {
        float rawDiff = endAngle - startAngle;
        float wrappedDiff = WrapAngle180(rawDiff);
        return startAngle + wrappedDiff;
    }

    /// <summary>
    /// 将角度差值包装到[-180, 180)范围
    /// </summary>
    private float WrapAngle180(float angle)
    {
        angle = angle % 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle <= -180f)
            angle += 360f;
        return angle;
    }

    protected override void ProcessReceivedData(string data)
    {
        try
        {
            if (float.TryParse(data, out float angle))
            {
                lock (DataLock)
                {
                    ThreadSafeAngle = angle;
                    ThreadSafeDataFlag = true;
                }
                NewDataReceived = true;
            }
        }
        catch (FormatException ex)
        {
            Debug.LogWarning($"数据格式错误: {data}, 错误: {ex.Message}");
        }
    }
}