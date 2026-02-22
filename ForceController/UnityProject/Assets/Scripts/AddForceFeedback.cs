using UnityEngine;
using System;
using System.Threading;

public class AddForceFeedback : SerialController
{
    private float _lastSentAngle = 0f; // 上次发送的角度
    private const float AngleThreshold = 0.5f; // 角度变化阈值（度），避免频繁发送
    private bool isSend = false;
    
    public Rigidbody rigidBody;
    public float forceValue;
    public float tempTorque;

    protected override void Start()
    {
        base.Start();
        rigidBody = this.gameObject.GetComponent<Rigidbody>();
    }

    protected override void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) 
        {
            isSend = true;
            tempTorque = forceValue;
        }
        else if (Input.GetKeyDown(KeyCode.S)) 
        {
            isSend = false;
            tempTorque = 0f;
            rigidBody.angularVelocity = Vector3.zero;
        }
        
        
        SendData("T" + tempTorque.ToString("F2")); // 格式化为两位小数发送
        rigidBody.AddTorque(new Vector3(0, -tempTorque, 0));
        rigidBody.angularVelocity = Vector3.zero;
        
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
    /// 可以处理任意大的角度差和多圈旋转
    /// </summary>
    private float CalculateShortestPathAngle(float startAngle, float endAngle)
    {
        // 计算原始差值
        float rawDiff = endAngle - startAngle;
        
        // 使用模运算将差值归一化到[-180, 180)范围
        // 这确保了总是选择最短路径
        float wrappedDiff = WrapAngle180(rawDiff);
        
        // 返回最短路径目标角度
        return startAngle + wrappedDiff;
    }

    
    /// <summary>
    /// 将角度差值包装到[-180, 180)范围
    /// </summary>
    private float WrapAngle180(float angle)
    {
        // 使用模运算处理任意角度
        angle = angle % 360f;
        
        // 调整到[-180, 180)范围
        if (angle > 180f)
            angle -= 360f;
        else if (angle <= -180f)
            angle += 360f;
            
        return angle;
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
    
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name=="Sphere")
        {
            rigidBody.AddForce(new Vector3(-forceValue, 0, 0));
            isSend = true;
        }
    }
}
