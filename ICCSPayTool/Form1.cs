using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using ICCSPayAPI;
using System.Threading;
using System.Collections.Concurrent;
using log4net;

namespace ICCSPayTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private static ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        bool isRefundable = false;//是否可退款标志符。
        string orderNo = "";
        string orderStatus = "0";
        string step = "0";
        string paymentVendor = "";
        string sqlConn = "";
        string sqlTable = "";
        string[] strS = new string[3];

        private void button1_Click(object sender, EventArgs e)
        {
            orderNo = textBox1.Text.Trim();
            if (!string.IsNullOrEmpty(orderNo) && (orderNo.TrimEnd()[0] >= 'A' && orderNo.TrimEnd()[0] <= 'Z'))
            {
                if (orderNo.Length == 30)
                {
                    if ((orderNo.StartsWith("QS") && Convert.ToInt32(orderNo.Substring(2, 4)) <= 2025) || (orderNo.StartsWith("S") && Convert.ToInt32(orderNo.Substring(1, 4)) <= 2025) || (orderNo.StartsWith("W") && Convert.ToInt32(orderNo.Substring(1, 4)) <= 2018) || (orderNo.StartsWith("A") && Convert.ToInt32(orderNo.Substring(1, 4)) <= 2025) || (orderNo.StartsWith("R") && Convert.ToInt32(orderNo.Substring(1, 4)) <= 2025))
                    {
                        try
                        {
                            //gaoke 20180508 根据订单信息确定查询数据库
                            if (orderNo.Substring(3, 27).Contains("CS"))
                            {
                                sqlConn = "Server=172.20.24.196;DataBase=ICCSPayDB_develop;uid=sa;pwd=`123qwer";
                            }
                            else if (orderNo.Substring(3, 27).Contains("S2"))
                            {
                                sqlConn = "Server=172.20.24.194;DataBase=ICCSPayDB_deploy;uid=sa;pwd=`123qwer";
                            }
                            else if (orderNo.Substring(3, 27).Contains("S3"))
                            {
                                sqlConn = "Server=172.20.24.200;DataBase=ICCSPayDB_deploy;uid=sa;pwd=`123qwer";
                            }
                            else
                            {
                                sqlConn = "Server=172.20.1.188;DataBase=ICCSPayDB_deploy;uid=sa;pwd=`123qwer";
                            }
                            //gaoke 20180508 根据订单信息确定查询表
                            if (orderNo.StartsWith("W"))
                            {
                                strS[0] = "Select * from WebOrder where TradeNo = '" + orderNo + "'";
                                strS[1] = "Select * from WebOrder_his where TradeNo = '" + orderNo + "'";
                                strS[2] = "Select * from WebOrder_his_3mon where TradeNo = '" + orderNo + "'";
                            }
                            else if (orderNo.StartsWith("S"))
                            {
                                strS[0] = "Select * from StationOrder where TradeNo = '" + orderNo + "'";
                                strS[1] = "Select * from StationOrder_his where TradeNo = '" + orderNo + "'";
                                strS[2] = "Select * from StationOrder_his_3mon where TradeNo = '" + orderNo + "'";
                            }
                            else if (orderNo.StartsWith("QS"))
                            {
                                strS[0] = "Select * from QRCStationOrder where TradeNo = '" + orderNo + "'";
                                strS[1] = "Select * from QRCStationOrder_his where TradeNo = '" + orderNo + "'";
                                strS[2] = "Select * from QRCStationOrder_his_3mon where TradeNo = '" + orderNo + "'";
                            }
                            else if (orderNo.StartsWith("R"))
                            {
                                strS[0] = "Select * from WTOrder where TradeNo = '" + orderNo + "'";
                                strS[1] = "Select * from WTOrder where TradeNo = '" + orderNo + "'";
                                strS[2] = "Select * from WTOrder where TradeNo = '" + orderNo + "'";
                            }
                            else if (orderNo.StartsWith("A"))
                            {
                                strS[0] = "Select * from BomOrder where OrderNo = '" + orderNo + "'";
                                strS[1] = "Select * from BomOrder_his where OrderNo = '" + orderNo + "'";
                                strS[2] = "Select * from BomOrder_his_3mon where OrderNo = '" + orderNo + "'";
                            }
                            SqlConnection Conn = new SqlConnection(sqlConn);
                            Conn.Open();
                            DataSet ds = new DataSet();
                            SqlDataAdapter da = new SqlDataAdapter();
                            for (int i = 0; i < strS.Length; i++)
                            {
                                SqlCommand cmd = new SqlCommand(strS[i], Conn);
                                da.SelectCommand = cmd;
                                da.SelectCommand.ExecuteNonQuery();
                                da.Fill(ds);
                                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    orderStatus = ds.Tables[0].Rows[0]["OrderStatus"].ToString();
                                    step = ds.Tables[0].Rows[0]["Step"].ToString();
                                    paymentVendor = ds.Tables[0].Rows[0]["PaymentVendor"].ToString();
                                    sqlTable = strS[i].Substring(strS[i].IndexOf("from") + 4, strS[i].IndexOf("where") - 1 - strS[i].IndexOf("from") - 4);
                                    break;
                                }
                            }
                            Conn.Close();

                            //gaoke 20180508 根据订单状态给出查询结果
                            if (orderStatus == "5")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为已正常出票！";
                            }
                            else if (orderStatus == "8")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为已成功退款！";
                            }
                            else if (orderStatus == "7")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为已申请退款！";
                            }
                            else if (orderStatus == "9")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为乘客账户异常导致退款失败，会继续自动退款，无需人工处理！";
                            }
                            else if (orderStatus == "10")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为已关闭，无需处理！";
                            }
                            else if (orderStatus == "6")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为设备故障导致少出票，需车站现场退款给乘客！";
                            }
                            else if (orderStatus == "4")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为支付失败，可尝试申请退款！";
                                isRefundable = true;
                            }
                            else if (orderStatus == "3")
                            {
                                if (orderNo.StartsWith("W") && step.Equals("7"))
                                {
                                    isRefundable = true;
                                    textBox2.Text = "订单 " + orderNo + " 当前状态为支付成功待取票状态，可申请退款！";
                                }
                                else if (step.Equals("11"))
                                {
                                    textBox2.Text = "订单 " + orderNo + " 当前状态为支付成功且认证通过，因设备未按流程发出票结果通知，故需供货商核查后处理！";
                                }
                            }
                            else if (orderStatus == "1")
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态为待支付，可尝试申请退款！";
                                isRefundable = true;
                            }
                            else
                            {
                                textBox2.Text = "订单 " + orderNo + " 当前状态异常，需核查数据库！";
                            }
                            _log.Info("订单 " + orderNo + " 在" + sqlConn.Substring(sqlConn.IndexOf("172"), sqlConn.IndexOf("DataBase") - 1 - sqlConn.IndexOf("172")) + " 服务器 " + sqlConn.Substring(sqlConn.IndexOf("ICCSPayDB"), sqlConn.IndexOf("uid") - 1 - sqlConn.IndexOf("DataBase") - 9) + "数据库" + sqlTable + "表中查询到，订单状态为：" + orderStatus);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    else
                    {
                        label4.Text = "订单号中时间不对，请核查！";
                    }
                }
                else
                {
                    label4.Text = "订单号格式不正确！请输入长度为30位的订单号！";
                }
            }
            else
            {
                label4.Text = "订单号格式不正确！请输入长度为30位且以大写W、S、A、R、QS等字母开头的订单号！";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (isRefundable)
            {
                ICCSPayAPIResultVO payResultVo = null;
                ICCSPayAPIHandle ap = new ICCSPayAPIHandle();
                payResultVo = ap.ICCSPay_Orderquery(paymentVendor, orderNo);
                string s = payResultVo.toPrint();

                if ((payResultVo != null && payResultVo.is_success && payResultVo.ActualFee > 100) && ((paymentVendor == "0001" && payResultVo.result_code == "SUCCESS") || (paymentVendor == "1001" && payResultVo.result_code == "TRADE_SUCCESS") || (paymentVendor == "1002" && payResultVo.result_code == "OK")))
                {
                    //确定订单已支付，后续可开展退款
                    ICCSPayAPIResultVO refundVo = ap.ICCSPay_Refund(paymentVendor, payResultVo.transactionId, orderNo, payResultVo.ActualFee, payResultVo.ActualFee);
                    if (refundVo.is_success)
                    {
                        //更新订单记录
                        SqlConnection Conn1 = new SqlConnection(sqlConn);
                        string updateSql = "";
                        if (orderNo.StartsWith("A"))
                        {
                            updateSql = "Update " + sqlTable + " set TransactionId='" + payResultVo.transactionId + "',BankType='" + payResultVo.BankType + "',PayEndTime='" + payResultVo.returnEndTime + "',PayEndTimeRaw='" + payResultVo.returnEndTimeRaw + "',ActualFee='" + payResultVo.ActualFee + "',OrderStatus='8',Step='7' where orderNo = '" + orderNo + "'";
                        }
                        else
                        {
                            updateSql = "Update " + sqlTable + " set TransactionId='" + payResultVo.transactionId + "',BankType='" + payResultVo.BankType + "',PayEndTime='" + payResultVo.returnEndTime + "',PayEndTimeRaw='" + payResultVo.returnEndTimeRaw + "',ActualFee='" + payResultVo.ActualFee + "',OrderStatus='8',Step='7' where tradeNo = '" + orderNo + "'";
                        }
                        SqlCommand cmd1 = new SqlCommand(updateSql, Conn1);
                        try
                        {
                            Conn1.Open();
                            cmd1.ExecuteNonQuery();
                            Conn1.Close();
                            textBox3.Text = "订单 " + orderNo + " 已退款！";
                            _log.Info("订单 " + orderNo + " 已于" + DateTime.Now.ToString() + "申请退款成功，退款金额 ：" + refundVo.ActualFee + "分，且已更新订单表订单状态。");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        //增加退款表信息
                        SqlConnection Conn2 = new SqlConnection(sqlConn);
                        string insertSql = "insert into WebOrderRefund  (WebOrderRefundId ,WebOrderId ,TradeNo,ExternalTradeNo,RefundTradeNo"
           + ",RefundReason,PaymentVendor,RefundFee,TotalFee,BankType"
           + ",RequestTime,IsRequestSuccess,RequestErrCodeDes ,IsRespondSuccess"
           + ",RespondTime ,RespondErrCodeDes ,OrderStatus) VALUES "
           + "('" + Guid.NewGuid() + "','" + Guid.NewGuid() + "','" + orderNo + "','" + Guid.NewGuid() + "','" + payResultVo.transactionId + "','"
           + "申请退款" + "','" + paymentVendor + "','" + payResultVo.ActualFee + "','" + payResultVo.ActualFee + "','" + payResultVo.BankType + "','"
           + DateTime.Now + "','0','" + "" + "','0','"
           + refundVo.returnEndTime + "','" + "" + "','8')";
                        SqlCommand cmd2 = new SqlCommand(insertSql, Conn2);
                        try
                        {
                            Conn2.Open();
                            cmd2.ExecuteNonQuery();
                            Conn2.Close();
                            _log.Info("订单 " + orderNo + " 已申请退款，并将订单退款记录写入退款表。");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                    }
                }
                else
                {
                    textBox3.Text = "订单 " + orderNo + " 未支付，不可申请退款！";
                }
            }
            else
            {
                textBox3.Text = "订单 " + orderNo + " 不可申请退款！";
            }
        }

        private void button3_Click(object sender, EventArgs e)  //订单校准
        {
            string begin_date = textBox4.Text + " 00:00:00";
            string end_date = textBox5.Text + " 23:59:59";
            if (DateTime.Compare(Convert.ToDateTime(end_date), Convert.ToDateTime(begin_date)) > 0)
            {
                sqlConn = "Server=172.20.24.196;DataBase=ICCSPayDB_develop;uid=sa;pwd=`123qwer";
                string sqlStr = "select * from StationOrder where ((OrderStatus <> '5') and PaymentVendor in ('1001','1002','0001','0002') and PayEndTime >= '" + begin_date + "' and PayEndTime <= '" + end_date + "')";
                SqlConnection Conn = new SqlConnection(sqlConn);
                Conn.Open();
                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter();
                SqlCommand cmd = new SqlCommand(sqlStr, Conn);
                da.SelectCommand = cmd;
                da.SelectCommand.ExecuteNonQuery();
                da.Fill(ds);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ICCSPayAPIResultVO payResultVo = null;
                    ICCSPayAPIHandle ap = new ICCSPayAPIHandle();
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        //orderStatus = ds.Tables[0].Rows[i]["OrderStatus"].ToString();
                        //step = ds.Tables[0].Rows[i]["Step"].ToString();
                        payResultVo = ap.ICCSPay_Orderquery(ds.Tables[0].Rows[i]["PaymentVendor"].ToString(), ds.Tables[0].Rows[i]["TradeNo"].ToString());
                      
                        if ((ds.Tables[0].Rows[i]["OrderStatus"].ToString() == "1" || ds.Tables[0].Rows[i]["OrderStatus"].ToString() == "4") && (payResultVo != null && payResultVo.is_success && payResultVo.ActualFee > 100) && ((paymentVendor == "0001" && payResultVo.result_code == "SUCCESS") || (paymentVendor == "1001" && payResultVo.result_code == "TRADE_SUCCESS") || (paymentVendor == "1002" && payResultVo.result_code == "OK")))
                        {
 
                        }
                    }
                }
                Conn.Close();
            }
        }
    }
}
