using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Http接口类型
/// </summary>
public class HttpInterface
{
    /// <summary>
    /// 响应回调
    /// </summary>
    private HttpCallBack m_HttpCallBack = null;
    public HttpCallBack CallBack
    {
        set
        {
            m_HttpCallBack = value;
        }
    }
    public bool m_NeedNetWait = false;
    /// <summary>
    /// 下载数据
    /// </summary>
    public byte[] m_HttpData = null;
    /// <summary>
    /// 当前http状态
    /// </summary>
    public Http.HttpState m_HttpState = Http.HttpState.E_HTTP_STATE_IDLE;
    /// <summary>
    /// 是否已推送
    /// </summary>
    private bool m_IsPost = false;   
    private Http m_http = new Http();
    /// <summary>
    /// 请求服务器
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="data">信息数据</param>
    /// <param name="msgId">协议ID</param>
    public void Request(string url, byte[] data, int msgId)
    {
        if (data != null && data.Length > 0)
        {
            m_IsPost = true;
            m_http.HttpPostRequest(url, data, msgId, StateCallBack);
        }
        else
        {
            m_http.HttpRequest(url, StateCallBack);
        }
    }

    /// <summary>
    /// 重新请求
    /// </summary>
    public void RetryRequest()
    {
        if (m_NeedNetWait == true)
        {
            //显示展示框
        }
        if (m_IsPost == true)
        {
            m_http.RetryPostRequest();
        }
        else
        {
            m_http.RetryHttpRequest();
        }
    }

    /// <summary>
    /// 检测是否请求完成
    /// </summary>
    /// <returns></returns>
    public bool CheckIsFinished()
    {
        if (m_HttpState == Http.HttpState.E_HTTP_STATE_DOWNLOAD_FINISHED || m_HttpState == Http.HttpState.E_HTTP_STATE_ERROR)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 请求完成
    /// </summary>
    public void Finished()
    {
        if (m_HttpCallBack != null)
        {
            if (m_NeedNetWait == true)
            {
                //展示等待窗
            }
            m_HttpCallBack(this);
        }
    }

    /// <summary>
    /// 终止
    /// </summary>
    public void Abort()
    {
        if (m_http!=null)
        {
            m_http.ExitThread();
        }
    }

    /// <summary>
    /// 状态回调
    /// </summary>
    /// <param name="state">http连接状态</param>
    private void StateCallBack(Http.HttpState state)
    {
        if (state == Http.HttpState.E_HTTP_STATE_DOWNLOAD_FINISHED)
        {
            m_HttpData = m_http.GetHttpData();
        }
        m_HttpState = state;
    }
}
