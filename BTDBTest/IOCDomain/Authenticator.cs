namespace BTDBTest.IOCDomain
{
	public class Authenticator : IAuthenticator
	{
	    readonly ILogger _logger;
	    readonly IErrorHandler _handler;
	    readonly IDatabase _database;

		public Authenticator(ILogger logger, IErrorHandler handler, IDatabase database)
		{
			_logger = logger;
			_handler = handler;
			_database = database;
		}

		public ILogger Logger { get { return _logger; } }
		public IErrorHandler ErrorHandler { get { return _handler; } }
		public IDatabase Database { get { return _database; } }
	}
}