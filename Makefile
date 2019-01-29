pack_client:
	cd src/SwiftClient; dotnet pack -c Release
pack_service:
	cd src/SwiftClient.AspNetCore; dotnet pack -c Release
push_client:
	./push_client.sh
push_service:
	./push_service.sh
upload_client: pack_client push_client
upload_service: pack_service push_service
	