# dotnet tool install --global polyglotdatastudio.graphql.cli
# dotnet tool update --global polyglotdatastudio.graphql.cli

# Preview URL https://xmc-[org]-[project]-[environment].sitecorecloud.io/sitecore/api/graph/edge
# Live URL https://edge.sitecorecloud.io/api/graphql

& polyglotdatastudio.graphql.cli `
	-u "https://xmc-[org]-[project]-[environment].sitecorecloud.io/sitecore/api/graph/edge" `
	-h "{ 'sc_apikey': '[`$prompt:sc_apikey]' }" `
	-n "Web.GraphQL" `
	-o ".\apps\Web.GraphQL\" `
	--force `
	--public