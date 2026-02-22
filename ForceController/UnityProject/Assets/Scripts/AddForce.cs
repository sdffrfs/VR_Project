using UnityEngine;
using System;
using System.Threading;

public class AddForce : SerialController
{
    private float _lastSentAngle = 0f;
    private const float AngleThreshold = 0.5f;
    private bool isSend = false;
    
    // 碰撞状态标记
    private bool _isCollidingWithWall = false;
    // 碰撞方向标记：1表示逆时针，-1表示顺时针
    private int _collisionDirection = 1;
    
    public Rigidbody rigidBody;
    public float forceValue;
    public float tempTorque;
    
    // 可配置的持续扭矩值
    public float continuousTorque = 3f;

    protected override void Start()
    {
        base.Start();
        rigidBody = this.gameObject.GetComponent<Rigidbody>();
    }

    protected override void Update()
    {
        // 根据碰撞状态和方向决定发送的扭矩值
        if (_isCollidingWithWall)
        {
            // 乘以方向系数，逆时针为正，顺时针为负
            WriteToCache(_collisionDirection * continuousTorque);
        }
        else
        {
            WriteToCache(0);
        }
        
        ReadFromCache();
    }

    private void WriteToCache(float TorqueValue)
    {
        SendData("T" + TorqueValue.ToString("F2"));
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
    
    // 碰撞开始检测
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Wall")
        {
            _isCollidingWithWall = true;
            isSend = true;
            
            // 检测碰撞方向
            _collisionDirection = DetectCollisionDirection(collision);
            
            Debug.Log($"开始与墙体碰撞，方向：{(_collisionDirection > 0 ? "逆时针" : "顺时针")}，持续施力");
        }
    }
    
    // 碰撞持续检测
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name == "Wall")
        {
            _isCollidingWithWall = true;
            // 可以在这里持续更新碰撞方向（如果需要）
        }
    }
    
    // 碰撞结束检测
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.name == "Wall")
        {
            _isCollidingWithWall = false;
            isSend = false;
            Debug.Log("离开墙体，停止施力");
        }
    }
    
    /// <summary>
    /// 检测碰撞方向（顺时针或逆时针）
    /// </summary>
    private int DetectCollisionDirection(Collision collision)
    {
        if (collision.contacts.Length == 0) return 1; // 默认逆时针
        
        // 获取第一个接触点[1,7](@ref)
        ContactPoint contact = collision.contacts[0];
        
        // 获取碰撞点位置和法线[1,7](@ref)
        Vector3 collisionPoint = contact.point;
        Vector3 collisionNormal = contact.normal;
        
        // 计算从物体中心到碰撞点的向量
        Vector3 toCollisionPoint = collisionPoint - transform.position;
        
        // 计算物体前进方向（使用速度方向或transform.forward）
        Vector3 moveDirection = rigidBody.velocity.normalized;
        if (moveDirection.magnitude < 0.1f)
        {
            // 如果速度太小，使用物体的正面方向
            moveDirection = transform.forward;
        }
        
        // 计算碰撞点向量与前进方向的叉积[6](@ref)
        // 如果Y分量为正，表示逆时针碰撞；为负表示顺时针碰撞
        Vector3 crossProduct = Vector3.Cross(moveDirection, toCollisionPoint.normalized);
        
        // 根据叉积的Y分量判断碰撞方向
        if (crossProduct.y > 0)
        {
            return 1; // 逆时针碰撞
        }
        else
        {
            return -1; // 顺时针碰撞
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