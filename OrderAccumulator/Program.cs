using OrderAccumulator;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

var settings = new SessionSettings("acceptor.cfg");
var storeFactory = new FileStoreFactory(settings);
var logFactory = new FileLogFactory(settings);
var application = new FixAcceptor();
var acceptor = new ThreadedSocketAcceptor(application, storeFactory, settings, logFactory);

acceptor.Start();
Console.WriteLine("OrderAccumulator iniciado. Pressione qualquer tecla para sair...");
Console.ReadKey();
//acceptor.Stop();