using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Http状态委托定义
/// </summary>
/// <param name="state"></param>
public delegate void HttpStateBack(Http.HttpState state);
/// <summary>
/// Http线程连接
/// </summary>
public class Http
{
    private WebClient m_Client = new WebClient();
    private Thread m_Thread;
    /// <summary>
    /// 地址
    /// </summary>
    private string m_url = null;
    /// <summary>
    /// 连接状态
    /// </summary>
    public enum HttpState : int
    {
        /// <summary>
        /// 等待
        /// </summary>
        E_HTTP_STATE_IDLE,
        /// <summary>
        /// 下载
        /// </summary>
        E_HTTP_STATE_DOWNLOADING,
        /// <summary>
        /// 报错
        /// </summary>
        E_HTTP_STATE_ERROR,
        /// <summary>
        /// 完成
        /// </summary>
        E_HTTP_STATE_DOWNLOAD_FINISHED,
        E_HTTP_STATE_MAX
    }
    /// <summary>
    /// 当前状态
    /// </summary>
    private HttpState m_HttpState = HttpState.E_HTTP_STATE_IDLE;
    /// <summary>
    /// 状态回调
    /// </summary>
    private HttpStateBack m_HttpStateCB = null;
    /// <summary>
    /// 推送数据
    /// </summary>
    private byte[] m_PostData = null;
    /// <summary>
    /// 下载数据
    /// </summary>
    private byte[] m_GetData = null;
    /// <summary>
    /// 协议ID
    /// </summary>
    private int m_MsgId;
    /// <summary>
    /// 构造
    /// </summary>
    public Http()
    {
        m_HttpState = HttpState.E_HTTP_STATE_IDLE;
        m_url = null;
    }

    /// <summary>
    /// 提交数据并下载Post
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="stateCB">状态回调</param>
    /// <param name="msgId">协议ID</param>
    /// <param name="postData">消息体</param>
    public void HttpPostRequest(string url,byte[] postData,int msgId,HttpStateBack stateCB)
    {
        if (m_HttpState!=HttpState.E_HTTP_STATE_DOWNLOADING)
        {
            m_url = url;
            m_HttpStateCB = stateCB;
            m_PostData = postData;
            m_MsgId = msgId;
            //开启协程
            ThreadStart threadstart = new ThreadStart(HttpPostProc);
            m_Thread = new Thread(threadstart);
            m_Thread.Start();
        }
    }

    /// <summary>
    /// 重新提交数据并下载
    /// </summary>
    public void RetryPostRequest()
    {
        if (m_HttpState!=HttpState.E_HTTP_STATE_DOWNLOADING)
        {
            ThreadStart threadstart = new ThreadStart(HttpPostProc);
            m_Thread = new Thread(threadstart);
            m_Thread.Start();
        }
    }
    /// <summary>
    /// 连接并下载
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="stateCB">状态回调</param>
    public void HttpRequest(string url,HttpStateBack stateCB)
    {
        if (m_HttpState!=HttpState.E_HTTP_STATE_DOWNLOADING)
        {
            m_url = url;
            m_HttpStateCB = stateCB;

            ThreadStart threadstart = new ThreadStart(HttpLoadProc);
            m_Thread = new Thread(threadstart);
            m_Thread.Start();
        }
    }

    /// <summary>
    /// 重新连接并下载
    /// </summary>
    public void RetryHttpRequest()
    {
        if (m_HttpState==HttpState.E_HTTP_STATE_DOWNLOADING)
        {
            ThreadStart threadstart = new ThreadStart(HttpLoadProc);
            m_Thread = new Thread(threadstart);
            m_Thread.Start();
        }
    }

    /// <summary>
    /// 获取下载信息
    /// </summary>
    /// <returns></returns>
    public byte[] GetHttpData()
    {
        if (m_GetData != null)
        {
            return m_GetData;
        }
        return null;
    }

    /// <summary>
    /// 退出线程
    /// </summary>
    public void ExitThread()
    {
        if (m_Thread!=null)
        {
            m_Thread.Abort();
        }
    }

    /// <summary>
    /// 提交数据并下载并下载
    /// </summary>
    private void HttpPostProc()
    {
        m_HttpState = HttpState.E_HTTP_STATE_DOWNLOADING;
        if (m_HttpStateCB != null)
        {
            m_HttpStateCB(m_HttpState);
        }
        try
        {
            m_Client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            m_Client.Encoding = Encoding.UTF8;
            m_GetData = m_Client.UploadData(new Uri(m_url), "POST", m_PostData);
            m_HttpState = HttpState.E_HTTP_STATE_DOWNLOAD_FINISHED;
            if (m_HttpStateCB != null)
            {
                m_HttpStateCB(m_HttpState);
            }
        }
        catch (Exception)
        {
            m_HttpState = HttpState.E_HTTP_STATE_ERROR;
            if (m_HttpStateCB!=null)
            {
                m_HttpStateCB(m_HttpState);
            }
            throw;
        }
    }

    /// <summary>
    /// 连接并下载
    /// </summary>
    private void HttpLoadProc()
    {
        m_HttpState = HttpState.E_HTTP_STATE_DOWNLOADING;
        if (m_HttpStateCB!=null)
        {
            m_HttpStateCB(m_HttpState);
        }
        try
        {
            m_GetData = m_Client.DownloadData(m_url);
            //m_HttpState = HttpState.E_HTTP_STATE_DOWNLOAD_FINISHED;
            if (m_HttpStateCB!=null)
            {
                m_HttpStateCB(m_HttpState);
            }
        }
        catch (Exception)
        {
            m_HttpState = HttpState.E_HTTP_STATE_ERROR;
            if (m_HttpStateCB!=null)
            {
                m_HttpStateCB(m_HttpState);
            }
            throw;
        }
    }

}
