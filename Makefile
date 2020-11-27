project = tezos-bot
solution = TezosNotifyBot

export ASPNETCORE_ENVIRONMENT = Development

rm-dev-db: stop-dev-db
	@docker rm $(project)-db

stop-dev-db:
	@docker stop $(project)-db

start-dev-db:
	@docker start $(project)-db

create-dev-db:
	@docker run -d \
	-p 54320:5432 \
	--name=$(project)-db \
	--restart=always \
	-e POSTGRES_DB=$(project) \
	-e POSTGRES_USER=$(project) \
	-e POSTGRES_PASSWORD=$(project) \
	postgres:alpine

migrate:
	@dotnet ef database update -s ./$(solution)/ -p ./$(solution).Storage/ --context TezosDataContext

migrate-add: 
	@dotnet ef migrations add $(name) -s ./$(solution)/ -p ./$(solution).Storage/ --context TezosDataContext

migrate-down: 
	@dotnet ef database update $(name) -s ./$(solution)/ -p ./$(solution).Storage/ --context TezosDataContext

migrate-rm:
	@dotnet ef migrations remove -s ./$(solution)/ -p ./$(solution).Storage/ --context TezosDataContext
