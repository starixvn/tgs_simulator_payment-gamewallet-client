using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.GameWallet
{
    class GameWalletDepositHandlerFactory(IDictionary<string, Type> requestDepositHandlerTypes, IServiceProvider serviceProvider): IDepositHandlerFactory
    {
        public IDepositHandler GetDepositHandler(string channelCode)
        => requestDepositHandlerTypes.TryGetValue(channelCode, out var depositHandlerType)?(IDepositHandler)serviceProvider.GetService(depositHandlerType!)!: throw new NotImplementedException();
    }
}
