using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReE.Combat.AI
{
    /// <summary>
    /// Packet 008.7: ダミー提案（ルールベース）
    /// 将来的にはここを LLMAdviceProvider などに差し替える運用を想定
    /// </summary>
    public class RuleBasedAdviceProvider : IAdviceProvider
    {
        public string GetAdvice(AdviceMode mode, string context = null)
        {
            switch (mode)
            {
                case AdviceMode.Safe:
                    return "【Safe】ガード優先／回復可能なら回復／無理な攻めを避ける";
                
                case AdviceMode.Balanced:
                    return "【Balanced】通常攻撃＋状況で回復／ガードは必要時のみ";
                
                case AdviceMode.Risky:
                    return "【Risky】攻撃スキル／攻撃魔法優先（MP枯渇に注意）";

                case AdviceMode.Free:
                    if (string.IsNullOrEmpty(context)) return "【相談】何か困っていますか？";
                    // 簡易エコーバック
                    return $"【相談】「{context}」についての提案：現在は判断材料が不足しています。（ダミー）";
            }
            return "";
        }
    }
}
