using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monitor : SerialController
{
    // Start is called before the first frame update

    // Update is called once per frame
    protected override void Update()
    {
        Debug.Log(this.ThreadSafeAngle);
    }
    
    protected override void ProcessReceivedData(string data)
    {
        // 在这里实现处理传感器数据的逻辑
        // 例如解析数据并根据数据内容执行相应操作
        
        try
        {
            // 解析浮点数角度值[4](@ref)
            if (float.TryParse(data, out float angle)) // 将data转换为浮点数，并保存到angle变量里
            {
                lock (DataLock) // 读取线程共享的数据必须加锁
                {
                    ThreadSafeAngle = angle;
                    ThreadSafeDataFlag = true;
                }
                NewDataReceived = true;
                
            }
            else
            {
                // Debug.LogWarning($"无法解析数据: {data}");
            }
        }
        catch (FormatException ex) // 捕获格式异常
        {
            Debug.LogWarning($"数据格式错误: {data}, 错误: {ex.Message}");
        }
    }
}
