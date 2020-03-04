using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    interface IIndicator
    {
        void UpdateState();
        bool IsBuySignal();
        bool IsSellSignal();
    }
}
