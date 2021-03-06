pg_dump --column-inserts \
    --dbname $POSTGRES_DB \
    --username $POSTGRES_USER \
    -a \
    -t user \
    -t user_address \
    -t address_config \
    -t known_address \
    -t tezos_release \
    -t proposal \
    -t proposal_vote \
    > /var/dump/dump_`date +%d-%m-%Y"_"%H_%M_%S`.sql