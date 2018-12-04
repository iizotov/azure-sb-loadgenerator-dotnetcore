#!/bin/sh

[ "$INITIAL_SLEEP" ] && /bin/sleep $INITIAL_SLEEP
dotnet loadgenerator.dll -c "$CONNECTION_STRING_1" -b $BATCH_1 -t $THROUGHPUT_1 --terminate-after $TERMINATE_AFTER_1 -j -s $SIZE_1 --service $SERVICE_1
dotnet loadgenerator.dll -c "$CONNECTION_STRING_2" -b $BATCH_2 -t $THROUGHPUT_2 --terminate-after $TERMINATE_AFTER_2 -j -s $SIZE_2 --service $SERVICE_2