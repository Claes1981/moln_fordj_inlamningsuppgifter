# Create the app registration and service principal
SP_OUTPUT=$(az ad sp create-for-rbac \
  --name "cloudsoft-github-actions" \
  --skip-assignment \
  --query '{clientId: appId, tenantId: appId}' \
  --output json)

# Extract values
CLIENT_ID=$(echo $SP_OUTPUT | jq -r '.clientId')
TENANT_ID=$(az account show --query tenantId --output tsv)
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

echo "CLIENT_ID=$CLIENT_ID"
echo "TENANT_ID=$TENANT_ID"
echo "SUBSCRIPTION_ID=$SUBSCRIPTION_ID"

az ad app federated-credential create --id $CLIENT_ID --parameters '{
  "name": "github-actions-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:Claes1981/moln_fordj_inlamningsuppgifter:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

az ad app federated-credential list --id $CLIENT_ID --output table

# Create the resource group first (required as scope for role assignment)
az group create \
  --name cloudsoft-rg \
  --location northeurope

# Assign Owner role on the resource group.
# Owner includes Contributor permissions plus roleAssignments/write,
# which is needed so the OIDC SP can create role assignments (e.g.
# Storage Blob Data Contributor) during CI/CD deployments.
# Contributor alone is insufficient — it lacks Microsoft.Authorization/roleAssignments/write.
# Role-Based Access Administrator does not exist in this educational subscription.
az role assignment create \
  --role Owner \
  --assignee $CLIENT_ID \
  --scope $(az group show --name cloudsoft-rg --query id --output tsv)

gh secret set AZURE_CLIENT_ID --body "$CLIENT_ID"
gh secret set AZURE_TENANT_ID --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"