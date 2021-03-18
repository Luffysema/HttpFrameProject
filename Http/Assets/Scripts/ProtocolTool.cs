using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using protocol;

/// <summary>
/// 协议工具类
/// </summary>
public class ProtocolTool
{
    /// <summary>
    /// protocol编译工具
    /// </summary>
    static CmdSerializer m_builder = new CmdSerializer();
    /// <summary>
    /// 加密字节组
    /// </summary>
    static byte[] m_EncryptKey = null;
    /// <summary>
    /// 消息头长度
    /// </summary>
    static int MAX_HEADER_LEN = 8;
    enum ParserState
    {
        E_STATE_HEADER,
        E_STATE_BODY
    }
    /// <summary>
    /// 封包
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="msgId">消息ID</param>
    /// <param name="session">加密初始字符串</param>
    /// <param name="cmd">消息体</param>
    /// <returns></returns>
    public static byte[] PackMsg<T>(int msgId, string session, T cmd)
    {
        byte[] bodyData = PBSeralize(cmd);
        //加密
        EncryptData(bodyData);
        //协议Id号转字节
        int msgIdLen = IPAddress.HostToNetworkOrder(msgId);//将由主机字节顺序（HBO）的较短的值转换为网络字节顺序(大端模式)
        byte[] msgIdData = BitConverter.GetBytes(msgIdLen);
        //加密字符转字节
        byte[] sessionData = Encoding.UTF8.GetBytes(session);
        byte sessionLen = (byte)sessionData.Length;
#if UNITY_EDITOR
        Debug.Log("msgIdLen:" + msgIdData.Length);
        Debug.Log("sessionDataLen:" + sessionData.Length);
#endif     
        //消息体总长度   +1是为了保存加密字符长度
        int dataLen = 1 + sessionData.Length + msgIdData.Length + bodyData.Length;
        byte[] data = new byte[dataLen];
        //加入加密消息体
        data[0] = sessionLen;
        for (int i = 0; i < sessionData.Length; i++)
        {
            data[1 + i] = sessionData[i];
        }
        //加入协议号消息体
        for (int i = 0; i < msgIdData.Length; i++)
        {
            data[1 + sessionData.Length + i] = msgIdData[i];
        }
        //加入数据消息体
        for (int i = 0; i < bodyData.Length; i++)
        {
            data[1 + sessionData.Length + msgIdData.Length + i] = bodyData[i];
        }
        dataLen = data.Length;
        //返回总包
        return data;
    }

    /// <summary>
    /// 拆包
    /// 多个消息体的结合：消息头（8个字节）+消息体+消息头（8个字节）+消息体.....
    /// </summary>
    /// <param name="datas"></param>
    public static List<RspMsgCntx> ParseRspCmd(byte[] datas)
    {
        List<RspMsgCntx> msgCntxList = new List<RspMsgCntx>();
        ParserState parserState = ParserState.E_STATE_HEADER;

        int leftBytes = datas.Length;
        int offset = 0;//偏移量 “多个消息体位置纪录”
        int headerCount = 0;
        int bodyCount = 0;
        byte[] msgHeader = new byte[MAX_HEADER_LEN];
        byte[] msgBody = null;
        int bodyLen = 0;//消息体长度
        int msgType = 0;//协议号（第六个字节）
        byte code = 0;//状态码（第五个字节）
        do
        {
            switch (parserState)
            {
                case ParserState.E_STATE_HEADER:
                    //是否有足够的消息头长度
                    if (leftBytes - (MAX_HEADER_LEN - headerCount) >= 0)
                    {
                        Array.Copy(datas, offset, msgHeader, headerCount, MAX_HEADER_LEN - headerCount);

                        offset += MAX_HEADER_LEN - headerCount;
                        leftBytes -= MAX_HEADER_LEN - headerCount;

                        headerCount = 0;

                        int msgLen = BitConverter.ToInt32(msgHeader, 0);
                        msgLen = IPAddress.NetworkToHostOrder(msgLen);
                        bodyLen = msgLen - 5; // WARNNING!!!!!! -sequenceID, -msgType, -time

                        code = msgHeader[4];
                        msgType = BitConverter.ToInt32(msgHeader, 5);
                        msgType = IPAddress.NetworkToHostOrder(msgType);

                        if (bodyLen > 0)
                        {
                            if (msgBody == null)
                            {
                                msgBody = new Byte[bodyLen];

                                if (msgBody == null)
                                {
                                    Debug.LogError("ParseCmd Not enough memory....");
                                    return null;
                                }
                            }

                            bodyCount = 0;
                            parserState = ParserState.E_STATE_BODY;
                        }
                        else if (bodyLen == 0)
                        {       
                            RspMsgCntx msgCntx = new RspMsgCntx();
                            msgCntx.cmdLen = bodyLen;
                            msgCntx.msgType = msgType;
                            msgCntx.code = code;
                            msgCntx.cmd = null;

                            msgCntxList.Add(msgCntx);
                            msgHeader.Initialize(); // memset
                            parserState = ParserState.E_STATE_HEADER;
                        }
                        else
                        {
                            Debug.LogError("ParseCmd body's length < 0");
                            return null;
                        }
                    }
                    else // ..
                    {
                        //memcpy(m_msgPackage.m_header + m_msgPackage.m_headerCount, pOffset, leftBytes);
                        Array.Copy(datas, offset, msgHeader, headerCount, leftBytes);
                        headerCount += leftBytes;

                        Debug.LogError("ParseCmd Not enough header's data");
                        return null;
                    }
                    break;
                case ParserState.E_STATE_BODY:
                    if (leftBytes - (bodyLen - bodyCount) >= 0)
                    {
                        Array.Copy(datas, offset, msgBody, bodyCount, bodyLen - bodyCount);

                        offset += bodyLen - bodyCount;
                        leftBytes -= bodyLen - bodyCount;

                        RspMsgCntx msgCntx = new RspMsgCntx();
                        msgCntx.cmdLen = bodyLen;
                        msgCntx.msgType = msgType;
                        msgCntx.code = code;
                        msgCntx.cmd = msgBody;

                        EncryptData(msgCntx.cmd);

                        msgCntxList.Add(msgCntx);

                        msgHeader.Initialize(); // memset..
                        msgBody = null;
                        headerCount = 0;
                        bodyCount = 0;

                        parserState = ParserState.E_STATE_HEADER;
                    }
                    else
                    {
                        Array.Copy(datas, offset, msgBody, bodyCount, leftBytes);
                        bodyCount += leftBytes;

                        Debug.LogError("ParseCmd Not enough body's data");
                        return null;
                    }
                    break;
            }
        } while (leftBytes > 0);
        return msgCntxList;
    }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="cmd">消息体</param>
    /// <returns></returns>
    public static byte[] PBSeralize<T>(T cmd)
    {
        MemoryStream stream = new MemoryStream();
        //protobuffer自带的序列化方法
        m_builder.Serialize(stream, cmd);

        byte[] data = new byte[stream.Length];
        stream.Seek(0, SeekOrigin.Begin);
        stream.Read(data, 0, (int)stream.Length);
        return data;
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="instance">目标类型</param>
    /// <param name="bytes">数据</param>
    public static void PBDeserialize<T>(T instance, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return;
        }
        MemoryStream stream = new MemoryStream(bytes);
        Type type = instance.GetType();
        m_builder.Deserialize(stream, instance, type);
    }

    /// <summary>
    /// 设置加密字节组
    /// </summary>
    /// <param name="str"></param>
    public static void SetEncryptKey(string str)
    {
        string str_e = "abc";
        byte[] b = Encoding.ASCII.GetBytes(str_e);
        if (String.IsNullOrEmpty(str) == false)
        {
            m_EncryptKey = Encoding.ASCII.GetBytes(str);
            int j = 0;
            for (int i = 0; i < m_EncryptKey.Length; i++)
            {
                m_EncryptKey[i] ^= b[j++];
                if (j >= b.Length)
                {
                    j = 0;
                }
            }
        }
    }

    /// <summary>
    /// 加密解密（异或）
    /// </summary>
    /// <param name="data">消息体</param>
    static void EncryptData(byte[] data)
    {
        if (m_EncryptKey != null && m_EncryptKey.Length > 0 && data != null)
        {
            int j = 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= m_EncryptKey[j++];
                if (j >= m_EncryptKey.Length)
                {
                    j = 0;
                }
            }
        }
    }
}
