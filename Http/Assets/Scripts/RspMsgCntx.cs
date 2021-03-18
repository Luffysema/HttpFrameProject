using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通行返回的数据体
/// </summary>
public class RspMsgCntx
{
    public int msgType;
    public byte code = 0;
    public int cmdLen = 0; // protobuf len...
    public byte[] cmd;
}
