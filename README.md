# 3.4
Please check the below issues.
1. Default is 2000 if "playerLimit" option is set to any number in shard.cfg of welcome_service.
   Check this option.
2. Now server may be too much to serve lots of clients because 32bit server only use 4GB in any RAM.
    Please check the configure of servers. That is, check whether your services are layouted on a server or not.
    For instance, all of service may be layouted on the own server with each other.
      welcome_service and login_service layout on a server and openroom_service layout on the other server,         history_service layout on the another server and so on.

Please let me just take the log when some problems are happend at the server but not all of log.
