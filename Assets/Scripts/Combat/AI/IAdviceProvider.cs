using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReE.Combat.AI
{
    public enum AdviceMode
    {
        Safe,
        Balanced,
        Risky,
        Free
    }

    public interface IAdviceProvider
    {
        /// <summary>
        /// 現在の戦況とモードに基づいてアドバイスを返す
        /// </summary>
        /// <param name="mode">アドバイスの傾向（Safety/Balanced/Risky/Free）</param>
        /// <param name="context">Freeモード時の追加コンテキスト（質問文など）</param>
        /// <returns>提案テキスト</returns>
        string GetAdvice(AdviceMode mode, string context = null);
    }
}
