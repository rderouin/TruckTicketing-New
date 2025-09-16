sqlcmd -E -dTruckTicketDataMigration -i%~dp0ClearQueues.sql -SAHQDSV-D365REP1 -v path="%~dp0" 
sqlcmd -E -dTruckTicketDataMigration -i%~dp0RunMessageQueueing.sql -SAHQDSV-D365REP1 -v path="%~dp0" 