// ==================== 启动窗体切换 ====================
// 取消下面某行的注释切换启动窗体：
//#define TEST_SENS            // 灵敏度测试（侧键2单击锁头移动，不开枪）
//#define TEST_MODE          // HumanLikeMove 轨迹测试
//#define TEST_RIFLE          // ISSUE-013 步枪模式隔离测试
//#define TEST_SNIPER         // 狙击反作弊触发测试（瞬移+开枪）
//define TEST_CROSSHAIR      // 步枪准心命中颜色采样测试
//#define TEST_CROSSHAIR_STATE // 准心状态监视（真人模式原生链路诊断）
// ======================================================

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gprs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#if TEST_SENS
            // 灵敏度测试：按侧键2 锁头移动一次（不开枪，CD 1s），观察过冲/欠冲标定灵敏度
            Application.Run(new TestSensForm());
#elif TEST_SNIPER
            // 狙击反作弊触发测试：瞬移+开枪，复用瞬狙流程但不锁人
            Application.Run(new TestSniperForm());
#elif TEST_CROSSHAIR
            // 准心命中颜色采样测试：观察步枪准心命中敌人的瞬间 RGB 变化
            Application.Run(new TestCrosshairColorForm());
#elif TEST_CROSSHAIR_STATE
            // 准心状态监视测试：复刻主模块真人模式链路，观察准心四态/RGB/状态机时序
            Application.Run(new TestCrosshairStateForm());
#elif TEST_RIFLE
            // ISSUE-013 测试模式：步枪模式隔离测试
            Application.Run(new TestRifleForm());
#elif TEST_MODE
            // 测试模式：启动 HumanLikeMove 测试窗体
            Application.Run(new TestMoveForm());
#else
            // 正常模式：启动主窗体
            Application.Run(new Form1());
#endif
        }
    }
}