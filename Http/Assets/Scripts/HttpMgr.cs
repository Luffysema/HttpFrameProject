using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void HttpCallBack(HttpInterface _httpInterface);
/// <summary>
/// Http管理器
/// </summary>
public class HttpMgr : MonoBehaviour
{
    /// <summary>
    /// 单例化
    /// </summary>
    static HttpMgr m_HttpMgr = new HttpMgr();
    private HttpMgr()  {}
    public static HttpMgr GetHttpInstance()
    {
        return m_HttpMgr;
    }
    /// <summary>
    /// 加密字符
    /// </summary>
    public static string m_StartSession = "123456789";
    /// <summary>
    /// 请求列表
    /// </summary>
    private static List<HttpInterface> m_HttpList = new List<HttpInterface>();
    /// <summary>
    /// 当前请求
    /// </summary>
    private static HttpInterface m_CurrentHttp = null;

    void Update()
    {
        if (m_HttpList.Count > 0)
        {
            for (int i = 0; i < m_HttpList.Count; i++)
            {
                if (m_HttpList[i].CheckIsFinished())
                {
                    m_HttpList.Remove(m_HttpList[i]);
                    m_HttpList[i].Finished();

                    //一帧执行一次
                    break;
                }
            }
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < m_HttpList.Count; i++)
        {
            m_HttpList[i].Abort();
        }
        m_HttpMgr = null;
    }

    /// <summary>
    /// 请求接口(对外)
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="url">地址</param>
    /// <param name="msgId">协议号</param>
    /// <param name="cmd">消息内容</param>
    /// <param name="needNetWait">是否需要等待</param>
    public static void HttpCmdRequest<T>(string url, int msgId, T cmd, bool needNetWait)
    {
        HttpCmdRequest(url, msgId, cmd, needNetWait, 0);
    }

    /// <summary>
    /// 请求接口（对外）
    /// </summary>
    /// <param name="needWaitTime">等待时间</param>
    public static void HttpCmdRequest<T>(string url, int msgId, T cmd, bool needNetWait, float needWaitTime)
    {
        if (m_HttpMgr!=null)
        {
            m_HttpMgr._HttpCmdRequest(url, msgId, cmd, needNetWait, needWaitTime, HttpResponse);
        }
    }

    /// <summary>
    /// 请求接口
    /// </summary>
    /// <param name="httpResponse">请求回调</param>
    private void _HttpCmdRequest<T>(string url, int msgId, T cmd, bool needNetWait, float needWaitTime, HttpCallBack httpResponse)
    {
        _HttpCmdRequest(url, msgId, ProtocolTool.PackMsg(msgId, m_StartSession, cmd), needNetWait, needWaitTime, httpResponse);
    }

    /// <summary>
    /// 请求接口
    /// </summary>
    /// <param name="bytes">序列化之后的消息内容</param>
    private void _HttpCmdRequest(string url, int msgId, byte[] bytes, bool needNetWait, float needWaitTime, HttpCallBack httpResponse)
    {
        if (m_HttpList.Count >= 16 && needNetWait == false)
        {
#if UNITY_EDITOR
            Debug.LogError("Http请求数过多，新增请求无效。。。");
#endif
            if (string.IsNullOrEmpty(url)==false)
            {
                HttpInterface one = new HttpInterface();
                one.m_NeedNetWait = needNetWait;
                one.CallBack = httpResponse;
                if (needNetWait==true)
                {
                    //展示等待框
                    m_CurrentHttp = one;
                }
                //请求
                one.Request(url, bytes, msgId);
                //
                m_HttpList.Add(one);
            }
            return;
        }

    }

    /// <summary>
    /// 响应处理
    /// </summary>
    /// <param name="_httpInterface"></param>
    private static void HttpResponse(HttpInterface _httpInterface)
    {
        if (_httpInterface.m_HttpState == Http.HttpState.E_HTTP_STATE_DOWNLOAD_FINISHED)
        {
            //反序列化
        }
        else if (_httpInterface.m_HttpState == Http.HttpState.E_HTTP_STATE_ERROR)
        {
            //报错提示
        }
    }
}
