#!/bin/bash

var_expand() {
  if [ -z "${1-}" ] || [ $# -ne 1 ]; then
    printf 'var_expand: expected one argument\n' >&2;
    return 1;
  fi
  eval printf '%s' "\"\${$1?}\""
}

echo "Sleeping for $INITIAL_SLEEP seconds"
[ "$INITIAL_SLEEP" ] && /bin/sleep $INITIAL_SLEEP

for ((i=1;i<=$NUM_ITERATIONS;i++))
do
    dotnet loadgenerator.dll -c "$(var_expand CONNECTION_STRING_$i)" -b $(var_expand BATCH_$i) \
        -t $(var_expand THROUGHPUT_$i) --terminate-after $(var_expand TERMINATE_AFTER_$i) \
        -j -s $(var_expand SIZE_$i) --service $(var_expand SERVICE_$i)
    echo "sleeping if SLEEP_$i is defined..."
    /bin/sleep $(var_expand SLEEP_$i) || :
done

