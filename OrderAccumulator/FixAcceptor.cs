using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message = QuickFix.Message;

namespace OrderAccumulator
{
    public class FixAcceptor : MessageCracker, IApplication
    {
        private readonly Dictionary<string, decimal> _exposures = new();
        private const decimal ExposureLimit = 100_000_000m;
        private Session? _session;

        public void FromApp(Message message, SessionID sessionID) => Crack(message, sessionID);


        public void OnCreate(SessionID sessionId)
        {
            {
                _session = Session.LookupSession(sessionId);
                if (_session is null)
                    throw new ApplicationException("Somehow session is not found");
            }
        }

        public void OnLogon(SessionID sessionID) => Console.WriteLine($"[Acceptor] Conectado: {sessionID}");
        public void OnLogout(SessionID sessionID) => Console.WriteLine($"[Acceptor] Desconectado: {sessionID}");
        public void ToAdmin(Message message, SessionID sessionID) { }
        public void FromAdmin(Message message, SessionID sessionID) { }
        public void ToApp(Message message, SessionID sessionID) => Console.WriteLine($"[Acceptor] Enviado: {message}");

        public void OnMessage(NewOrderSingle order, SessionID sessionID)
        {
            string symbol = order.Symbol.Obj;
            string side = order.Side.Obj == Side.BUY ? "Compra" : "Venda";
            int quantity = (int)order.OrderQty.Obj;
            decimal price = order.Price.Obj;
            decimal orderValue = price * quantity;

            _exposures.TryGetValue(symbol, out decimal currentExposure);
            decimal newExposure = side == "Compra" ? currentExposure + orderValue : currentExposure - orderValue;

            var response = new ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(Math.Abs(newExposure) > ExposureLimit ? ExecType.REJECTED : ExecType.NEW),
            new OrdStatus(OrdStatus.FILLED),
            order.Symbol,
            order.Side,
            new LeavesQty(0),
            new CumQty(quantity),  // Adicionado
            new AvgPx(price));

            response.SetField(new OrderQty(quantity));
            response.SetField(new Price(price));

            if (Math.Abs(newExposure) > ExposureLimit)
            {
                response.SetField(new Text("Limite de exposição excedido"));
            }
            else
            {
                _exposures[symbol] = newExposure;
            }

            Session.SendToTarget(response, sessionID);
        }
    }
}
